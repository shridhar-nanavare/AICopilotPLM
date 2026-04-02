using AiCopilot.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Clients;

internal sealed class AiProviderClient : IAiProviderClient
{
    private readonly ILogger<AiProviderClient> _logger;

    public AiProviderClient(ILogger<AiProviderClient> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateReplyAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending prompt to external AI provider.");

        var safePrompt = userPrompt.Length > 80 ? userPrompt[..80] + "..." : userPrompt;
        var response = $"[Simulated Reply] {systemPrompt} | Prompt: {safePrompt}";

        return Task.FromResult(response);
    }
}
