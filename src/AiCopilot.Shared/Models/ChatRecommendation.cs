namespace AiCopilot.Shared.Models;

public sealed record ChatRecommendation(
    Guid PartId,
    string PartNumber,
    string PartName,
    Guid DocumentId,
    string FileName,
    string StoragePath,
    string Snippet,
    double SimilarityScore,
    double RankingScore);
