using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IMultiAgentOrchestrator
{
    Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request, CancellationToken cancellationToken = default);
}
