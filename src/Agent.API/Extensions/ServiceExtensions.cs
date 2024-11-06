using Agent.Application.RobotLogin;
using Agent.Infrastructure.OpenAi;
using Ardalis.GuardClauses;
using Microsoft.SemanticKernel;

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

    public static void AddAi(this IServiceCollection services, ConfigurationManager configuration)
    {
        var openAiApiKey = configuration.GetSection("OpenAi:ApiKey").Value;
        Guard.Against.NullOrWhiteSpace(openAiApiKey);
        services.AddOpenAIChatCompletion("gpt-4o-mini", openAiApiKey);

        services.AddTransient(serviceProvider => new Kernel(serviceProvider));
    }
}