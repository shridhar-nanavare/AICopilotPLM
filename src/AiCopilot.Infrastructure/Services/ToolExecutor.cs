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

    public Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.CreatePartAsync(request, cancellationToken);

    public Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.FindDuplicateAsync(request, cancellationToken);

    public Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default) =>
        _plmMockApiClient.AnalyzeBomAsync(request, cancellationToken);
}
