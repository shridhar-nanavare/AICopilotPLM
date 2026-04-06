using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IAgentOrchestrator
{
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
