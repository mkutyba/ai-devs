using System.Text;
using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCalc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Agent.Application.JsonCompleter;

public interface IJsonCompleterService
{
    Task<Result> CompleteTheQuestAsync(CancellationToken ct);
}

public class JsonCompleterService : IJsonCompleterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JsonCompleterService> _logger;
    private readonly IOpenAiService _openAiService;
    private readonly JsonCompleterSettings _settings;

    public JsonCompleterService(HttpClient httpClient, ILogger<JsonCompleterService> logger, IOptions<JsonCompleterSettings> settings, IOpenAiService openAiService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _openAiService = openAiService;
        _settings = settings.Value;
    }

    public async Task<Result> CompleteTheQuestAsync(CancellationToken ct)
    {
        var apiKey = _settings.ApiKey;

        _logger.LogDebug("Getting file content");

        var fileContent = await File.ReadAllTextAsync("json.txt", ct);
        var jsonObject = JObject.Parse(fileContent);
        var testData = jsonObject["test-data"]?.ToArray();

        if (testData == null)
        {
            return new Result(false, "No test data found");
        }

        foreach (var test in testData)
        {
            var expression = new Expression(test["question"]?.ToString());
            var result = expression.Evaluate();
            test["answer"] = int.Parse(result?.ToString() ?? string.Empty);

            var testTest = test["test"];

            if (testTest is null)
            {
                continue;
            }

            var question = testTest["q"];

            if (question == null)
            {
                continue;
            }

            var answer = await _openAiService.GetAnswerToSimpleQuestionAsync(question.ToString(), ct);
            testTest["a"] = answer;
        }

        jsonObject["test-data"] = new JArray(testData);
        jsonObject["apikey"] = apiKey;

        _logger.LogDebug("Sending answer: {Answer}", jsonObject.ToString());

        var request = new CentralRequestModelTask3
        {
            Task = "JSON",
            Apikey = apiKey,
            Answer = jsonObject
        };

        var requestJson = JsonConvert.SerializeObject(request, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_settings.ReportUrl, content, ct);

        var responseContent = await response.Content.ReadAsStringAsync(ct);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }
}