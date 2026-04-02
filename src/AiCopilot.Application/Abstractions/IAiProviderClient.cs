namespace AiCopilot.Application.Abstractions;

[Obsolete("Use IOpenAiService instead.")]
public interface IAiProviderClient
{
    Task<string> GenerateReplyAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken = default);
}
