using AiCopilot.Worker.Options;
using Microsoft.Extensions.Options;

namespace AiCopilot.Worker.Services;

internal sealed class DailyMonitoringWorker : BackgroundService
{
    private readonly AutoOrchestrationService _autoOrchestrationService;
    private readonly WorkerJobOptions _options;
    private readonly ILogger<DailyMonitoringWorker> _logger;

    public DailyMonitoringWorker(
        AutoOrchestrationService autoOrchestrationService,
        IOptions<WorkerJobOptions> options,
        ILogger<DailyMonitoringWorker> logger)
    {
        _autoOrchestrationService = autoOrchestrationService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyMonitoringWorker started.");

        if (_options.RunDailyScanOnStartup)
        {
            await RunDailyScanAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRunUtc = GetNextRunUtc(DateTime.UtcNow, _options.DailyScanHourUtc);
            var delay = nextRunUtc - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            await RunDailyScanAsync(stoppingToken);
        }
    }

    private async Task RunDailyScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _autoOrchestrationService.HandleMonitoringIssuesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Daily monitoring scan failed.");
        }
    }

    private static DateTime GetNextRunUtc(DateTime nowUtc, int dailyScanHourUtc)
    {
        var nextRun = new DateTime(
            nowUtc.Year,
            nowUtc.Month,
            nowUtc.Day,
            dailyScanHourUtc,
            0,
            0,
            DateTimeKind.Utc);

        if (nextRun <= nowUtc)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun;
    }
}
