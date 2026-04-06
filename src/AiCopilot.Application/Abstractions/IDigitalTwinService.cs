using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IDigitalTwinService
{
    Task<DigitalTwinStateResult> RefreshPartStateAsync(Guid partId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DigitalTwinStateResult>> RefreshAllStatesAsync(CancellationToken cancellationToken = default);
}
