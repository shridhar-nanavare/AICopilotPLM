namespace AiCopilot.Infrastructure.Data.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<Feedback> FeedbackEntries { get; set; } = new List<Feedback>();
}
