using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Clients;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.AddHttpClient();
        services.AddSingleton<IAiProviderClient, AiProviderClient>();

        var connectionString = configuration.GetConnectionString("PlmDatabase")
            ?? "Host=localhost;Port=5432;Database=aicopilot_plm;Username=postgres;Password=postgres";

        services.AddDbContext<PlmDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        return services;
    }
}
