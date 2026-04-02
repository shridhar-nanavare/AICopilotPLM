using AiCopilot.Application.DependencyInjection;
using AiCopilot.Infrastructure.DependencyInjection;
using AiCopilot.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddHostedService<PromptQueueWorker>();

var host = builder.Build();
host.Run();
