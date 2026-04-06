namespace AiCopilot.Shared.Models;

public sealed record FindDuplicateResult(
    string QueryText,
    IReadOnlyList<DuplicateCandidate> Candidates);
