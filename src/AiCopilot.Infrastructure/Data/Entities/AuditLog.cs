namespace AiCopilot.Infrastructure.Data.Entities;

public class AuditLog : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? AgentDecision { get; set; }
    public bool? UserApproval { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; }
}
