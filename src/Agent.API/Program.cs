using System.Reflection;
using Agent.API;
using Agent.API.Extensions;
using Agent.Application;
using Agent.Infrastructure.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.AddSerilog();

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddInfrastructure(builder);
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddHealthChecks()
    .AddCheck("ping", () => new HealthCheckResult(HealthStatus.Healthy));

var app = builder.Build();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Agent.API
{
    public partial class Program;
}