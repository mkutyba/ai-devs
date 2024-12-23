using Agent.Application.Ai;
using Agent.Application.Censor;
using Agent.Application.Hq;
using Agent.Application.JsonCompleter;
using Agent.Application.RobotLogin;
using Agent.Application.RobotVerifier;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<RobotLoginService>();
        services.AddTransient<RobotVerifierService>();
        services.AddTransient<JsonCompleterService>();
        services.AddTransient<HqService>();
        services.AddTransient<IAiSimpleAnswerService, AiSimpleAnswerService>();
        services.AddTransient<CensorService>();

        return services;
    }
}