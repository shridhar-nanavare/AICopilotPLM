namespace AiCopilot.Shared.Models;

public sealed record ToolExecutionPolicy(
    AgentIntent Intent,
    RiskLevel RiskLevel,
    bool RequiresApproval,
    string Description);
