using Agent.Application.ArticleProcessor;

namespace Agent.API.Endpoints.Task10;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task10", async (ArticleProcessorService articleProcessorService, CancellationToken ct) =>
        {
            var result = await articleProcessorService.CompleteTask10Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}