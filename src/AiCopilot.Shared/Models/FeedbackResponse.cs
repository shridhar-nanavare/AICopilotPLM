namespace AiCopilot.Shared.Models;

public sealed record FeedbackResponse(
    Guid FeedbackId,
    Guid EmbeddingId,
    double FeedbackScore,
    DateTimeOffset SubmittedAtUtc);
