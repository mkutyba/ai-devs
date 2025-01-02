using Agent.Application.VisionAnalysis;

namespace Agent.API.Endpoints.Task8;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task8", async (ImageGenerationService imageGenerationService, CancellationToken ct) =>
        {
            var result = await imageGenerationService.CompleteTask8Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}