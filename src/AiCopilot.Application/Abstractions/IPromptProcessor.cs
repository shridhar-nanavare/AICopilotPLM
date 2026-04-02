using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IPromptProcessor
{
    Task<PromptResponse> ProcessAsync(PromptRequest request, CancellationToken cancellationToken = default);
}
