namespace AiCopilot.Application.Abstractions;

public interface IAuditLogService
{
    Task WriteAsync(
        string action,
        string? agentDecision = null,
        bool? userApproval = null,
        string metadata = "{}",
        CancellationToken cancellationToken = default);
}
