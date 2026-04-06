namespace AiCopilot.Shared.Models;

public sealed record AgentResponse(
    AgentIntent Intent,
    bool Succeeded,
    string Summary,
    CreatePartResult? CreatedPart = null,
    FindDuplicateResult? DuplicateResult = null,
    BomAnalysisResult? BomAnalysis = null);
