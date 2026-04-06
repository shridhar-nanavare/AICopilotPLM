using AiCopilot.Application.Abstractions;

namespace AiCopilot.Infrastructure.Services;

internal sealed class StaticTenantProvider : ICurrentTenantProvider
{
    public StaticTenantProvider(string tenantId)
    {
        TenantId = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();
    }

    public string TenantId { get; }
}
