using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IPartFeatureService
{
    Task<PartFeatureResult> UpdateAsync(PartFeatureUpdateRequest request, CancellationToken cancellationToken = default);
}
