namespace AiCopilot.Shared.Models;

public sealed record SimulationResult(
    Guid PartId,
    decimal BaseCost,
    decimal EstimatedCostImpact,
    decimal EstimatedCost,
    string Risk,
    string Recommendation,
    IReadOnlyList<string> Reasons);
