using System.Text.RegularExpressions;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Worker.Services;

internal sealed partial class AutoOrchestrationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoOrchestrationService> _logger;

    public AutoOrchestrationService(IServiceScopeFactory scopeFactory, ILogger<AutoOrchestrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleMonitoringIssuesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var digitalTwinService = scope.ServiceProvider.GetRequiredService<IDigitalTwinService>();
        var monitoringAgent = scope.ServiceProvider.GetRequiredService<IMonitoringAgent>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IMultiAgentOrchestrator>();

        var refreshedStates = await digitalTwinService.RefreshAllStatesAsync(cancellationToken);
        _logger.LogInformation("Refreshed {Count} digital twin state record(s) before daily monitoring.", refreshedStates.Count);

        var response = await monitoringAgent.ScanAsync(new MonitoringRequest(), cancellationToken);

        _logger.LogInformation("Daily monitoring scan found {IssueCount} issue(s).", response.TotalIssues);

        foreach (var issue in response.Issues)
        {
            var query = BuildIssueQuery(issue);

            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogInformation("Skipping issue '{Title}' because no orchestration query could be derived.", issue.Title);
                continue;
            }

            var result = await orchestrator.ExecuteAsync(new MultiAgentRequest(query), cancellationToken);

            _logger.LogInformation(
                "Triggered orchestrator for monitoring issue '{Title}'. Success={Succeeded}. Summary={Summary}",
                issue.Title,
                result.Succeeded,
                result.FinalSummary);
        }
    }

    public async Task<DateTime> HandlePlmEventsAsync(DateTime lastCheckpointUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlmDbContext>();
        var digitalTwinService = scope.ServiceProvider.GetRequiredService<IDigitalTwinService>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IMultiAgentOrchestrator>();

        var newParts = await dbContext.Parts
            .AsNoTracking()
            .Where(x => x.CreatedUtc > lastCheckpointUtc)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var part in newParts)
        {
            if (await dbContext.PartFeatures.AsNoTracking().AnyAsync(x => x.PartId == part.Id, cancellationToken))
            {
                await digitalTwinService.RefreshPartStateAsync(part.Id, cancellationToken);
            }

            var query = $"FIND_DUPLICATE part number {part.PartNumber} name \"{part.Name}\"";
            var result = await orchestrator.ExecuteAsync(new MultiAgentRequest(query), cancellationToken);

            _logger.LogInformation(
                "Event trigger processed new part {PartNumber}. Success={Succeeded}. Summary={Summary}",
                part.PartNumber,
                result.Succeeded,
                result.FinalSummary);
        }

        var newBomItems = await dbContext.Bom
            .AsNoTracking()
            .Include(x => x.ParentPart)
            .Where(x => x.CreatedUtc > lastCheckpointUtc)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var bomItem in newBomItems)
        {
            if (await dbContext.PartFeatures.AsNoTracking().AnyAsync(x => x.PartId == bomItem.ParentPartId, cancellationToken))
            {
                await digitalTwinService.RefreshPartStateAsync(bomItem.ParentPartId, cancellationToken);
            }

            var query = $"ANALYZE_BOM {bomItem.ParentPart.PartNumber}";
            var result = await orchestrator.ExecuteAsync(new MultiAgentRequest(query), cancellationToken);

            _logger.LogInformation(
                "Event trigger processed BOM change for parent part {PartNumber}. Success={Succeeded}. Summary={Summary}",
                bomItem.ParentPart.PartNumber,
                result.Succeeded,
                result.FinalSummary);
        }

        var latestTimestamp = new[]
        {
            lastCheckpointUtc,
            newParts.Select(x => x.CreatedUtc).DefaultIfEmpty(lastCheckpointUtc).Max(),
            newBomItems.Select(x => x.CreatedUtc).DefaultIfEmpty(lastCheckpointUtc).Max()
        }.Max();

        return latestTimestamp;
    }

    private static string? BuildIssueQuery(MonitoringIssue issue)
    {
        if (issue.Type == MonitoringIssueType.DuplicatePart)
        {
            var nameMatch = DuplicateNameRegex().Match(issue.Description);
            var partName = nameMatch.Success ? nameMatch.Groups["name"].Value : null;

            return !string.IsNullOrWhiteSpace(partName)
                ? $"FIND_DUPLICATE name \"{partName}\""
                : null;
        }

        if (issue.Type == MonitoringIssueType.BomIssue)
        {
            var partNumberMatch = PartNumberRegex().Match(issue.Description);
            return partNumberMatch.Success
                ? $"ANALYZE_BOM {partNumberMatch.Groups["value"].Value}"
                : null;
        }

        return null;
    }

    [GeneratedRegex(@"'(?<name>[^']+)'")]
    private static partial Regex DuplicateNameRegex();

    [GeneratedRegex(@"(?<value>[A-Z]{1,4}[-_/]?\d[\w\-./]*)", RegexOptions.IgnoreCase)]
    private static partial Regex PartNumberRegex();
}
