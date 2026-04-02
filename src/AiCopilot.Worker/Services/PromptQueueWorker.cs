using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;

namespace AiCopilot.Worker.Services;

internal sealed class PromptQueueWorker : BackgroundService
{
    private readonly IPromptProcessor _promptProcessor;
    private readonly ILogger<PromptQueueWorker> _logger;

    public PromptQueueWorker(IPromptProcessor promptProcessor, ILogger<PromptQueueWorker> logger)
    {
        _promptProcessor = promptProcessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PromptQueueWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var request = new PromptRequest("system-worker", "Run scheduled health prompt");
            var response = await _promptProcessor.ProcessAsync(request, stoppingToken);

            _logger.LogInformation("Background prompt processed at {ProcessedAt}: {Message}", response.ProcessedAtUtc, response.Message);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
