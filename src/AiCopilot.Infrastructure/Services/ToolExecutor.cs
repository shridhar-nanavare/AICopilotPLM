using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Clients;
using AiCopilot.Shared.Models;

namespace AiCopilot.Infrastructure.Services;

internal sealed class ToolExecutor : IToolExecutor
{
    private readonly IPlmMockApiClient _plmMockApiClient;

    public ToolExecutor(IPlmMockApiClient plmMockApiClient)
    {
        _plmMockApiClient = plmMockApiClient;
    }

    public ToolExecutionPolicy GetExecutionPolicy(AgentIntent intent) =>
        intent switch
        {
            AgentIntent.CreatePart => new ToolExecutionPolicy(
                intent,
                RiskLevel.High,
                true,
                "CREATE_PART modifies PLM data and requires explicit approval before execution."),
            AgentIntent.AnalyzeBom => new ToolExecutionPolicy(
                intent,
                RiskLevel.Medium,
                false,
                "ANALYZE_BOM is allowed to run automatically, but execution should be logged for review."),
            AgentIntent.FindDuplicate => new ToolExecutionPolicy(
                intent,
                RiskLevel.Low,
                false,
                "FIND_DUPLICATE is read-only and can run automatically."),
            _ => new ToolExecutionPolicy(
                intent,
                RiskLevel.Low,
                false,
                "Unknown tools do not have a special execution policy.")
        };

    public Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.CreatePartAsync(request, cancellationToken);

    public Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.FindDuplicateAsync(request, cancellationToken);

    public Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.AnalyzeBomAsync(request, cancellationToken);
}
