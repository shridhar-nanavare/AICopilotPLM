namespace AiCopilot.Shared.Models;

public sealed record PromptResponse(string Message, DateTimeOffset ProcessedAtUtc);
