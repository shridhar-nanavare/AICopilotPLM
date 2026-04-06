namespace AiCopilot.Shared.Models;

public sealed record BomComponentSummary(
    Guid PartId,
    string PartNumber,
    string Name,
    decimal Quantity);
