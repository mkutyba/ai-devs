using Agent.API.Extensions;
using Agent.Application.RobotLogin;
using Agent.Infrastructure.OpenAi;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.ClearProviders().AddSerilog();

builder.Services.AddOpenApi();
builder.Services.RegisterSettings(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddTransient<IOpenAiService, OpenAiService>();
builder.Services.AddTransient<IRobotLoginService, RobotLoginService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/task1", async ([FromServices] IRobotLoginService robotLoginService, CancellationToken ct) =>
{
    var result = await robotLoginService.PerformLoginAsync(ct);

    if (result.IsSuccess)
    {
        return Results.Ok(result.Content);
    }

    return Results.Problem(result.Content, statusCode: 500);
});

app.Run();