namespace AiCopilot.Shared.Models;

public sealed record BomAnalysisResult(
    Guid PartId,
    string PartNumber,
    string Name,
    int ChildCount,
    int ParentCount,
    decimal TotalChildQuantity,
    IReadOnlyList<BomComponentSummary> Components);
