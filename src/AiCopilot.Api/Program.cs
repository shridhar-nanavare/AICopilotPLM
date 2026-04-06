using AiCopilot.Api.Extensions;
using AiCopilot.Application.DependencyInjection;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlmDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Applying database migrations for the API service.");
    dbContext.Database.Migrate();
    logger.LogInformation("Database migrations completed for the API service.");
}

if (builder.Configuration.GetValue<bool>("RUN_MIGRATIONS_ONLY"))
{
    Log.Information("RUN_MIGRATIONS_ONLY is enabled. Exiting after successful migration.");
    return;
}

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program;
