using Agent.Application.Abstractions;
using Agent.Application.ArticleProcessor;
using Agent.Infrastructure.Ai;
using Agent.Infrastructure.VectorDatabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

namespace Agent.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        services
            .AddAi(builder.Configuration)
            .AddKernel()
            .AddQdrantVectorStore()
            .AddQdrantVectorStoreRecordCollection<Guid, RagRecord>("learnings");

        services.AddSettings(builder.Configuration);

        builder.AddQdrantClient("qdrant");

        return services;
    }

    private static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAiService, AiService>();
        services.AddTransient<IVectorDatabaseService, VectorDatabaseService>();

        var settings = configuration.GetSection(AiSettings.ConfigurationKey).Get<AiSettings>()!;

        foreach (var type in AiModelConfiguration.ModelTypes)
        {
            var (modelId, provider) = type.Value;
            var serviceId = type.Key.ToString();

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5),
                BaseAddress = settings.Ollama.ApiEndpoint!
            };

            switch (provider)
            {
                case AiProvider.OpenAI:
                    services.AddOpenAIChatCompletion(modelId, settings.OpenAI.ApiKey!, serviceId: serviceId);
                    break;
                case AiProvider.OpenAIAudio:
                    services.AddOpenAIAudioToText(modelId, settings.OpenAI.ApiKey!, serviceId: serviceId);
                    break;
                case AiProvider.OpenAIImage:
                    services.AddOpenAITextToImage(settings.OpenAI.ApiKey!, modelId: modelId, serviceId: serviceId);
                    break;
                case AiProvider.OpenAIEmbedding:
                    services.AddOpenAITextEmbeddingGeneration(modelId, settings.OpenAI.ApiKey!, serviceId: serviceId);
                    break;
                case AiProvider.Ollama:
                    services.AddOllamaChatCompletion(modelId, httpClient, serviceId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(provider.ToString());
            }
        }

        return services;
    }
}