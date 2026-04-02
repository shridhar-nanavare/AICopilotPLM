using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IFeedbackService
{
    Task<FeedbackResponse> SubmitAsync(FeedbackRequest request, CancellationToken cancellationToken = default);
}
