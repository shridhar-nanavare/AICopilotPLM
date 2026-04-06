namespace AiCopilot.Shared.Models;

public sealed record MultiAgentResponse(
    string Goal,
    AgentIntent Intent,
    bool Succeeded,
    IReadOnlyList<MultiAgentStepResult> Steps,
    string FinalSummary,
    AgentResponse? FinalResult = null,
    RiskLevel RiskLevel = RiskLevel.Low,
    bool ApprovalRequired = false);
