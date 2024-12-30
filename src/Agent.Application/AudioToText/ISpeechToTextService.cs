namespace Agent.Application.AudioToText;

public interface ISpeechToTextService
{
    Task<string> ConvertAudioToText(string audioFile, CancellationToken ct);
}