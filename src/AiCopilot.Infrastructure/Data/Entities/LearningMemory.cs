namespace AiCopilot.Infrastructure.Data.Entities;

public class LearningMemory : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public string Plan { get; set; } = "[]";
    public double SuccessRate { get; set; }
    public int ExecutionCount { get; set; }
    public string LastOutcome { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; }
}
