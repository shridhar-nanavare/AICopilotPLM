using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Clients;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Options;
using AiCopilot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AiProviderOptions>()
            .Bind(configuration.GetSection(AiProviderOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<PlmApiOptions>()
            .Bind(configuration.GetSection(PlmApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IOpenAiService, OpenAiService>((serviceProvider, httpClient) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<AiProviderOptions>>()
                    .Value;

                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        services.AddHttpClient<IPlmMockApiClient, PlmMockApiClient>();

        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IPartFeatureService, PartFeatureService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IDigitalTwinService, DigitalTwinService>();
        services.AddScoped<ILearningMemoryService, LearningMemoryService>();
        services.AddScoped<ISimulationService, SimulationService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IPlmMockApiService, PlmMockApiService>();
        services.AddScoped<IPlannerAgent, PlannerAgent>();
        services.AddScoped<IToolExecutor, ToolExecutor>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddScoped<IMultiAgentOrchestrator, MultiAgentOrchestrator>();
        services.AddScoped<IMonitoringAgent, MonitoringAgent>();

        var connectionString = configuration.GetConnectionString("PlmDatabase")
            ?? "Host=localhost;Port=5432;Database=aicopilot_plm;Username=postgres;Password=postgres";

        services.AddDbContext<PlmDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        return services;
    }
}
