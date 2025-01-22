using Agent.Application.Abstractions.Ai;
using Agent.Application.Abstractions.VectorDatabase;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.FactAnalyzer;

public class FactAnalyzerService
{
    private readonly ILogger<FactAnalyzerService> _logger;
    private readonly IVectorDatabaseService _vectorDatabaseService;
    private readonly IAiService _aiService;
    private readonly HqService _hqService;

    public FactAnalyzerService(ILogger<FactAnalyzerService> logger, IVectorDatabaseService vectorDatabaseService, IAiService aiService, HqService hqService)
    {
        _logger = logger;
        _vectorDatabaseService = vectorDatabaseService;
        _aiService = aiService;
        _hqService = hqService;
    }

    public async Task<Result> CompleteTask11Async(CancellationToken ct)
    {
        var (factPaths, reportPaths) = GetFilePaths(ct);

        var factKeywords = await ProcessFactsAsync(factPaths, ct);
        await StoreFactsInDbAsync(factKeywords, ct);
        var reportKeywords = await ProcessReportsAsync(reportPaths, ct);

        var result = reportKeywords.ToDictionary(
            x => Path.GetFileName(x.Key),
            x => x.Value);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("dokumenty", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task<Dictionary<string, string>> ProcessReportsAsync(string[] reportPaths, CancellationToken ct)
    {
        var results = new Dictionary<string, string>();

        foreach (var reportPath in reportPaths)
        {
            var reportContent = await File.ReadAllTextAsync(reportPath, ct);
            var reportKeywords = await ExtractKeywordsFromPath(reportPath, ct);
            var ragRecords = await _vectorDatabaseService.GetMatchingRecordsAsync(reportKeywords, VectorDatabaseCollection.Facts, 1, ct);
            var keywords = await ExtractKeywords($"{reportContent}\n\nAdditional context: {(ragRecords.Any() ? ragRecords.First() : "none")}", ct);
            results.Add(reportPath, keywords);
            _logger.LogInformation("Processed report file {ReportPath} with keywords: {Keywords}", reportPath, keywords);
        }

        return results;
    }

    private async Task StoreFactsInDbAsync(Dictionary<string, string> factKeywords, CancellationToken ct)
    {
        await _vectorDatabaseService.RecreateCollection(ct);

        foreach (var (path, keywords) in factKeywords)
        {
            var factContent = await File.ReadAllTextAsync(path, ct);
            await _vectorDatabaseService.SaveFactToVectorDbAsync(factContent, keywords, VectorDatabaseCollection.Facts, ct);
        }
    }

    private async Task<Dictionary<string, string>> ProcessFactsAsync(string[] factPaths, CancellationToken ct)
    {
        var results = new Dictionary<string, string>();

        foreach (var factPath in factPaths)
        {
            var keywords = await ExtractKeywordsFromPath(factPath, ct);
            results.Add(factPath, keywords);
            _logger.LogInformation("Processed fact file {FactPath} with keywords: {Keywords}", factPath, keywords);
        }

        return results;
    }

    private async Task<string> ExtractKeywordsFromPath(string path, CancellationToken ct)
    {
        var fileContents = await File.ReadAllTextAsync(path, ct);
        var content = $"{fileContents}\n\nFile name: {Path.GetFileNameWithoutExtension(path)}";
        return await ExtractKeywords(content, ct);
    }

    private async Task<string> ExtractKeywords(string content, CancellationToken ct)
    {
        const string systemPrompt = """
                                    You are an expert at analyzing text and extracting key information.
                                    Extract the most important facts and people mentioned in the text.
                                    Format your response as a comma - separated list of keywords.
                                    Focus on names of people, organizations, locations, and key events and their characteristics and properties.
                                    Respond only with a list of keywords, don't add anything else.
                                    """;

        var userPrompt = $"Analyze this text and extract key facts and people as keywords:\n\n{content}";

        var keywords = await _aiService.GetChatCompletionAsync(
            AiModelType.Gpt4o_202411,
            systemPrompt,
            userPrompt,
            ct);
        return keywords;
    }

    private (string[] FactPaths, string[] ReportPaths) GetFilePaths(CancellationToken ct)
    {
        var factPaths = Directory.GetFiles("data/pliki_z_fabryki/facts", "*.txt", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found fact files: {FactPaths}", string.Join(", ", factPaths));

        var reportPaths = Directory.GetFiles("data/pliki_z_fabryki", "*.txt", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found report files: {ReportPaths}", string.Join(", ", reportPaths));

        return (factPaths, reportPaths);
    }
}