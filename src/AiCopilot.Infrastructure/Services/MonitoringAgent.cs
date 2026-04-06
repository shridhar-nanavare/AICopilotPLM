using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Services;

internal sealed class MonitoringAgent : IMonitoringAgent
{
    private readonly PlmDbContext _dbContext;
    private readonly ILogger<MonitoringAgent> _logger;

    public MonitoringAgent(PlmDbContext dbContext, ILogger<MonitoringAgent> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<MonitoringResponse> ScanAsync(MonitoringRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var issues = new List<MonitoringIssue>();

        if (request.IncludeDuplicateParts)
        {
            var partIssues = await FindDuplicatePartIssuesAsync(cancellationToken);
            issues.AddRange(partIssues);
        }

        if (request.IncludeBomIssues)
        {
            var bomIssues = await FindBomIssuesAsync(cancellationToken);
            issues.AddRange(bomIssues);
        }

        _logger.LogInformation("MonitoringAgent detected {IssueCount} issue(s).", issues.Count);

        return new MonitoringResponse(issues.Count, issues);
    }

    private async Task<IReadOnlyList<MonitoringIssue>> FindDuplicatePartIssuesAsync(CancellationToken cancellationToken)
    {
        var parts = await _dbContext.Parts
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var duplicateGroups = parts
            .GroupBy(part => NormalizeName(part.Name))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .ToList();

        return duplicateGroups
            .Select(group =>
            {
                var relatedIds = group.Select(x => x.Id).ToList();
                var partNumbers = string.Join(", ", group.Select(x => x.PartNumber));

                return new MonitoringIssue(
                    MonitoringIssueType.DuplicatePart,
                    "Potential duplicate parts detected",
                    $"Parts with similar name '{group.First().Name}' were found: {partNumbers}.",
                    relatedIds);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<MonitoringIssue>> FindBomIssuesAsync(CancellationToken cancellationToken)
    {
        var bomItems = await _dbContext.Bom
            .AsNoTracking()
            .Include(x => x.ParentPart)
            .Include(x => x.ChildPart)
            .ToListAsync(cancellationToken);

        var issues = new List<MonitoringIssue>();

        foreach (var item in bomItems)
        {
            if (item.ParentPartId == item.ChildPartId)
            {
                issues.Add(new MonitoringIssue(
                    MonitoringIssueType.BomIssue,
                    "BOM self-reference detected",
                    $"Part {item.ParentPart.PartNumber} references itself as a child component.",
                    [item.Id, item.ParentPartId]));
            }

            if (item.Quantity <= 0)
            {
                issues.Add(new MonitoringIssue(
                    MonitoringIssueType.BomIssue,
                    "BOM quantity is not positive",
                    $"BOM row for parent {item.ParentPart.PartNumber} and child {item.ChildPart.PartNumber} has quantity {item.Quantity}.",
                    [item.Id, item.ParentPartId, item.ChildPartId]));
            }

            var reverseLinkExists = bomItems.Any(other =>
                other.Id != item.Id &&
                other.ParentPartId == item.ChildPartId &&
                other.ChildPartId == item.ParentPartId);

            if (reverseLinkExists)
            {
                issues.Add(new MonitoringIssue(
                    MonitoringIssueType.BomIssue,
                    "BOM circular reference detected",
                    $"Parts {item.ParentPart.PartNumber} and {item.ChildPart.PartNumber} reference each other in BOM relationships.",
                    [item.ParentPartId, item.ChildPartId]));
            }
        }

        return issues
            .DistinctBy(issue => $"{issue.Type}:{issue.Title}:{string.Join(',', issue.RelatedIds.OrderBy(x => x))}")
            .ToList();
    }

    private static string NormalizeName(string name) =>
        string.Concat(name
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit));
}
