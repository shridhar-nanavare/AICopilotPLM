using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Clients;
using AiCopilot.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiCopilot.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.AddHttpClient();
        services.AddSingleton<IAiProviderClient, AiProviderClient>();

        return services;
    }
}
