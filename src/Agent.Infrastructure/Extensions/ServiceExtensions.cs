using System.ClientModel;
using Agent.Application.Abstractions;
using Agent.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OpenAI;

namespace Agent.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAi(configuration)
            .AddKernel();

        services.AddSettings(configuration);

        return services;
    }

    private static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAiService, AiService>();

        services.ConfigureHttpClientDefaults(c =>
            c.ConfigureHttpClient(x =>
            {
                x.Timeout = TimeSpan.FromMinutes(5);
            }));

        var settings = configuration.GetSection(AiSettings.ConfigurationKey).Get<AiSettings>()!;

        foreach (var type in AiModelConfiguration.ModelTypes)
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
                    services.AddOpenAIChatCompletion(modelId, settings.OpenAI.ApiKey!, type.Key);
                    break;
                case AiProvider.OpenAIAudio:
                    services.AddOpenAIAudioToText(modelId, settings.OpenAI.ApiKey!, serviceId: type.Key.ToString());
                    break;
                case AiProvider.Ollama:
                    services.AddOllamaChatCompletion(modelId, httpClient, type.Key.ToString());
                    break;
            }
        }

        return services;
    }

    private static IServiceCollection AddOpenAIChatCompletion(this IServiceCollection services, string modelId, string apiKey, AiModelType serviceId)
    {
        var clientOptions = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromMinutes(5)
        };

        var customClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return services.AddOpenAIChatCompletion(modelId, customClient, serviceId: serviceId.ToString());
    }
}