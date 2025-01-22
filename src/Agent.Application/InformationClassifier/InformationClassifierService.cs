using System.Text.Json;
using Agent.Application.Abstractions.Ai;
using Agent.Application.AudioToText;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.InformationClassifier;

public class InformationClassifierService
{
    private readonly ILogger<InformationClassifierService> _logger;
    private readonly HqService _hqService;
    private readonly IAiService _aiService;
    private readonly SpeechToTextService _speechToTextService;

    public InformationClassifierService(ILogger<InformationClassifierService> logger, HqService hqService, IAiService aiService, SpeechToTextService speechToTextService)
    {
        _logger = logger;
        _hqService = hqService;
        _aiService = aiService;
        _speechToTextService = speechToTextService;
    }

    public async Task<Result> CompleteTask9Async(CancellationToken ct)
    {
        var (txtPaths, pngPaths, mp3Paths) = GetFilePaths(ct);
        var filesClassification = await ProcessFiles(txtPaths, pngPaths, mp3Paths, ct);

        var result = new FileClassificationResult(
            People:
            [
                .. filesClassification
                    .Where(r => r.Value == InformationClassType.People)
                    .Select(r => Path.GetFileName(r.Key))
            ],
            Hardware:
            [
                .. filesClassification
                    .Where(r => r.Value == InformationClassType.Machines)
                    .Select(r => Path.GetFileName(r.Key))
            ]);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("kategorie", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }

    private (string[] TxtPaths, string[] PngPaths, string[] Mp3Paths) GetFilePaths(CancellationToken ct)
    {
        var txtPaths = Directory.GetFiles("data/pliki_z_fabryki", "*.txt", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found .txt files: {TxtPaths}", string.Join(", ", txtPaths));

        var pngPaths = Directory.GetFiles("data/pliki_z_fabryki", "*.png", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found .png files: {PngPaths}", string.Join(", ", pngPaths));

        var mp3Paths = Directory.GetFiles("data/pliki_z_fabryki", "*.mp3", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found .mp3 files: {Mp3Paths}", string.Join(", ", mp3Paths));

        return (txtPaths, pngPaths, mp3Paths);
    }

    private async Task<Dictionary<string, InformationClassType>> ProcessFiles(string[] txtPaths, string[] pngPaths, string[] mp3Paths, CancellationToken ct)
    {
        var textResults = await ProcessTextFiles(txtPaths, ct);
        var imageResults = await ProcessImageFiles(pngPaths, ct);
        var audioResults = await ProcessAudioFiles(mp3Paths, ct);

        var results = new Dictionary<string, InformationClassType>();
        foreach (var dict in new[]
                 {
                     textResults,
                     imageResults,
                     audioResults
                 })
        {
            foreach (var kvp in dict)
            {
                results[kvp.Key] = kvp.Value;
            }
        }

        return results;
    }

    private const string ClassificationPrompt = """
                                                <objective>
                                                Analyze the given text and classify it into a structured JSON response.
                                                Focus only on:
                                                - Confirmed presence of people or clear evidence of current human activity
                                                - Hardware repairs and physical machine issues (ignore software-related issues)
                                                </objective>

                                                <rules>
                                                - Return ONLY valid JSON format
                                                - Include exactly two fields: "thinking_process" and "answer"
                                                - For machines, only consider hardware/physical issues, not software
                                                - For people classification, ONLY include:
                                                    * Confirmed sightings or evidence of recent presence of people that are going to be searched for in order to be detained or already have been detained or captured 
                                                    * Captured or detained individuals
                                                - DO NOT classify as "people" if:
                                                    * People were not found
                                                    * Location is abandoned
                                                    * Only mentions searching without results
                                                    * Historical or past presence only
                                                    * People are mentioned in a non-relevant context, e.g. food delivery
                                                - "thinking_process" must explain the classification reasoning
                                                - "answer" must be exactly one of: "people", "machines", or "none"
                                                - No markdown formatting or additional text
                                                - Maintain clean, parseable JSON structure
                                                </rules>

                                                <answer_format>
                                                {
                                                    "thinking_process": "Brief explanation of classification logic",
                                                    "answer": "category"
                                                }
                                                </answer_format>

                                                <validation>
                                                - Verify JSON is properly formatted
                                                - Confirm answer is one of three allowed values
                                                - Ensure thinking process explains classification
                                                - Double-check that "people" classification only applies to confirmed presence
                                                </validation>
                                                """;

    private async Task<Dictionary<string, InformationClassType>> ProcessTextFiles(string[] txtPaths, CancellationToken ct)
    {
        var results = new Dictionary<string, InformationClassType>();

        foreach (var path in txtPaths)
        {
            var content = await File.ReadAllTextAsync(path, ct);

            var analysis = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_Mini_202407, ClassificationPrompt, content, ct);

            var response = JsonSerializer.Deserialize<AIResponse>(analysis)!;

            _logger.LogInformation("Full analysis of {FileName}: {Analysis}", Path.GetFileName(path), analysis);
            _logger.LogInformation("Answer: {Answer}", response.Answer);

            results[path] = Enum.Parse<InformationClassType>(response.Answer, true);
        }

        return results;
    }

    private async Task<Dictionary<string, InformationClassType>> ProcessImageFiles(string[] pngPaths, CancellationToken ct)
    {
        var results = new Dictionary<string, InformationClassType>();

        foreach (var path in pngPaths)
        {
            await using var fileStream = File.OpenRead(path);
            var imageBytes = new byte[fileStream.Length];
            await fileStream.ReadExactlyAsync(imageBytes, ct);
            var images = new List<ReadOnlyMemory<byte>>
            {
                imageBytes
            };

            var analysis = await _aiService.GetVisionChatCompletionAsync(AiModelType.Gpt4o_Mini_202407, ClassificationPrompt, "Analyze this image:", images, ct);

            var response = JsonSerializer.Deserialize<AIResponse>(analysis)!;

            _logger.LogInformation("Full analysis of {FileName}: {Analysis}", Path.GetFileName(path), analysis);
            _logger.LogInformation("Answer: {Answer}", response.Answer);

            results[path] = Enum.Parse<InformationClassType>(response.Answer, true);
        }

        return results;
    }

    private async Task<Dictionary<string, InformationClassType>> ProcessAudioFiles(string[] mp3Paths, CancellationToken ct)
    {
        var results = new Dictionary<string, InformationClassType>();

        foreach (var path in mp3Paths)
        {
            await using var audioStream = File.OpenRead(path);

            _logger.LogInformation("Starting transcription of {AudioFile}", path);
            var transcriptionFile = await _speechToTextService.ConvertAudioToText(path, ".whisper", "en", ct);
            _logger.LogInformation("Finished transcription of {AudioFile}", path);

            var content = await File.ReadAllTextAsync(transcriptionFile, ct);

            var analysis = await _aiService.GetChatCompletionAsync(AiModelType.Gpt4o_Mini_202407, ClassificationPrompt, content, ct);

            var response = JsonSerializer.Deserialize<AIResponse>(analysis)!;

            _logger.LogInformation("Full analysis of {FileName}: {Analysis}", Path.GetFileName(path), analysis);
            _logger.LogInformation("Answer: {Answer}", response.Answer);

            results[path] = Enum.Parse<InformationClassType>(response.Answer, true);
        }

        return results;
    }
}