using System.Text.RegularExpressions;
using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Services;

internal sealed partial class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IToolExecutor _toolExecutor;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IToolExecutor toolExecutor,
        ILogger<AgentOrchestrator> logger)
    {
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var intent = DetectIntent(request.Query);
        var policy = _toolExecutor.GetExecutionPolicy(intent);

        _logger.LogInformation(
            "Agent orchestrator detected intent {Intent} with risk {RiskLevel} for query: {Query}",
            intent,
            policy.RiskLevel,
            request.Query);

        return intent switch
        {
            AgentIntent.CreatePart => await CreatePartAsync(request, policy, cancellationToken),
            AgentIntent.FindDuplicate => await FindDuplicateAsync(request.Query, policy, cancellationToken),
            AgentIntent.AnalyzeBom => await AnalyzeBomAsync(request.Query, policy, cancellationToken),
            _ => new AgentResponse(
                AgentIntent.Unknown,
                false,
                "Unable to determine intent. Supported intents are CREATE_PART, FIND_DUPLICATE, and ANALYZE_BOM.",
                RiskLevel: policy.RiskLevel)
        };
    }

    private static AgentIntent DetectIntent(string query)
    {
        var normalized = query.Trim().ToUpperInvariant();

        if (normalized.Contains("CREATE_PART", StringComparison.Ordinal) ||
            normalized.Contains("CREATE PART", StringComparison.Ordinal) ||
            normalized.Contains("ADD PART", StringComparison.Ordinal) ||
            normalized.Contains("NEW PART", StringComparison.Ordinal))
        {
            return AgentIntent.CreatePart;
        }

        if (normalized.Contains("FIND_DUPLICATE", StringComparison.Ordinal) ||
            normalized.Contains("FIND DUPLICATE", StringComparison.Ordinal) ||
            normalized.Contains("DUPLICATE", StringComparison.Ordinal))
        {
            return AgentIntent.FindDuplicate;
        }

        if (normalized.Contains("ANALYZE_BOM", StringComparison.Ordinal) ||
            normalized.Contains("ANALYZE BOM", StringComparison.Ordinal) ||
            normalized.Contains("BOM", StringComparison.Ordinal))
        {
            return AgentIntent.AnalyzeBom;
        }

        return AgentIntent.Unknown;
    }

    private async Task<AgentResponse> CreatePartAsync(
        AgentRequest request,
        ToolExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        var partNumber = ExtractPartNumber(request.Query);
        var name = ExtractPartName(request.Query);

        if (string.IsNullOrWhiteSpace(partNumber) || string.IsNullOrWhiteSpace(name))
        {
            return new AgentResponse(
                AgentIntent.CreatePart,
                false,
                "CREATE_PART requires both a part number and a name. Example: 'CREATE_PART part number PN-100 name Motor Housing'.",
                RiskLevel: policy.RiskLevel);
        }

        if (policy.RequiresApproval && !request.Approved)
        {
            return new AgentResponse(
                AgentIntent.CreatePart,
                false,
                "CREATE_PART is high risk and requires approval before execution.",
                RiskLevel: policy.RiskLevel,
                ApprovalRequired: true);
        }

        try
        {
            var result = await _toolExecutor.CreatePartAsync(
                new CreatePartRequest(partNumber, name),
                cancellationToken);

            return new AgentResponse(
                AgentIntent.CreatePart,
                true,
                $"Created part {result.PartNumber} ({result.Name}).",
                CreatedPart: result,
                RiskLevel: policy.RiskLevel);
        }
        catch (InvalidOperationException exception)
        {
            return new AgentResponse(
                AgentIntent.CreatePart,
                false,
                exception.Message,
                RiskLevel: policy.RiskLevel);
        }
    }

    private async Task<AgentResponse> FindDuplicateAsync(
        string query,
        ToolExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        var partNumber = ExtractPartNumber(query);
        var name = ExtractPartName(query);
        var searchText = string.Join(" ", [partNumber, name]).Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            searchText = RemoveIntentKeywords(query);
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new AgentResponse(
                AgentIntent.FindDuplicate,
                false,
                "FIND_DUPLICATE requires a part number, a name, or descriptive duplicate query text.",
                RiskLevel: policy.RiskLevel);
        }

        var duplicateResult = await _toolExecutor.FindDuplicateAsync(
            new FindDuplicateRequest(searchText),
            cancellationToken);

        return new AgentResponse(
            AgentIntent.FindDuplicate,
            true,
            duplicateResult.Candidates.Count == 0
                ? "No duplicate candidates were found."
                : $"Found {duplicateResult.Candidates.Count} duplicate candidate(s).",
            DuplicateResult: duplicateResult,
            RiskLevel: policy.RiskLevel);
    }

    private async Task<AgentResponse> AnalyzeBomAsync(
        string query,
        ToolExecutionPolicy policy,
        CancellationToken cancellationToken)
    {
        var queryText = ExtractPartNumber(query) ?? ExtractPartName(query) ?? RemoveIntentKeywords(query);

        if (string.IsNullOrWhiteSpace(queryText))
        {
            return new AgentResponse(
                AgentIntent.AnalyzeBom,
                false,
                "ANALYZE_BOM requires a part number or descriptive part name.",
                RiskLevel: policy.RiskLevel);
        }

        try
        {
            if (policy.RiskLevel == RiskLevel.Medium)
            {
                _logger.LogInformation("Executing MEDIUM risk tool ANALYZE_BOM for query: {Query}", queryText);
            }

            var result = await _toolExecutor.AnalyzeBomAsync(
                new AnalyzeBomRequest(queryText),
                cancellationToken);

            return new AgentResponse(
                AgentIntent.AnalyzeBom,
                true,
                $"BOM analysis for {result.PartNumber}: {result.ChildCount} child component(s), used in {result.ParentCount} parent assembly(s).",
                BomAnalysis: result,
                RiskLevel: policy.RiskLevel);
        }
        catch (InvalidOperationException exception)
        {
            return new AgentResponse(
                AgentIntent.AnalyzeBom,
                false,
                exception.Message,
                RiskLevel: policy.RiskLevel);
        }
    }

    private static string RemoveIntentKeywords(string query)
    {
        var stripped = IntentKeywordRegex().Replace(query, " ");
        return Regex.Replace(stripped, @"\s+", " ").Trim(' ', ':', '-', '.');
    }

    private static string? ExtractPartNumber(string query)
    {
        var match = PartNumberRegex().Match(query);
        if (match.Success)
        {
            return match.Groups["value"].Value.Trim().Trim('"', '\'');
        }

        return null;
    }

    private static string? ExtractPartName(string query)
    {
        var explicitMatch = PartNameRegex().Match(query);
        if (explicitMatch.Success)
        {
            var value = explicitMatch.Groups["quoted"].Success
                ? explicitMatch.Groups["quoted"].Value
                : explicitMatch.Groups["value"].Value;

            return value.Trim().Trim('"', '\'');
        }

        var createMatch = CreatePartFallbackRegex().Match(query);
        if (createMatch.Success)
        {
            return createMatch.Groups["name"].Value.Trim().Trim('"', '\'');
        }

        return null;
    }

    [GeneratedRegex(@"(?:PART\s*NUMBER|PARTNUMBER|PN)\s*[:=]?\s*(?<value>[A-Za-z0-9._/\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PartNumberRegex();

    [GeneratedRegex(@"NAME\s*[:=]?\s*(?:""(?<quoted>[^""]+)""|'(?<quoted>[^']+)'|(?<value>.+))", RegexOptions.IgnoreCase)]
    private static partial Regex PartNameRegex();

    [GeneratedRegex(@"(?:CREATE_PART|CREATE\s+PART|ADD\s+PART|NEW\s+PART)\s+(?<number>[A-Za-z0-9._/\-]+)\s+(?<name>.+)", RegexOptions.IgnoreCase)]
    private static partial Regex CreatePartFallbackRegex();

    [GeneratedRegex(@"CREATE_PART|CREATE\s+PART|ADD\s+PART|NEW\s+PART|FIND_DUPLICATE|FIND\s+DUPLICATE|ANALYZE_BOM|ANALYZE\s+BOM|DUPLICATE|BOM|FOR", RegexOptions.IgnoreCase)]
    private static partial Regex IntentKeywordRegex();
}
