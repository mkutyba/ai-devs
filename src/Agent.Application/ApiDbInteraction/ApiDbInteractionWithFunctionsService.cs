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
        var result = await ExecuteTask13PromptWithFunctions(ct);
        var dcIds = JsonSerializer.Deserialize<int[]>(result);

        _logger.LogDebug("Sending result: {Result}", dcIds);

        var response = await _hqService.SendReportAsync("database", dcIds!, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    public async Task<Result> CompleteTask14Async(CancellationToken ct)
    {
        var data = await _hqService.GetTask14FileAsync(ct);
        var result = await ExecuteTask14PromptWithFunctions($"Memo: {data}", ct);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("loop", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<string> ExecuteTask14PromptWithFunctions(string userPrompt, CancellationToken ct)
    {
        const string systemPrompt = """
                                    You are digital data forensics expert. You have access to a database with information about people and places.
                                    You can ask 2 APIs for people and places, using functions ExecutePeopleQuery or ExecutePlacesQuery.
                                    
                                    Consider what people and what city names are mentioned in the memo.
                                    Ask the relevant API about the mentioned clues.
                                    Chances are that from the data received by the API you will get further names or city names.
                                    Query them one by one until you find the city where BARBARA is located.
                                    
                                    There is a chance that by walking down some twisty road you will come across a secret flag 🚩.
                                    
                                    Put all the data together and fill in the missing information. Who was Aleksander and Barbara's associate? With whom did Rafal see each other?
                                    Perhaps finding information on this topic will allow us to pick out another place to look for Barbara.
                                    
                                    When you track down the city where BARBARA is located, send back the name of the city in the exact form from the API and nothing else,
                                    no additional characters, quotes or apostrophes. Sample output for the city of Wrocław: WROCLAW.
                                    Very important: the city name must be exactly the same as returned by the API.
                                    
                                    Sample response: WROCLAW
                                    """;

        return await _aiService.GetChatCompletionWithFunctionsAsync(
            AiModelType.Gpt4o_202411,
            systemPrompt,
            userPrompt,
            ct);
    }

    private async Task<string> ExecuteTask13PromptWithFunctions(CancellationToken ct)
    {
        const string systemPrompt = """
                                    You are a database expert. Use the ExecuteQuery function to interact with the MySQL database.
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