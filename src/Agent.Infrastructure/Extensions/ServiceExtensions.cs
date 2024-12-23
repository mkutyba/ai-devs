using Agent.Application;
using Agent.Application.Abstractions;
using Agent.Infrastructure.OpenAi;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Agent.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenAi(configuration)
            .AddKernel();

        return services;
    }

    private static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAiService, AiService>();

        var settings = configuration.GetSection(AiSettings.ConfigurationKey).Get<AiSettings>()!;

        foreach (var type in ModelConfiguration.ModelTypes)
        {
            var (modelId, provider) = type.Value;

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
                BaseAddress = settings.Ollama.ApiEndpoint!
            };

            switch (provider)
            {
                case AiProvider.OpenAI:
                    services.AddOpenAIChatCompletion(modelId, settings.OpenAI.ApiKey!, serviceId: type.Key.ToString());
                    break;

                case AiProvider.Ollama:
#pragma warning disable SKEXP0070
                    services.AddOllamaChatCompletion(modelId, httpClient, type.Key.ToString());
#pragma warning restore SKEXP0070
                    break;
            }
        }

        return services;
    }
}