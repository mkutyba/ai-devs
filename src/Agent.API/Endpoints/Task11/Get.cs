using Agent.Application.FactAnalyzer;

namespace Agent.API.Endpoints.Task11;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task11", async (FactAnalyzerService factAnalyzerService, CancellationToken ct) =>
        {
            var result = await factAnalyzerService.CompleteTask11Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}