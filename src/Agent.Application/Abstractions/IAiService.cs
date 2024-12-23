namespace Agent.Application.Abstractions;

public interface IAiService
{
    Task<string> GetChatCompletionAsync(ModelType modelId, string systemMessage, string userMessage, CancellationToken ct);
}