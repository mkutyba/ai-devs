using Agent.Application.RobotLogin;

namespace Agent.API.Endpoints.Task1;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/task1", async (IRobotLoginService robotLoginService, CancellationToken ct) =>
        {
            var result = await robotLoginService.PerformLoginAsync(ct);

            if (result.IsSuccess)
            {
                return Results.Ok(result.Content);
            }

            return Results.Problem(result.Content, statusCode: 500);
        });
    }
}