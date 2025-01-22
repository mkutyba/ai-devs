using Agent.Application.ApiDbInteraction;

namespace Agent.API.Endpoints.Task15;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task15", async (ApiDbInteractionWithFunctionsService apiDbInteractionService, CancellationToken ct) =>
        {
            var result = await apiDbInteractionService.CompleteTask15Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}