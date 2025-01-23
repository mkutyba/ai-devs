using Agent.Application.DataClassifier;

namespace Agent.API.Endpoints.Task17;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task17", async (DataClassifierService dataClassifierService, CancellationToken ct) =>
        {
            var result = await dataClassifierService.CompleteTask17Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}