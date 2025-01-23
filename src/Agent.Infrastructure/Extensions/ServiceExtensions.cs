using System.Globalization;
using Agent.Application.Abstractions.Ai;
using Agent.Application.Abstractions.GraphDatabase;
using Agent.Application.Abstractions.VectorDatabase;
using Agent.Application.ArticleProcessor;
using Agent.Infrastructure.Ai;
using Agent.Infrastructure.Ai.Plugins;
using Agent.Infrastructure.GraphDatabase;
using Agent.Infrastructure.VectorDatabase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Neo4j.Driver;
using NorthernNerds.Aspire.Neo4j;

namespace Agent.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        services
            .AddAi(builder.Configuration)
            .AddAiPlugins()
            .AddKernel()
            .AddQdrantVectorStore()
            .AddQdrantVectorStoreRecordCollection<Guid, RagRecord>("learnings");

        services.AddSettings(builder.Configuration);

        builder.AddQdrantClient("qdrant");
        services.AddTransient<IVectorDatabaseService, VectorDatabaseService>();
        if (Convert.ToBoolean(builder.Configuration.GetSection("Aspire").Value, CultureInfo.InvariantCulture))
        {
            builder.AddNeo4jClient("graph-db");
        }
        else
        {
            IDriver driver = new FakeDriver();
            builder.Services.AddSingleton(driver);
        }

        services.AddTransient<IGraphDatabaseService, GraphDatabaseService>();

        return services;
    }

    private static IServiceCollection AddKernel(this IServiceCollection services)
    {
        services.AddTransient(serviceProvider =>
        {
            KernelPluginCollection pluginCollection = serviceProvider.GetRequiredService<KernelPluginCollection>();

            return new Kernel(serviceProvider, pluginCollection);
        });

        return services;
    }

    private static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IAiService, AiService>();

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

    private static IServiceCollection AddAiPlugins(this IServiceCollection services)
    {
        services.AddTransient<HqApiDbPlugin>();

        services.AddSingleton<KernelPluginCollection>(serviceProvider =>
        [
            KernelPluginFactory.CreateFromObject(serviceProvider.GetRequiredService<HqApiDbPlugin>()),
        ]);

        return services;
    }

    private class FakeDriver : IDriver
    {
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public IAsyncSession AsyncSession()
        {
            throw new NotImplementedException();
        }

        public IAsyncSession AsyncSession(Action<SessionConfigBuilder> action)
        {
            throw new NotImplementedException();
        }

        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IServerInfo> GetServerInfoAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryVerifyConnectivityAsync()
        {
            throw new NotImplementedException();
        }

        public Task VerifyConnectivityAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SupportsMultiDbAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> SupportsSessionAuthAsync()
        {
            throw new NotImplementedException();
        }

        public IExecutableQuery<IRecord, IRecord> ExecutableQuery(string cypher)
        {
            throw new NotImplementedException();
        }

        public Task<bool> VerifyAuthenticationAsync(IAuthToken authToken)
        {
            throw new NotImplementedException();
        }

        public Config Config { get; } = null!;
        public bool Encrypted { get; }
    }
}