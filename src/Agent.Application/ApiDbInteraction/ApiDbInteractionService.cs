using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Agent.Application.Abstractions;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.ApiDbInteraction;

public class ApiDbInteractionService
{
    private readonly ILogger<ApiDbInteractionService> _logger;
    private readonly HqService _hqService;
    private readonly IAiService _aiService;
    private readonly HttpClient _httpClient;
    private List<string> _dbCommands = [];
    private List<string> _dbResults = [];

    public ApiDbInteractionService(ILogger<ApiDbInteractionService> logger, HqService hqService, IAiService aiService, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _hqService = hqService;
        _aiService = aiService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
    }

    public async Task<Result> CompleteTask13Async(CancellationToken ct)
    {
        var resultString = await ExecuteTaskPrompt(ct);
        var result = JsonSerializer.Deserialize<int[]>(resultString);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("database", result!, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<string> ExecuteTaskPrompt(CancellationToken ct)
    {
        var systemPrompt = """
                           API do zapytań do bazy danych https://centrala.ag3nts.org/apidb

                           Struktura zapytania - w odpowiedzi otrzymasz JSON-a z danymi
                           {
                               "task": "database",
                               "apikey": "Twój klucz API",
                               "query": "select * from users limit 1"
                           }

                           Poza typowymi selectami przydatne mogą być polecenia:
                           - show tables = zwraca listę tabel
                           - show create table NAZWA_TABELI = pokazuje, jak zbudowana jest konkretna tabela

                           Co należy zrobić w zadaniu?
                           1. Połącz się z nowym API, które dostarczyliśmy. Komunikujesz się z nim przez JSON-a
                           2. Zdobądź strukturę tabel, które Cię interesują
                           3. Przekaż strukturę do LLM-a i poproś o przygotowanie zapytania SQL, które spełnia nasze wymagania
                           4. Zdobądź odpowiedź na pytanie: “które aktywne datacenter (DC_ID) są zarządzane przez pracowników, którzy są na urlopie (is_active=0)”
                           5. Odpowiedź (w formie tablicy) podaj na samym końcu jako wynik w tagu <result>wynik</result>
                           """;

        var userPrompt = $"""
                          masz wydobyć z bazy odpowiedź na pytanie: “które aktywne datacenter (DC_ID) są zarządzane przez pracowników, którzy są na urlopie (is_active=0)”
                          pracujesz na bazie MySQL
                          możesz podawać zapytania do bazy danych, które zostaną wysłane do API a ich wynik zostanie Ci zaprezentowany

                          do tej pory zrobiłeś:
                          {string.Join("\n", _dbCommands)}

                          wyniki tych zapytań:
                          {string.Join("\n", _dbResults)}

                          Podaj mi zapytanie do uruchomienia (tylko zapytanie, żadnych dodatkowych treści, żadnych cudzysłowów ani apostrofów dookoła).
                          Jeżeli uważasz, że doprowadziłeś do wyniku, podaj tylko wynik w tagu <result>wynik</result>.
                          """;

        var query = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_202411, systemPrompt, userPrompt, ct);

        var match = Regex.Match(query, "<result>(.*?)</result>");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        var result = await ExecuteApiDbQuery(query, ct);
        _dbCommands.Add(query);
        _dbResults.Add(result);

        return await ExecuteTaskPrompt(ct);
    }

    private async Task<string> ExecuteApiDbQuery(string query, CancellationToken ct)
    {
        _logger.LogInformation("Executing SQL query: {Query}", query);
        var request = new
        {
            task = "database",
            apikey = _hqService.ApiKey,
            query
        };

        var response = await _httpClient.PostAsJsonAsync(_hqService.ApiDbUrl, request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
        _logger.LogInformation("Query execution response: {Result}", result?.RootElement.GetRawText());

        return result?.RootElement.GetRawText() ?? string.Empty;
    }
}