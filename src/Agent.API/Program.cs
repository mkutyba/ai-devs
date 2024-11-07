using System.Reflection;
using Agent.API;
using Agent.API.Extensions;
using Agent.Application;
using Agent.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Agent.API
{
    public partial class Program;
}