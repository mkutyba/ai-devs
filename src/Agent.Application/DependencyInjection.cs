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
        services.AddTransient<IRobotLoginService, RobotLoginService>();
        services.AddTransient<IRobotVerifierService, RobotVerifierService>();
        services.AddTransient<IJsonCompleterService, JsonCompleterService>();

        return services;
    }
}