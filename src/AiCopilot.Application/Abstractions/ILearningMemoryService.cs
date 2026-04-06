using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface ILearningMemoryService
{
    Task<ReusablePlanResult?> FindReusablePlanAsync(
        string scenario,
        double minimumSuccessRate = 0.80d,
        CancellationToken cancellationToken = default);

    Task<LearningMemoryResult> StoreExecutionOutcomeAsync(
        string scenario,
        PlannerResponse plan,
        MultiAgentResponse response,
        CancellationToken cancellationToken = default);
}
