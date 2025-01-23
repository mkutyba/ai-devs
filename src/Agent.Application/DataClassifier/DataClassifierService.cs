using Agent.Application.Abstractions.Ai;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.DataClassifier;

public class DataClassifierService
{
    private readonly ILogger<DataClassifierService> _logger;
    private readonly IAiService _aiService;
    private readonly HqService _hqService;

    public DataClassifierService(ILogger<DataClassifierService> logger, IAiService aiService, HqService hqService)
    {
        _logger = logger;
        _aiService = aiService;
        _hqService = hqService;
    }

    public async Task<Result> CompleteTask17Async(CancellationToken ct)
    {
        var result = await ClassifyDataAsync(ct);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("research", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<List<string>> ClassifyDataAsync(CancellationToken ct)
    {
        var data = await File.ReadAllTextAsync("data/lab_data/verify.txt", ct);
        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var dataToVerify = lines
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0], x => x[1]);

        var validIds = new List<string>();

        const string systemMessage = "You are provided with research results. Decide whether the results are valid or have been manipulated.";
        foreach (var dataSample in dataToVerify)
        {
            var result = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_Mini_202407_FineTunedS04E02, systemMessage, dataSample.Value, ct);
            if (result.Trim().Equals("valid", StringComparison.OrdinalIgnoreCase))
            {
                validIds.Add(dataSample.Key);
            }
        }

        return validIds;
    }
}