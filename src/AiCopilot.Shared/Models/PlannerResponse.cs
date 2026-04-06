namespace AiCopilot.Shared.Models;

public sealed record PlannerResponse(
    string Goal,
    IReadOnlyList<PlannerStep> Steps);
