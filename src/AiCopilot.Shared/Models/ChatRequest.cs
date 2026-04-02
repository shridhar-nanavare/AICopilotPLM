namespace AiCopilot.Shared.Models;

public sealed record ChatRequest(Guid? SessionId, string Query);
