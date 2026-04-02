namespace AiCopilot.Infrastructure.Data.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatSessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public ChatSession ChatSession { get; set; } = null!;
}
