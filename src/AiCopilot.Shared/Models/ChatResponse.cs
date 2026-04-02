namespace AiCopilot.Shared.Models;

public sealed record ChatResponse(
    string Summary,
    IReadOnlyList<ChatRecommendation> Recommendations);
