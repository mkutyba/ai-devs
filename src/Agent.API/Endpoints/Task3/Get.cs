using Agent.Application.JsonCompleter;

namespace Agent.API.Endpoints.Task3;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task3", async (JsonCompleterService jsonCompleterService, CancellationToken ct) =>
        {
            var result = await jsonCompleterService.CompleteTheQuestAsync(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}