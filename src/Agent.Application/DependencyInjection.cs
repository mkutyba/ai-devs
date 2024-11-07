using Agent.Application.RobotLogin;
using Microsoft.Extensions.DependencyInjection;

namespace Agent.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddTransient<IRobotLoginService, RobotLoginService>();

        return services;
    }
}