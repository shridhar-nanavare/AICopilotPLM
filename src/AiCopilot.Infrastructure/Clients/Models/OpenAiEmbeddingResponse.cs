using System.Text.Json.Serialization;

namespace AiCopilot.Infrastructure.Clients.Models;

internal sealed record OpenAiEmbeddingResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<OpenAiEmbeddingData>? Data);

internal sealed record OpenAiEmbeddingData(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("embedding")] IReadOnlyList<float>? Embedding);
