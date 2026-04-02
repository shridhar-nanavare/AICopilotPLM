namespace AiCopilot.Infrastructure.Options;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string Endpoint { get; init; } = "https://example-ai-provider.local";

    public string ApiKey { get; init; } = "change-me";
}
