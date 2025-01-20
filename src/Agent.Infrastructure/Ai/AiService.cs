using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AudioToText;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.TextToImage;

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

    public async Task<string> GetChatCompletionWithImagesAsync(AiModelType modelId, string systemMessage, string userMessage, ReadOnlyMemory<byte>[] images, CancellationToken ct)
    {
        ChatHistory history = [];
        history.AddSystemMessage(systemMessage);
        _logger.LogDebug("Chat system message: {SystemMessage}", systemMessage);

        _logger.LogDebug("Chat user message: {UserMessage}", userMessage);
        var messageContent = new ChatMessageContentItemCollection
        {
            new TextContent(userMessage)
        };

        foreach (var imageBytes in images)
        {
            messageContent.Add(new ImageContent(imageBytes, "image/jpeg"));
        }

        history.AddUserMessage(messageContent);

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

    public async Task<string> GetVisionChatCompletionAsync(AiModelType modelId, string systemMessage, string userMessage, IReadOnlyCollection<ReadOnlyMemory<byte>> imageData, CancellationToken ct)
    {
        ChatHistory history = [];
        history.AddSystemMessage(systemMessage);
        _logger.LogDebug("Chat system message: {SystemMessage}", systemMessage);

        _logger.LogDebug("Chat user message: {UserMessage}", userMessage);
        var messageContent = new ChatMessageContentItemCollection
        {
            new TextContent(userMessage)
        };

        foreach (var imageBytes in imageData)
        {
            messageContent.Add(new ImageContent(imageBytes, "image/png"));
        }

        history.AddUserMessage(messageContent);

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(modelId.ToString());
        var response = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: ct);
        _logger.LogDebug("Chat response: {ResponseContent}", response.Content);

        return response.Content ?? string.Empty;
    }

    public async Task<string> GenerateImageAsync(AiModelType modelId, string userMessage, AiImageSize imageSize, AiImageQuality imageQuality, CancellationToken ct)
    {
        if (modelId != AiModelType.Dalle3)
        {
            throw new InvalidOperationException("Only DALL·E 3 model supports image generation");
        }

        var textToImageService = _kernel.GetRequiredService<ITextToImageService>(modelId.ToString());

        var executionSettings = new OpenAITextToImageExecutionSettings
        {
            Size = GetImageDimensions(imageSize),
            Quality = GetImageQuality(imageQuality),
        };

        var result = await textToImageService.GetImageContentsAsync(new TextContent(userMessage), executionSettings, cancellationToken: ct);

        return result[0].Uri?.ToString() ?? string.Empty;
    }

    public async Task<float[]> GetEmbeddingAsync(AiModelType modelId, string text, CancellationToken ct)
    {
        var embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>(modelId.ToString());
        var embeddings = await embeddingService.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return embeddings.ToArray();
    }

    public async Task<string> GetChatCompletionWithFunctionsAsync(AiModelType modelId, string systemMessage, string userMessage, CancellationToken ct)
    {
        ChatHistory history = [];
        history.AddSystemMessage(systemMessage);
        _logger.LogDebug("Chat system message: {SystemMessage}", systemMessage);

        _logger.LogDebug("Chat user message: {UserMessage}", userMessage);
        history.AddUserMessage(userMessage);

        var promptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(modelId.ToString());
        var response = await chatCompletionService.GetChatMessageContentAsync(history, promptExecutionSettings, _kernel, ct);
        _logger.LogDebug("Chat response: {ResponseContent}", response.Content);

        return response.Content ?? string.Empty;
    }

    private static (int Width, int Height) GetImageDimensions(AiImageSize size) =>
        size switch
        {
            AiImageSize.Square1024 => (1024, 1024),
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };

    private static string GetImageQuality(AiImageQuality quality) =>
        quality switch
        {
            AiImageQuality.Standard => "standard",
            AiImageQuality.Hd => "hd",
            _ => throw new ArgumentOutOfRangeException(nameof(quality))
        };
}