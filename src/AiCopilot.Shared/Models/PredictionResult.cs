namespace AiCopilot.Shared.Models;

public sealed record PredictionResult(
    Guid PartId,
    string FailureRisk,
    string Lifecycle,
    double FailureRiskScore,
    IReadOnlyList<string> Reasons);
