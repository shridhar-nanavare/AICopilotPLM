using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface ILearningMemoryService
{
    Task<LearningMemoryResult> StoreExecutionOutcomeAsync(
        string scenario,
        PlannerResponse plan,
        MultiAgentResponse response,
        CancellationToken cancellationToken = default);
}
