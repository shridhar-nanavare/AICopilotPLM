using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class PartFeatureService : IPartFeatureService
{
    private readonly PlmDbContext _dbContext;

    public PartFeatureService(PlmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PartFeatureResult> UpdateAsync(
        PartFeatureUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Lifecycle);

        if (request.UsageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.UsageCount), "Usage count cannot be negative.");
        }

        if (request.FailureRate < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(request.FailureRate), "Failure rate cannot be negative.");
        }

        if (request.Cost < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Cost), "Cost cannot be negative.");
        }

        var partExists = await _dbContext.Parts
            .AnyAsync(x => x.Id == request.PartId, cancellationToken);

        if (!partExists)
        {
            throw new InvalidOperationException($"Part {request.PartId} was not found.");
        }

        var feature = await _dbContext.Set<PartFeature>()
            .SingleOrDefaultAsync(x => x.PartId == request.PartId, cancellationToken);

        var utcNow = DateTime.UtcNow;

        if (feature is null)
        {
            feature = new PartFeature
            {
                Id = Guid.NewGuid(),
                PartId = request.PartId,
                UsageCount = request.UsageCount,
                FailureRate = request.FailureRate,
                Lifecycle = request.Lifecycle.Trim(),
                Cost = request.Cost,
                UpdatedUtc = utcNow
            };

            _dbContext.Set<PartFeature>().Add(feature);
        }
        else
        {
            feature.UsageCount = request.UsageCount;
            feature.FailureRate = request.FailureRate;
            feature.Lifecycle = request.Lifecycle.Trim();
            feature.Cost = request.Cost;
            feature.UpdatedUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PartFeatureResult(
            feature.PartId,
            feature.UsageCount,
            feature.FailureRate,
            feature.Lifecycle,
            feature.Cost,
            feature.UpdatedUtc);
    }
}
