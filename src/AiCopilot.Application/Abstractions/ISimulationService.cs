using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface ISimulationService
{
    Task<SimulationResult> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default);
}
