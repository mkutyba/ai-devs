using System.Text.Json;
using Agent.Application.Abstractions;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.ApiDbInteraction;

public class ApiDbInteractionWithFunctionsService
{
    private readonly ILogger<ApiDbInteractionWithFunctionsService> _logger;
    private readonly HqService _hqService;
    private readonly IAiService _aiService;
    private readonly HttpClient _httpClient;

    public ApiDbInteractionWithFunctionsService(ILogger<ApiDbInteractionWithFunctionsService> logger, HqService hqService, IAiService aiService, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _hqService = hqService;
        _aiService = aiService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
    }

    public async Task<Result> CompleteTask13Async(CancellationToken ct)
    {
        var result = await ExecuteTaskWithFunctions(ct);
        var dcIds = JsonSerializer.Deserialize<int[]>(result);

        _logger.LogDebug("Sending result: {Result}", dcIds);

        var response = await _hqService.SendReportAsync("database", dcIds!, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<string> ExecuteTaskWithFunctions(CancellationToken ct)
    {
        const string systemPrompt = """
                                    You are a database expert. Your task is to find which active datacenter (DC_ID) are managed by employees who are on vacation (is_active=0).
                                    Use the ExecuteQuery function to interact with the MySQL database.
                                    First explore the database structure, then write queries to find the answer.
                                    Once you have the final result, return it as a JSON array of DC_IDs and nothing else, no additional characters, quotes or apostrophes.
                                    """;
        const string userPrompt = "które aktywne datacenter (DC_ID) są zarządzane przez pracowników, którzy są na urlopie (is_active=0)";

        return await _aiService.GetChatCompletionWithFunctionsAsync(
            AiModelType.Gpt4o_202411,
            systemPrompt,
            userPrompt,
            ct);
    }
}