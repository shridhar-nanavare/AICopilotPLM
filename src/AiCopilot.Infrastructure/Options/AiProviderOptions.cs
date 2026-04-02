using System.ComponentModel.DataAnnotations;

namespace AiCopilot.Infrastructure.Options;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    [Required]
    public string Endpoint { get; init; } = "https://api.openai.com/";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string ChatModel { get; init; } = "gpt-4.1-mini";

    [Required]
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 60;
}
