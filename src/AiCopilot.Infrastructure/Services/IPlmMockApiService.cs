using AiCopilot.Shared.Models;

namespace AiCopilot.Infrastructure.Services;

public interface IPlmMockApiService
{
    Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default);

    Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default);

    Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default);
}
