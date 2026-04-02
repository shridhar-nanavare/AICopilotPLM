using System.Text.Json.Serialization;

namespace AiCopilot.Infrastructure.Clients.Models;

internal sealed record OpenAiEmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("input")] IReadOnlyList<string> Input);
