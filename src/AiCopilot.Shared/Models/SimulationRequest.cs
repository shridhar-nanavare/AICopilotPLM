namespace AiCopilot.Shared.Models;

public sealed record SimulationRequest(
    Guid PartId,
    string? MaterialChange = null,
    string? PartChange = null);
