namespace AiCopilot.Application.Abstractions;

public interface IChatService
{
    Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default);
}
