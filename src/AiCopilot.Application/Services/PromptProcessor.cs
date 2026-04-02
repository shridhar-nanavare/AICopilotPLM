using AiCopilot.Application.Abstractions;
using AiCopilot.Application.Configurations;
using AiCopilot.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCopilot.Application.Services;

internal sealed class PromptProcessor : IPromptProcessor
{
    private readonly IChatService _chatService;
    private readonly CopilotOptions _options;
    private readonly ILogger<PromptProcessor> _logger;

    public PromptProcessor(
        IChatService chatService,
        IOptions<CopilotOptions> options,
        ILogger<PromptProcessor> logger)
    {
        _chatService = chatService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PromptResponse> ProcessAsync(PromptRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(request));
        }

        if (request.Prompt.Length > _options.MaxPromptLength)
        {
            throw new ArgumentException($"Prompt exceeds max length of {_options.MaxPromptLength}.", nameof(request));
        }

        _logger.LogInformation("Processing prompt for user {UserId}.", request.UserId);

        var reply = await _chatService.ProcessQueryAsync(request.Prompt, cancellationToken);

        return new PromptResponse(reply, DateTimeOffset.UtcNow);
    }
}
