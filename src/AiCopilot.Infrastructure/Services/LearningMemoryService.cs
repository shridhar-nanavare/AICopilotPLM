using System.Text.Json;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class LearningMemoryService : ILearningMemoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PlmDbContext _dbContext;

    public LearningMemoryService(PlmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LearningMemoryResult> StoreExecutionOutcomeAsync(
        string scenario,
        PlannerResponse plan,
        MultiAgentResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(response);

        var normalizedScenario = NormalizeScenario(scenario);
        var serializedPlan = JsonSerializer.Serialize(plan.Steps, JsonOptions);

        var memory = await _dbContext.Set<LearningMemory>()
            .SingleOrDefaultAsync(x => x.Scenario == normalizedScenario, cancellationToken);

        var updatedUtc = DateTime.UtcNow;
        var wasSuccessful = response.Succeeded ? 1d : 0d;
        var lastOutcome = response.FinalSummary.Trim();

        if (memory is null)
        {
            memory = new LearningMemory
            {
                Id = Guid.NewGuid(),
                Scenario = normalizedScenario,
                Plan = serializedPlan,
                SuccessRate = wasSuccessful,
                ExecutionCount = 1,
                LastOutcome = lastOutcome,
                UpdatedUtc = updatedUtc
            };

            _dbContext.Set<LearningMemory>().Add(memory);
        }
        else
        {
            var totalSuccess = (memory.SuccessRate * memory.ExecutionCount) + wasSuccessful;
            memory.ExecutionCount += 1;
            memory.SuccessRate = totalSuccess / memory.ExecutionCount;
            memory.Plan = serializedPlan;
            memory.LastOutcome = lastOutcome;
            memory.UpdatedUtc = updatedUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LearningMemoryResult(
            memory.Scenario,
            memory.Plan,
            memory.SuccessRate,
            memory.ExecutionCount,
            memory.LastOutcome,
            memory.UpdatedUtc);
    }

    private static string NormalizeScenario(string scenario) =>
        string.Join(
            ' ',
            scenario.Trim()
                .ToUpperInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
