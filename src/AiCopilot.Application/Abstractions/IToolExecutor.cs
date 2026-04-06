using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IToolExecutor
{
    ToolExecutionPolicy GetExecutionPolicy(AgentIntent intent);

    Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default);

    Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default);

    Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default);
}
