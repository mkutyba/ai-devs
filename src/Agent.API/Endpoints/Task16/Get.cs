using Agent.Application.PhotoToolsInteraction;

namespace Agent.API.Endpoints.Task16;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task16", async (PhotoToolsInteractionService photoToolsInteractionService, CancellationToken ct) =>
        {
            var result = await photoToolsInteractionService.CompleteTask16Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}