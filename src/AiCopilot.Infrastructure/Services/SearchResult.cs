namespace AiCopilot.Infrastructure.Services;

public sealed record SearchResult(
    Guid EmbeddingId,
    Guid DocumentId,
    Guid PartId,
    string PartNumber,
    string PartName,
    string FileName,
    string ContentType,
    string StoragePath,
    string ChunkText,
    double SimilarityScore,
    double FeedbackScore,
    int UsageCount,
    DateTime? LastUsed,
    double RankingScore);
