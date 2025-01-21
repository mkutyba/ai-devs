using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using Agent.Application;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Agent.Infrastructure.Ai.Plugins;

public class HqApiDbPlugin
{
    private readonly ILogger<HqApiDbPlugin> _logger;
    private readonly HqService _hqService;
    private readonly HttpClient _httpClient;

    public HqApiDbPlugin(ILogger<HqApiDbPlugin> logger, HqService hqService, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _hqService = hqService;
        _httpClient = httpClientFactory.CreateClient(HttpClientType.ResilientClient);
    }

    [KernelFunction("ExecuteQuery")]
    [Description("Run a SQL query against the API database")]
    public async Task<string> ExecuteQuery(string query)
    {
        _logger.LogInformation("API DB Executing SQL query: {Query} on URL {Url}", query, _hqService.ApiDbUrl);
        var request = new
        {
            task = "database",
            apikey = _hqService.ApiKey,
            query
        };

        var response = await _httpClient.PostAsJsonAsync(_hqService.ApiDbUrl, request, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken.None);
        _logger.LogInformation("API DB Query execution response: {Result}", result?.RootElement.GetRawText());

        return result?.RootElement.GetRawText() ?? string.Empty;
    }

    [KernelFunction("ExecutePeopleQuery")]
    [Description("Run a query against the API database of people. It accepts only a person first name as a parameter.")]
    public async Task<string> ExecutePeopleQuery(string query)
    {
        _logger.LogInformation("API DB Executing query: {Query} on URL {Url}", query, _hqService.PeopleDbUrl);
        var request = new
        {
            apikey = _hqService.ApiKey,
            query
        };

        var response = await _httpClient.PostAsJsonAsync(_hqService.PeopleDbUrl, request, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken.None);
        _logger.LogInformation("API DB Query execution response: {Result}", result?.RootElement.GetRawText());

        return result?.RootElement.GetRawText() ?? string.Empty;
    }

    [KernelFunction("ExecutePlacesQuery")]
    [Description("Run a query against the API database of places. It accepts only a city name as a parameter.")]
    public async Task<string> ExecutePlacesQuery(string query)
    {
        _logger.LogInformation("API DB Executing query: {Query} on URL {Url}", query, _hqService.PlacesDbUrl);
        var request = new
        {
            apikey = _hqService.ApiKey,
            query
        };

        var response = await _httpClient.PostAsJsonAsync(_hqService.PlacesDbUrl, request, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonDocument>(CancellationToken.None);
        _logger.LogInformation("API DB Query execution response: {Result}", result?.RootElement.GetRawText());

        return result?.RootElement.GetRawText() ?? string.Empty;
    }
}