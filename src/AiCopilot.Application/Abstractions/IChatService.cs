using AiCopilot.Shared.Models;

namespace AiCopilot.Application.Abstractions;

public interface IChatService
{
    Task<ChatResponse> ProcessQueryAsync(string query, CancellationToken cancellationToken = default);
}
