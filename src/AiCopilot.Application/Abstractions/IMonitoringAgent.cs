using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IMonitoringAgent
{
    Task<MonitoringResponse> ScanAsync(MonitoringRequest request, CancellationToken cancellationToken = default);
}
