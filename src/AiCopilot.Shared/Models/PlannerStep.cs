namespace AiCopilot.Shared.Models;

public sealed record PlannerStep(
    int Order,
    PlannerAgentType Agent,
    string Objective,
    string ExpectedOutput);
