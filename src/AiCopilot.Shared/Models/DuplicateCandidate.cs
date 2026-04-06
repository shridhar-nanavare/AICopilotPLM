namespace AiCopilot.Shared.Models;

public sealed record DuplicateCandidate(
    Guid EmbeddingId,
    Guid PartId,
    string PartNumber,
    string Name,
    double SimilarityScore,
    double RankingScore);
