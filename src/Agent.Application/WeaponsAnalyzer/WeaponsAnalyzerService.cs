using Agent.Application.Abstractions;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.WeaponsAnalyzer;

public class WeaponsAnalyzerService
{
    private readonly ILogger<WeaponsAnalyzerService> _logger;
    private readonly IVectorDatabaseService _vectorDatabaseService;
    private readonly HqService _hqService;

    public WeaponsAnalyzerService(ILogger<WeaponsAnalyzerService> logger, IVectorDatabaseService vectorDatabaseService, HqService hqService)
    {
        _logger = logger;
        _vectorDatabaseService = vectorDatabaseService;
        _hqService = hqService;
    }

    public async Task<Result> CompleteTask12Async(CancellationToken ct)
    {
        var paths = GetFilePaths();

        var facts = await ProcessFilesAsync(paths, ct);
        await StoreFactsInDbAsync(facts, ct);

        string query = "W raporcie, z którego dnia znajduje się wzmianka o kradzieży prototypu broni?";
        var match = await _vectorDatabaseService.GetMatchingRecordsAsync(query, VectorDatabaseCollection.WeaponFacts, 1, ct);
        if (match.Any())
        {
            _logger.LogInformation("Found matching record: {Match}", match.First());
        }
        else
        {
            _logger.LogInformation("No matching records found");
            return new Result(false, "No matching records found");
        }

        var parts = match.First().Split('_');
        var result = $"{parts[0]}-{parts[1]}-{parts[2]}";

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("wektory", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private async Task StoreFactsInDbAsync(Dictionary<string, string> factKeywords, CancellationToken ct)
    {
        await _vectorDatabaseService.RecreateCollection(ct);

        foreach (var (path, contents) in factKeywords)
        {
            await _vectorDatabaseService.SaveFactToVectorDbAsync(Path.GetFileNameWithoutExtension(path), contents, VectorDatabaseCollection.WeaponFacts, ct);
        }
    }

    private async Task<Dictionary<string, string>> ProcessFilesAsync(string[] paths, CancellationToken ct)
    {
        var results = new Dictionary<string, string>();

        foreach (var path in paths)
        {
            var fileContents = await File.ReadAllTextAsync(path, ct);
            results.Add(path, fileContents);
            _logger.LogInformation("Processed fact file {FactPath} with contents: {FileContents}", path, fileContents);
        }

        return results;
    }

    private string[] GetFilePaths()
    {
        var paths = Directory.GetFiles("data/pliki_z_fabryki/weapons_tests/do-not-share", "*.txt", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found files: {Paths}", string.Join(", ", paths));

        return paths;
    }
}