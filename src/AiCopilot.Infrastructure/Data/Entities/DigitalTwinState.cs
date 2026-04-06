namespace AiCopilot.Infrastructure.Data.Entities;

public class DigitalTwinState : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartHealth { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public string Trends { get; set; } = "{}";
    public DateTime UpdatedUtc { get; set; }

    public Part Part { get; set; } = null!;
}
