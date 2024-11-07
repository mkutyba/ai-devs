using Agent.API.Extensions;

namespace Agent.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddOpenApi()
            .AddSettings(configuration);
    }
}