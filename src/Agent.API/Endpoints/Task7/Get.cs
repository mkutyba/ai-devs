using Agent.Application.VisionAnalysis;

namespace Agent.API.Endpoints.Task7;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task7", async (MapAnalysisService mapAnalysisService, CancellationToken ct) =>
        {
            var result = await mapAnalysisService.CompleteTask7Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}