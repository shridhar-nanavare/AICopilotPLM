using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class PredictionService : IPredictionService
{
    private readonly PlmDbContext _dbContext;

    public PredictionService(PlmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PredictionResult> PredictAsync(
        PredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var feature = await _dbContext.PartFeatures
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PartId == request.PartId, cancellationToken);

        if (feature is null)
        {
            throw new InvalidOperationException($"Part features for part {request.PartId} were not found.");
        }

        var reasons = new List<string>();
        var score = 0d;

        if (feature.FailureRate >= 0.20d)
        {
            score += 0.50d;
            reasons.Add("Historical failure rate is very high.");
        }
        else if (feature.FailureRate >= 0.10d)
        {
            score += 0.30d;
            reasons.Add("Historical failure rate is elevated.");
        }
        else if (feature.FailureRate >= 0.05d)
        {
            score += 0.15d;
            reasons.Add("Historical failure rate is trending above the low-risk band.");
        }

        if (feature.UsageCount >= 1000)
        {
            score += 0.25d;
            reasons.Add("Usage count is extremely high, increasing wear exposure.");
        }
        else if (feature.UsageCount >= 500)
        {
            score += 0.15d;
            reasons.Add("Usage count is high.");
        }
        else if (feature.UsageCount >= 100)
        {
            score += 0.05d;
            reasons.Add("Usage count shows the part is in regular service.");
        }

        if (feature.Cost >= 10000m)
        {
            score += 0.10d;
            reasons.Add("Part cost is high, so failures are operationally significant.");
        }
        else if (feature.Cost <= 100m)
        {
            score -= 0.05d;
            reasons.Add("Low replacement cost slightly reduces overall failure impact.");
        }

        var normalizedLifecycle = NormalizeLifecycle(feature.Lifecycle);

        if (normalizedLifecycle is "obsolete" or "retired")
        {
            score += 0.20d;
            reasons.Add("Current lifecycle indicates the part is near or beyond retirement.");
        }
        else if (normalizedLifecycle is "mature")
        {
            score += 0.05d;
            reasons.Add("Mature lifecycle suggests aging inventory.");
        }
        else if (normalizedLifecycle is "prototype")
        {
            score += 0.10d;
            reasons.Add("Prototype lifecycle implies less production stability.");
        }

        score = Math.Clamp(score, 0d, 1d);

        var failureRisk = score switch
        {
            >= 0.70d => "HIGH",
            >= 0.40d => "MEDIUM",
            _ => "LOW"
        };

        var predictedLifecycle = PredictLifecycle(normalizedLifecycle, feature.UsageCount, feature.FailureRate, feature.Cost, reasons);

        if (reasons.Count == 0)
        {
            reasons.Add("Prediction is based on baseline rule thresholds with no strong risk signals.");
        }

        return new PredictionResult(
            feature.PartId,
            failureRisk,
            predictedLifecycle,
            score,
            reasons);
    }

    private static string PredictLifecycle(
        string normalizedLifecycle,
        int usageCount,
        double failureRate,
        decimal cost,
        List<string> reasons)
    {
        if (normalizedLifecycle is "obsolete" or "retired")
        {
            reasons.Add("Lifecycle remains in the retirement band.");
            return "RETIRED";
        }

        if (failureRate >= 0.20d || usageCount >= 1000 || normalizedLifecycle is "mature")
        {
            reasons.Add("Observed usage and lifecycle signals place the part in the mature stage.");
            return "MATURE";
        }

        if (normalizedLifecycle is "prototype" or "pilot")
        {
            reasons.Add("Lifecycle state still reflects an early-stage part.");
            return "EARLY";
        }

        if (usageCount < 100 && failureRate < 0.05d && cost > 0m)
        {
            reasons.Add("Low usage and limited failures suggest the part is still ramping up.");
            return "GROWTH";
        }

        reasons.Add("Current signals suggest the part should remain active.");
        return "ACTIVE";
    }

    private static string NormalizeLifecycle(string lifecycle) =>
        lifecycle.Trim().ToLowerInvariant();
}
