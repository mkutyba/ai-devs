using Agent.Application.RobotLogin;
using Agent.Infrastructure.OpenAi;

namespace Agent.API.Extensions;

public static class SettingsExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RobotLoginSettings>()
            .Bind(configuration.GetSection(RobotLoginSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OpenAiSettings>()
            .Bind(configuration.GetSection(OpenAiSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}