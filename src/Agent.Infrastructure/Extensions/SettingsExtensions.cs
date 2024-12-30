using Agent.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Infrastructure.Extensions;

public static class SettingsExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiSettings>()
            .Bind(configuration.GetSection(AiSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}