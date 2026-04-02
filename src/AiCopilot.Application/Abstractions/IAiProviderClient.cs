namespace AiCopilot.Application.Abstractions;

public interface IAiProviderClient
{
    Task<string> GenerateReplyAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken = default);
}
