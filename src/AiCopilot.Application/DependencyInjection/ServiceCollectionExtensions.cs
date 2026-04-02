using AiCopilot.Application.Abstractions;
using AiCopilot.Application.Configurations;
using AiCopilot.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiCopilot.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CopilotOptions>(configuration.GetSection(CopilotOptions.SectionName));
        services.AddScoped<IPromptProcessor, PromptProcessor>();

        return services;
    }
}
