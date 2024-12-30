namespace Agent.Application.Abstractions;

public interface IAiService
{
    Task<string> GetChatCompletionAsync(AiModelType modelId, string systemMessage, string userMessage, CancellationToken ct);
    Task<string> GetAudioTranscriptionAsync(AiModelType modelId, string userMessage, Stream audioStream, string fileName, string language, CancellationToken ct);
}