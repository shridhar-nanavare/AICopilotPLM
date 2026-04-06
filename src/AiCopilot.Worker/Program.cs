using AiCopilot.Application.DependencyInjection;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.DependencyInjection;
using AiCopilot.Worker.Options;
using AiCopilot.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, configuration) =>
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services
    .Configure<WorkerJobOptions>(builder.Configuration.GetSection(WorkerJobOptions.SectionName))
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddSingleton<AutoOrchestrationService>()
    .AddHostedService<DailyMonitoringWorker>()
    .AddHostedService<EventTriggerWorker>();

var host = builder.Build();
await EnsureDatabaseMigratedAsync(host);
await host.RunAsync();

static async Task EnsureDatabaseMigratedAsync(IHost host)
{
    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlmDbContext>();
            await dbContext.Database.MigrateAsync();
            Log.Information("Worker database migrations applied successfully.");
            return;
        }
        catch (Exception exception) when (attempt < maxAttempts)
        {
            Log.Warning(
                exception,
                "Worker database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in 5 seconds.",
                attempt,
                maxAttempts);

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    using var finalScope = host.Services.CreateScope();
    var finalDbContext = finalScope.ServiceProvider.GetRequiredService<PlmDbContext>();
    await finalDbContext.Database.MigrateAsync();
}
