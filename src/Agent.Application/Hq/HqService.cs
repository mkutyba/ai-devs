using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Agent.Application.Hq;

public class HqService
{
    public string ApiKey { get; }
    private readonly HttpClient _httpClient;
    private readonly ILogger<HqService> _logger;
    private readonly HqSettings _settings;
    public string Task10ArticleBaseUrl => $"{_settings.BaseUrl.TrimEnd('/')}/dane/";
    public string ApiDbUrl => $"{_settings.BaseUrl.TrimEnd('/')}/apidb";
    public string PeopleDbUrl => $"{_settings.BaseUrl.TrimEnd('/')}/people";
    public string PlacesDbUrl => $"{_settings.BaseUrl.TrimEnd('/')}/places";

    public HqService(IHttpClientFactory httpClientFactory, ILogger<HqService> logger, IOptions<HqSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
        _logger = logger;
        _settings = settings.Value;
        ApiKey = _settings.ApiKey;
    }

    public async Task<HttpResponseMessage> SendReportAsync(string taskName, object answer, CancellationToken ct)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/report";
        var request = new HqRequestModel
        {
            Task = taskName,
            Apikey = _settings.ApiKey,
            Answer = answer
        };
        var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, ct);

        return response;
    }

    public async Task<string> GetDataToCensor(CancellationToken ct)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/data/{_settings.ApiKey}/cenzura.txt";

        _logger.LogInformation("Fetching file from {Url}", url);
        return await _httpClient.GetStringAsync(url, ct);
    }

    public async Task<string> GetTask8Data(CancellationToken ct)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/data/{_settings.ApiKey}/robotid.json";

        _logger.LogInformation("Fetching file from {Url}", url);
        var data = await _httpClient.GetFromJsonAsync<HqRobotDescription>(url, ct);
        return data?.Description ?? throw new InvalidDataException("Failed to get robot description");
    }

    public async Task<string> GetTask10QuestionsAsync(CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/data/{_settings.ApiKey}/arxiv.txt";
            _logger.LogInformation("Downloading questions from: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get questions");
            throw;
        }
    }

    public async Task<string> GetTask10ArticleAsync(CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/dane/arxiv-draft.html";
            _logger.LogInformation("Downloading article from: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get article");
            throw;
        }
    }

    public async Task<string> GetTask14FileAsync(CancellationToken ct)
    {
        try
        {
            var url = $"{_settings.BaseUrl.TrimEnd('/')}/dane/barbara.txt";
            _logger.LogInformation("Downloading data from: {Url}", url);

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data");
            throw;
        }
    }
}