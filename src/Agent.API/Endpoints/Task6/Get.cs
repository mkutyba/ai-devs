using Agent.Application.InformationExtractor;

namespace Agent.API.Endpoints.Task6;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task6", async (InformationExtractorService informationExtractorService, CancellationToken ct) =>
        {
            var result = await informationExtractorService.CompleteTask6Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}