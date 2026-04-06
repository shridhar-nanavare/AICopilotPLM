namespace AiCopilot.Infrastructure.Data.Entities;

public class PartFeature
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int UsageCount { get; set; }
    public double FailureRate { get; set; }
    public string Lifecycle { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public Part Part { get; set; } = null!;
}
