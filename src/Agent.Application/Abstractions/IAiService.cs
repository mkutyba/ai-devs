namespace Agent.Application.Abstractions;

public interface IAiService
{
    Task<string> GetChatCompletionAsync(AiModelType modelId, string systemMessage, string userMessage, CancellationToken ct);
    Task<string> GetAudioTranscriptionAsync(AiModelType modelId, string userMessage, Stream audioStream, string fileName, string language, CancellationToken ct);
    Task<string> GetVisionChatCompletionAsync(AiModelType modelId, string systemMessage, string userMessage, IReadOnlyCollection<ReadOnlyMemory<byte>> imageData, CancellationToken ct);
    Task<string> GenerateImageAsync(AiModelType modelId, string userMessage, AiImageSize imageSize, AiImageQuality imageQuality, CancellationToken ct);
}