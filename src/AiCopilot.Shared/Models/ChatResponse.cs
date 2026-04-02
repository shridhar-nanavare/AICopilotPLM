namespace AiCopilot.Shared.Models;

public sealed record ChatResponse(
    Guid SessionId,
    string Summary,
    IReadOnlyList<ChatRecommendation> Recommendations);
