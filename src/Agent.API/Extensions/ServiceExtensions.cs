using Agent.Application.RobotLogin;
using Agent.Infrastructure.OpenAi;

namespace Agent.API.Extensions;

public static class ServiceExtensions
{
    public static void RegisterSettings(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOptions<RobotLoginSettings>()
            .Bind(configuration.GetSection(RobotLoginSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OpenAiSettings>()
            .Bind(configuration.GetSection(OpenAiSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}