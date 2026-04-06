namespace AiCopilot.Infrastructure.Data.Entities;

public class ChatSession : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<Feedback> FeedbackEntries { get; set; } = new List<Feedback>();
}
