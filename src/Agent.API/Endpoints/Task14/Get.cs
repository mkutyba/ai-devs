using Agent.Application.ApiDbInteraction;

namespace Agent.API.Endpoints.Task14;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task14", async (ApiDbInteractionWithFunctionsService apiDbInteractionService, CancellationToken ct) =>
        {
            var result = await apiDbInteractionService.CompleteTask14Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}