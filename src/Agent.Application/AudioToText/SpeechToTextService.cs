using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Agent.Application.AudioToText;

public class SpeechToTextService : ISpeechToTextService
{
    private readonly IAiService _aiService;
    private readonly ILogger<SpeechToTextService> _logger;

    public SpeechToTextService(IAiService aiService, ILogger<SpeechToTextService> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> ConvertAudioToText(string audioFile, CancellationToken ct)
    {
        var transcriptionPath = Path.ChangeExtension(audioFile, ".txt");

        if (File.Exists(transcriptionPath))
        {
            _logger.LogInformation("Using existing transcription file: {TranscriptionPath}", transcriptionPath);
            return transcriptionPath;
        }

        _logger.LogInformation("Creating new transcription for file: {AudioFile}", audioFile);
        await using var audioStream = File.OpenRead(audioFile);

        var transcription = await _aiService.GetAudioTranscriptionAsync(
            AiModelType.Whisper1,
            string.Empty,
            audioStream,
            Path.GetFileName(audioFile),
            "pl",
            ct);

        await File.WriteAllTextAsync(transcriptionPath, transcription, ct);
        _logger.LogInformation("Saved transcription to: {TranscriptionPath}", transcriptionPath);
        _logger.LogDebug("Transcription data: {Transcription}", transcription);

        return transcriptionPath;
    }
}