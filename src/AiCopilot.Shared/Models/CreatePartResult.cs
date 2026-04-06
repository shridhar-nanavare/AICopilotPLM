namespace AiCopilot.Shared.Models;

public sealed record CreatePartResult(
    Guid PartId,
    string PartNumber,
    string Name);
