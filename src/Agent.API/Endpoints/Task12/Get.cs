using Agent.Application.WeaponsAnalyzer;

namespace Agent.API.Endpoints.Task12;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task12", async (WeaponsAnalyzerService weaponsAnalyzerService, CancellationToken ct) =>
        {
            var result = await weaponsAnalyzerService.CompleteTask12Async(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}