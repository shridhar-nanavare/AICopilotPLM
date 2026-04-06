using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;

namespace AiCopilot.Infrastructure.Services;

internal sealed class AuditLogService : IAuditLogService
{
    private readonly PlmDbContext _dbContext;
    private readonly ICurrentTenantProvider _currentTenantProvider;
    private readonly ICurrentUserProvider _currentUserProvider;

    public AuditLogService(
        PlmDbContext dbContext,
        ICurrentTenantProvider currentTenantProvider,
        ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _currentTenantProvider = currentTenantProvider;
        _currentUserProvider = currentUserProvider;
    }

    public async Task WriteAsync(
        string action,
        string? agentDecision = null,
        bool? userApproval = null,
        string metadata = "{}",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        _dbContext.Set<AuditLog>().Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = _currentTenantProvider.TenantId,
            Action = action.Trim(),
            AgentDecision = string.IsNullOrWhiteSpace(agentDecision) ? null : agentDecision.Trim(),
            UserApproval = userApproval,
            PerformedBy = _currentUserProvider.UserName,
            Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
