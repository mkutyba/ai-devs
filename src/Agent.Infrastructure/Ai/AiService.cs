using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agent.Infrastructure.Ai;

public class AiService : IAiService
{
    private readonly Kernel _kernel;
    private readonly ILogger<AiService> _logger;

    public AiService(Kernel kernel, ILogger<AiService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> GetChatCompletionAsync(AiModelType modelId, string systemMessage, string userMessage, CancellationToken ct)
    {
        ChatHistory history = [];
        history.AddSystemMessage(systemMessage);
        _logger.LogDebug("Chat system message: {SystemMessage}", systemMessage);

        _logger.LogDebug("Chat user message: {UserMessage}", userMessage);
        history.AddUserMessage(userMessage);

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(modelId.ToString());
        var response = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: ct);
        _logger.LogDebug("Chat response: {ResponseContent}", response.Content);

        return response.Content ?? string.Empty;
    }

    public async Task<string> GetAudioTranscriptionAsync(AiModelType modelId, string userMessage, Stream audioStream, string fileName, string language, CancellationToken ct)
    {
        var audioToTextService = _kernel.GetRequiredService<IAudioToTextService>(modelId.ToString());

        var executionSettings = new OpenAIAudioToTextExecutionSettings(fileName)
        {
            Language = language,
            Prompt = userMessage,
            ResponseFormat = "json",
        };

        var audioContent = new AudioContent(
            await BinaryData.FromStreamAsync(audioStream, ct),
            mimeType: null);

        var result = await audioToTextService.GetTextContentAsync(audioContent, executionSettings, cancellationToken: ct);
        if (result.Text is null)
        {
            throw new InvalidOperationException($"Audio transcription failed for file {fileName}");
        }

        return result.Text;
    }
}