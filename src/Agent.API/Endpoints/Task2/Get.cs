using Agent.Application.RobotVerifier;

namespace Agent.API.Endpoints.Task2;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task2", async (RobotVerifierService robotVerifierService, CancellationToken ct) =>
        {
            var result = await robotVerifierService.VerifyAsync(ct);

            return result.IsSuccess ? Results.Ok(result.Content) : Results.Problem(result.Content, statusCode: 500);
        });
    }
}