using Pgvector;

namespace AiCopilot.Infrastructure.Data.Entities;

public class Embedding
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public Vector Vector { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }

    public Document Document { get; set; } = null!;
}
