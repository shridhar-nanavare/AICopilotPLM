using AiCopilot.Worker.Options;
using Microsoft.Extensions.Options;

namespace AiCopilot.Worker.Services;

internal sealed class EventTriggerWorker : BackgroundService
{
    private readonly AutoOrchestrationService _autoOrchestrationService;
    private readonly WorkerJobOptions _options;
    private readonly ILogger<EventTriggerWorker> _logger;
    private DateTime _lastCheckpointUtc;

    public EventTriggerWorker(
        AutoOrchestrationService autoOrchestrationService,
        IOptions<WorkerJobOptions> options,
        ILogger<EventTriggerWorker> logger)
    {
        _autoOrchestrationService = autoOrchestrationService;
        _options = options.Value;
        _logger = logger;
        _lastCheckpointUtc = DateTime.UtcNow.AddMinutes(-Math.Max(1, _options.InitialEventLookbackMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventTriggerWorker started with polling interval {IntervalSeconds}s.", _options.EventPollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _lastCheckpointUtc = await _autoOrchestrationService.HandlePlmEventsAsync(_lastCheckpointUtc, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Event-based trigger processing failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.EventPollIntervalSeconds)), stoppingToken);
        }
    }
}
