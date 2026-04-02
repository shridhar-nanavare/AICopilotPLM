namespace AiCopilot.Shared.Models;

public sealed record FeedbackRequest(
    Guid EmbeddingId,
    double Score,
    Guid? SessionId = null,
    string? Comment = null);
