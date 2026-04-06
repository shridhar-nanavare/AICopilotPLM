using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IPlannerAgent
{
    Task<PlannerResponse> CreatePlanAsync(PlannerRequest request, CancellationToken cancellationToken = default);
}
