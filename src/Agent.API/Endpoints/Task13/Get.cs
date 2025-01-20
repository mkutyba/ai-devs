using Agent.Application.ApiDbInteraction;

namespace Agent.API.Endpoints.Task13;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task13", async (ApiDbInteractionService apiDbInteractionService, CancellationToken ct) =>
        {
            var result = await apiDbInteractionService.CompleteTask13Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });

        app.MapGet("/task13fn", async (ApiDbInteractionWithFunctionsService apiDbInteractionService, CancellationToken ct) =>
        {
            var result = await apiDbInteractionService.CompleteTask13Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}