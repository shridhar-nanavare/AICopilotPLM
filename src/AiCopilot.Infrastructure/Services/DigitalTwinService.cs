using System.Text.Json;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class DigitalTwinService : IDigitalTwinService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PlmDbContext _dbContext;
    private readonly IPredictionService _predictionService;

    public DigitalTwinService(PlmDbContext dbContext, IPredictionService predictionService)
    {
        _dbContext = dbContext;
        _predictionService = predictionService;
    }

    public async Task<DigitalTwinStateResult> RefreshPartStateAsync(Guid partId, CancellationToken cancellationToken = default)
    {
        var feature = await _dbContext.PartFeatures
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartId == partId, cancellationToken);

        if (feature is null)
        {
            throw new InvalidOperationException($"Part features for part {partId} were not found.");
        }

        var prediction = await _predictionService.PredictAsync(new PredictionRequest(partId), cancellationToken);
        var partHealth = prediction.FailureRisk switch
        {
            "HIGH" => "CRITICAL",
            "MEDIUM" => "WATCH",
            _ => "GOOD"
        };

        var trends = JsonSerializer.Serialize(
            new
            {
                usageTrend = feature.UsageCount switch
                {
                    >= 1000 => "HEAVY",
                    >= 250 => "RISING",
                    _ => "STABLE"
                },
                failureTrend = feature.FailureRate switch
                {
                    >= 0.20d => "DEGRADING",
                    >= 0.10d => "WATCH",
                    _ => "STABLE"
                },
                lifecycleTrend = prediction.Lifecycle,
                costBand = feature.Cost switch
                {
                    >= 10000m => "HIGH",
                    >= 1000m => "MEDIUM",
                    _ => "LOW"
                }
            },
            JsonOptions);

        var twinState = await _dbContext.Set<DigitalTwinState>()
            .SingleOrDefaultAsync(x => x.PartId == partId, cancellationToken);

        var updatedUtc = DateTime.UtcNow;

        if (twinState is null)
        {
            twinState = new DigitalTwinState
            {
                Id = Guid.NewGuid(),
                PartId = partId,
                PartHealth = partHealth,
                RiskScore = prediction.FailureRiskScore,
                Trends = trends,
                UpdatedUtc = updatedUtc
            };

            _dbContext.Set<DigitalTwinState>().Add(twinState);
        }
        else
        {
            twinState.PartHealth = partHealth;
            twinState.RiskScore = prediction.FailureRiskScore;
            twinState.Trends = trends;
            twinState.UpdatedUtc = updatedUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DigitalTwinStateResult(
            twinState.PartId,
            twinState.PartHealth,
            twinState.RiskScore,
            twinState.Trends,
            twinState.UpdatedUtc);
    }

    public async Task<IReadOnlyList<DigitalTwinStateResult>> RefreshAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var partIds = await _dbContext.PartFeatures
            .AsNoTracking()
            .Select(x => x.PartId)
            .ToListAsync(cancellationToken);

        var results = new List<DigitalTwinStateResult>(partIds.Count);

        foreach (var partId in partIds)
        {
            results.Add(await RefreshPartStateAsync(partId, cancellationToken));
        }

        return results;
    }
}
