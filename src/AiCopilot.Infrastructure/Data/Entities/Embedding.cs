using Pgvector;

namespace AiCopilot.Infrastructure.Data.Entities;

public class Embedding : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public Vector Vector { get; set; } = null!;
    public double FeedbackScore { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsed { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Document Document { get; set; } = null!;
    public ICollection<Feedback> FeedbackEntries { get; set; } = new List<Feedback>();
}
