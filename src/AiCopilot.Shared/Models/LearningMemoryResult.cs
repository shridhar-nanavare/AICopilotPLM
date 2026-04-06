namespace AiCopilot.Shared.Models;

public sealed record LearningMemoryResult(
    string Scenario,
    string Plan,
    double SuccessRate,
    int ExecutionCount,
    string LastOutcome,
    DateTime UpdatedUtc);
