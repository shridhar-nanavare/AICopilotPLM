using System.Text.Json.Serialization;

namespace AiCopilot.Infrastructure.Clients.Models;

internal sealed record OpenAiChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiChatMessage> Messages,
    [property: JsonPropertyName("temperature")] double Temperature = 0.2);

internal sealed record OpenAiChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);
