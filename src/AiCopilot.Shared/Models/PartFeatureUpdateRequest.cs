namespace AiCopilot.Shared.Models;

public sealed record PartFeatureUpdateRequest(
    Guid PartId,
    int UsageCount,
    double FailureRate,
    string Lifecycle,
    decimal Cost);
