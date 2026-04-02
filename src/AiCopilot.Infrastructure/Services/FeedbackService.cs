using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Services;

internal sealed class FeedbackService : IFeedbackService
{
    private readonly PlmDbContext _dbContext;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(PlmDbContext dbContext, ILogger<FeedbackService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<FeedbackResponse> SubmitAsync(FeedbackRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Score is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Score), "Feedback score must be between 0 and 1.");
        }

        var embedding = await _dbContext.Embeddings
            .FirstOrDefaultAsync(x => x.Id == request.EmbeddingId, cancellationToken)
            ?? throw new InvalidOperationException($"Embedding '{request.EmbeddingId}' was not found.");

        if (request.SessionId.HasValue)
        {
            var sessionExists = await _dbContext.ChatSessions
                .AnyAsync(x => x.Id == request.SessionId.Value, cancellationToken);

            if (!sessionExists)
            {
                throw new InvalidOperationException($"Chat session '{request.SessionId.Value}' was not found.");
            }
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            EmbeddingId = request.EmbeddingId,
            ChatSessionId = request.SessionId,
            Score = request.Score,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };

        _dbContext.Feedback.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        embedding.FeedbackScore = await _dbContext.Feedback
            .Where(x => x.EmbeddingId == request.EmbeddingId)
            .AverageAsync(x => x.Score, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stored feedback {FeedbackId} for embedding {EmbeddingId}. Aggregated score is now {FeedbackScore}.",
            feedback.Id,
            request.EmbeddingId,
            embedding.FeedbackScore);

        return new FeedbackResponse(
            feedback.Id,
            request.EmbeddingId,
            embedding.FeedbackScore,
            DateTimeOffset.UtcNow);
    }
}
