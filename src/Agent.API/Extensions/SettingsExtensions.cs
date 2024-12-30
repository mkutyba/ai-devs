using Agent.Application.Hq;
using Agent.Application.RobotLogin;
using Agent.Application.RobotVerifier;

namespace Agent.API.Extensions;

public static class SettingsExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RobotLoginSettings>()
            .Bind(configuration.GetSection(RobotLoginSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RobotVerifierSettings>()
            .Bind(configuration.GetSection(RobotVerifierSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<HqSettings>()
            .Bind(configuration.GetSection(HqSettings.ConfigurationKey))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}