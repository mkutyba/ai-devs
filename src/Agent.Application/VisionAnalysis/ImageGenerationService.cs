using Agent.Application.Abstractions.Ai;
using Agent.Application.Hq;
using Microsoft.Extensions.Logging;

namespace Agent.Application.VisionAnalysis;

public class ImageGenerationService
{
    private readonly IAiService _aiService;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly HqService _hqService;

    public ImageGenerationService(IAiService aiService, ILogger<ImageGenerationService> logger, HqService hqService)
    {
        _aiService = aiService;
        _logger = logger;
        _hqService = hqService;
    }

    public async Task<Result> CompleteTask8Async(CancellationToken ct)
    {
        _logger.LogInformation("Generating image for task 8");

        var data = await _hqService.GetTask8Data(ct);

        var result = await _aiService.GenerateImageAsync(AiModelType.Dalle3, data, AiImageSize.Square1024, AiImageQuality.Standard, ct);

        _logger.LogDebug("Sending result: {Result}", result);

        var response = await _hqService.SendReportAsync("robotid", result, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Response content: {ResponseContent}", responseContent);

        return new Result(response.IsSuccessStatusCode, responseContent);
    }
}