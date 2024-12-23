using Agent.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Agent.Infrastructure.OpenAi;

public class AiService : IAiService
{
    private readonly Kernel _kernel;
    private readonly ILogger<AiService> _logger;
    private IChatCompletionService? _chatCompletionService;

    public AiService(Kernel kernel, ILogger<AiService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<string> GetChatCompletionAsync(ModelType modelId, string systemMessage, string userMessage, CancellationToken ct)
    {
        ChatHistory history = [];
        history.AddSystemMessage(systemMessage);
        _logger.LogDebug("Chat system message: {SystemMessage}", systemMessage);

        _logger.LogDebug("Chat user message: {UserMessage}", userMessage);
        history.AddUserMessage(userMessage);

        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>(modelId.ToString());
        var response = await _chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: ct);
        _logger.LogDebug("Chat response: {ResponseContent}", response.Content);

        return response.Content ?? string.Empty;
    }
}