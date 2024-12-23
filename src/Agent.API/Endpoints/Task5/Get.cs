using Agent.Application.Censor;

namespace Agent.API.Endpoints.Task5;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task5", async (CensorService censorService, CancellationToken ct) =>
        {
            var result = await censorService.CompleteTheTaskAsync(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}