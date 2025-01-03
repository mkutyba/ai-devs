using Agent.Application.InformationClassifier;

namespace Agent.API.Endpoints.Task9;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task9", async (InformationClassifierService informationClassifierService, CancellationToken ct) =>
        {
            var result = await informationClassifierService.CompleteTask9Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}