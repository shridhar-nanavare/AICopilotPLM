using System.Text.Json.Serialization;

namespace AiCopilot.Infrastructure.Clients.Models;

internal sealed record OpenAiChatResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChatChoice>? Choices);

internal sealed record OpenAiChatChoice(
    [property: JsonPropertyName("message")] OpenAiChatMessage? Message);
