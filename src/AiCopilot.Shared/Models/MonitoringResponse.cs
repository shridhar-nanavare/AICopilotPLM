namespace AiCopilot.Shared.Models;

public sealed record MonitoringResponse(
    int TotalIssues,
    IReadOnlyList<MonitoringIssue> Issues);
