using AiCopilot.Application.DependencyInjection;
using AiCopilot.Infrastructure.DependencyInjection;
using AiCopilot.Worker.Options;
using AiCopilot.Worker.Services;
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
host.Run();
