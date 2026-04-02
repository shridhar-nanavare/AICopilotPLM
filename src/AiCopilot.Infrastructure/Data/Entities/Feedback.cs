namespace AiCopilot.Infrastructure.Data.Entities;

public class Feedback
{
    public Guid Id { get; set; }
    public Guid EmbeddingId { get; set; }
    public Guid? ChatSessionId { get; set; }
    public double Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Embedding Embedding { get; set; } = null!;
    public ChatSession? ChatSession { get; set; }
}
