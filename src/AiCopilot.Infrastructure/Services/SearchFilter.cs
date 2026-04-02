namespace AiCopilot.Infrastructure.Services;

public sealed record SearchFilter(
    Guid? PartId = null,
    IReadOnlyCollection<Guid>? PartIds = null,
    string? PartNumber = null,
    string? PartNameContains = null,
    string? FileName = null,
    string? ContentType = null,
    string? StoragePathPrefix = null);
