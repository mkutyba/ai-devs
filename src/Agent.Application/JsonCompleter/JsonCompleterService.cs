using Agent.Application.Abstractions;
using Agent.Application.Ai;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCalc;
using Newtonsoft.Json.Linq;

namespace Agent.Application.JsonCompleter;

public class JsonCompleterService
{
    private readonly ILogger<JsonCompleterService> _logger;
    private readonly HqService _hqService;
    private readonly IAiSimpleAnswerService _aiSimpleAnswerService;
    private readonly HqSettings _settings;

    public JsonCompleterService(ILogger<JsonCompleterService> logger, IOptions<HqSettings> settings, HqService hqService, IAiSimpleAnswerService aiSimpleAnswerService)
    {
        _logger = logger;
        _hqService = hqService;
        _aiSimpleAnswerService = aiSimpleAnswerService;
        _settings = settings.Value;
    }

    public async Task<Result> CompleteTheQuestAsync(CancellationToken ct)
    {
        var apiKey = _settings.ApiKey;

        _logger.LogDebug("Getting file content");

        var fileContent = await File.ReadAllTextAsync("data/json.txt", ct);
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

            var answer = await _aiSimpleAnswerService.GetAnswerToSimpleQuestionAsync(question.ToString(), ct);
            testTest["a"] = answer;
        }

        jsonObject["test-data"] = new JArray(testData);
        jsonObject["apikey"] = apiKey;

        _logger.LogDebug("Sending answer: {Answer}", jsonObject.ToString());

        var response = await _hqService.SendReportAsync("JSON", jsonObject, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }
}