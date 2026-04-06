namespace AiCopilot.Shared.Models;

public sealed record MonitoringIssue(
    MonitoringIssueType Type,
    string Title,
    string Description,
    IReadOnlyList<Guid> RelatedIds);
