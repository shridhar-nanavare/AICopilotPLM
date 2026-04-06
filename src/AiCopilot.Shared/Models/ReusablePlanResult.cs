namespace AiCopilot.Shared.Models;

public sealed record ReusablePlanResult(
    string Scenario,
    double SuccessRate,
    double SimilarityScore,
    PlannerResponse Plan);
