using Agent.Application.RobotVerifier;

namespace Agent.API.Endpoints.Task2;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task2", async (IRobotVerifierService robotVerifierService, CancellationToken ct) =>
        {
            var result = await robotVerifierService.VerifyAsync(ct);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Content);
            }

            return Results.Problem(result.Content, statusCode: 500);
        });
    }
}