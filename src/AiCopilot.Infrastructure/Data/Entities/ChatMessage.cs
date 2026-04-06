namespace AiCopilot.Infrastructure.Data.Entities;

public class ChatMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ChatSessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public ChatSession ChatSession { get; set; } = null!;
}
