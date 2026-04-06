namespace AiCopilot.Shared.Models;

public sealed record MonitoringRequest(bool IncludeDuplicateParts = true, bool IncludeBomIssues = true);
