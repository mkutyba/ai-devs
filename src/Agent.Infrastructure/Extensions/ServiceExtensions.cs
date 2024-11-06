using Ardalis.GuardClauses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agent.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static void AddOpenAi(this IServiceCollection services, ConfigurationManager configuration)
    {
        var openAiApiKey = configuration.GetSection("OpenAi:ApiKey").Value;
        Guard.Against.NullOrWhiteSpace(openAiApiKey);
        services.AddOpenAIChatCompletion("gpt-4o-mini-2024-07-18", openAiApiKey);

        services.AddTransient(serviceProvider => new Kernel(serviceProvider));
    }
}