namespace AiCopilot.Shared.Models;

public sealed record DigitalTwinStateResult(
    Guid PartId,
    string PartHealth,
    double RiskScore,
    string Trends,
    DateTime UpdatedUtc);
