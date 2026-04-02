using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IChatService
{
    Task<ChatResponse> ProcessQueryAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
