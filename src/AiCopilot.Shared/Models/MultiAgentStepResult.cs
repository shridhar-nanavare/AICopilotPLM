namespace AiCopilot.Shared.Models;

public sealed record MultiAgentStepResult(
    int Order,
    PlannerAgentType Agent,
    bool Succeeded,
    string Summary,
    string Output);
