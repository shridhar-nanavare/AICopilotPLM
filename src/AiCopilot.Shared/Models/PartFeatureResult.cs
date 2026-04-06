namespace AiCopilot.Shared.Models;

public sealed record PartFeatureResult(
    Guid PartId,
    int UsageCount,
    double FailureRate,
    string Lifecycle,
    decimal Cost,
    DateTime UpdatedUtc);
