using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class SimulationService : ISimulationService
{
    private readonly PlmDbContext _dbContext;

    public SimulationService(PlmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SimulationResult> SimulateAsync(
        SimulationRequest request,
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
        var costDelta = 0m;
        var riskScore = 0d;

        ApplyMaterialChangeRules(request.MaterialChange, feature.Cost, reasons, ref costDelta, ref riskScore);
        ApplyPartChangeRules(request.PartChange, feature.Cost, feature.FailureRate, reasons, ref costDelta, ref riskScore);

        var estimatedCost = Math.Max(0m, feature.Cost + costDelta);
        riskScore = Math.Clamp(riskScore, 0d, 1d);

        var risk = riskScore switch
        {
            >= 0.70d => "HIGH",
            >= 0.40d => "MEDIUM",
            _ => "LOW"
        };

        var recommendation = BuildRecommendation(risk, request.MaterialChange, request.PartChange, costDelta, reasons);

        if (reasons.Count == 0)
        {
            reasons.Add("No strong simulation signals were found, so the baseline part profile remains the best guide.");
        }

        return new SimulationResult(
            request.PartId,
            feature.Cost,
            costDelta,
            estimatedCost,
            risk,
            recommendation,
            reasons);
    }

    private static void ApplyMaterialChangeRules(
        string? materialChange,
        decimal baseCost,
        List<string> reasons,
        ref decimal costDelta,
        ref double riskScore)
    {
        if (string.IsNullOrWhiteSpace(materialChange))
        {
            return;
        }

        var normalized = materialChange.Trim().ToLowerInvariant();

        if (normalized.Contains("steel", StringComparison.Ordinal))
        {
            costDelta += Math.Max(50m, baseCost * 0.08m);
            riskScore -= 0.05d;
            reasons.Add("Steel upgrade slightly increases cost but usually improves durability.");
        }

        if (normalized.Contains("aluminum", StringComparison.Ordinal) || normalized.Contains("aluminium", StringComparison.Ordinal))
        {
            costDelta += Math.Max(30m, baseCost * 0.05m);
            riskScore += 0.05d;
            reasons.Add("Aluminum change can reduce weight, but may introduce moderate durability tradeoffs.");
        }

        if (normalized.Contains("plastic", StringComparison.Ordinal) || normalized.Contains("polymer", StringComparison.Ordinal))
        {
            costDelta -= Math.Max(20m, baseCost * 0.10m);
            riskScore += 0.25d;
            reasons.Add("Plastic or polymer substitution lowers cost, but raises failure risk for structural applications.");
        }

        if (normalized.Contains("composite", StringComparison.Ordinal))
        {
            costDelta += Math.Max(100m, baseCost * 0.12m);
            riskScore += 0.10d;
            reasons.Add("Composite material raises cost and may require validation before adoption.");
        }
    }

    private static void ApplyPartChangeRules(
        string? partChange,
        decimal baseCost,
        double failureRate,
        List<string> reasons,
        ref decimal costDelta,
        ref double riskScore)
    {
        if (string.IsNullOrWhiteSpace(partChange))
        {
            return;
        }

        var normalized = partChange.Trim().ToLowerInvariant();

        if (normalized.Contains("simplify", StringComparison.Ordinal) || normalized.Contains("reduce components", StringComparison.Ordinal))
        {
            costDelta -= Math.Max(25m, baseCost * 0.07m);
            riskScore -= 0.05d;
            reasons.Add("Simplifying the part can reduce manufacturing cost and assembly complexity.");
        }

        if (normalized.Contains("tight tolerance", StringComparison.Ordinal) || normalized.Contains("precision", StringComparison.Ordinal))
        {
            costDelta += Math.Max(75m, baseCost * 0.10m);
            riskScore -= failureRate >= 0.10d ? 0.05d : 0d;
            reasons.Add("Tighter tolerances increase production cost, but can help reliability on unstable parts.");
        }

        if (normalized.Contains("lighter", StringComparison.Ordinal) || normalized.Contains("thin", StringComparison.Ordinal))
        {
            costDelta -= Math.Max(15m, baseCost * 0.04m);
            riskScore += 0.10d;
            reasons.Add("Weight reduction may save material cost, but can reduce safety margin.");
        }

        if (normalized.Contains("redesign", StringComparison.Ordinal) || normalized.Contains("major change", StringComparison.Ordinal))
        {
            costDelta += Math.Max(150m, baseCost * 0.15m);
            riskScore += 0.20d;
            reasons.Add("A major redesign introduces validation risk and upfront engineering cost.");
        }
    }

    private static string BuildRecommendation(
        string risk,
        string? materialChange,
        string? partChange,
        decimal costDelta,
        List<string> reasons)
    {
        if (risk == "HIGH")
        {
            reasons.Add("High simulated risk suggests additional review before implementation.");
            return "Do not apply automatically. Run engineering review and validation testing first.";
        }

        if (risk == "MEDIUM")
        {
            return costDelta <= 0m
                ? "Proceed with a controlled pilot and monitor quality closely."
                : "Proceed only after cost-benefit review and limited validation testing.";
        }

        if (!string.IsNullOrWhiteSpace(materialChange) || !string.IsNullOrWhiteSpace(partChange))
        {
            return costDelta <= 0m
                ? "Change looks favorable. Consider moving forward with standard validation."
                : "Change appears viable, but confirm the added cost is justified.";
        }

        return "No meaningful change was provided, so keep the current design baseline.";
    }
}
