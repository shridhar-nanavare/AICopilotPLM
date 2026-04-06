using System.Text.Json;
using System.Text.RegularExpressions;
using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Services;

internal sealed partial class MultiAgentOrchestrator : IMultiAgentOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPlannerAgent _plannerAgent;
    private readonly IToolExecutor _toolExecutor;
    private readonly ILearningMemoryService _learningMemoryService;
    private readonly ILogger<MultiAgentOrchestrator> _logger;

    public MultiAgentOrchestrator(
        IPlannerAgent plannerAgent,
        IToolExecutor toolExecutor,
        ILearningMemoryService learningMemoryService,
        ILogger<MultiAgentOrchestrator> logger)
    {
        _plannerAgent = plannerAgent;
        _toolExecutor = toolExecutor;
        _learningMemoryService = learningMemoryService;
        _logger = logger;
    }

    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var plan = await _plannerAgent.CreatePlanAsync(new PlannerRequest(request.Query), cancellationToken);
        var intent = DetectIntent(request.Query);
        var policy = _toolExecutor.GetExecutionPolicy(intent);
        var context = new ExecutionContext(request.Query, intent, request.Approved, policy);
        var stepResults = new List<MultiAgentStepResult>();

        foreach (var step in plan.Steps.OrderBy(x => x.Order))
        {
            var stepResult = step.Agent switch
            {
                PlannerAgentType.Search => await ExecuteSearchStepAsync(step, context, cancellationToken),
                PlannerAgentType.Analysis => ExecuteAnalysisStep(step, context),
                PlannerAgentType.Action => await ExecuteActionStepAsync(step, context, cancellationToken),
                _ => new MultiAgentStepResult(step.Order, step.Agent, false, "Unsupported planner agent.", "{}")
            };

            stepResults.Add(stepResult);

            if (!stepResult.Succeeded)
            {
                var response = new MultiAgentResponse(
                    plan.Goal,
                    intent,
                    false,
                    stepResults,
                    stepResult.Summary,
                    context.FinalResult,
                    policy.RiskLevel,
                    context.ApprovalRequired);

                await _learningMemoryService.StoreExecutionOutcomeAsync(request.Query, plan, response, cancellationToken);
                return response;
            }
        }

        var finalResponse = new MultiAgentResponse(
            plan.Goal,
            intent,
            true,
            stepResults,
            context.FinalResult?.Summary ?? "Plan completed successfully.",
            context.FinalResult,
            policy.RiskLevel,
            context.ApprovalRequired);

        await _learningMemoryService.StoreExecutionOutcomeAsync(request.Query, plan, finalResponse, cancellationToken);
        return finalResponse;
    }

    private async Task<MultiAgentStepResult> ExecuteSearchStepAsync(
        PlannerStep step,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (context.Intent)
            {
                case AgentIntent.CreatePart:
                {
                    var partNumber = ExtractPartNumber(context.Query);
                    var name = ExtractPartName(context.Query);
                    var queryText = string.Join(" ", [partNumber, name]).Trim();

                    if (string.IsNullOrWhiteSpace(partNumber) || string.IsNullOrWhiteSpace(name))
                    {
                        return CreateFailedStep(step, "SEARCH could not extract part number and name for CREATE_PART.");
                    }

                    var duplicateResult = await _toolExecutor.FindDuplicateAsync(
                        new FindDuplicateRequest(queryText),
                        cancellationToken);

                    context.PartNumber = partNumber;
                    context.PartName = name;
                    context.SearchResult = duplicateResult;

                    return CreateSuccessfulStep(
                        step,
                        $"SEARCH gathered {duplicateResult.Candidates.Count} duplicate candidate(s) for create-part validation.",
                        duplicateResult);
                }

                case AgentIntent.FindDuplicate:
                {
                    var queryText = ExtractPartNumber(context.Query) ?? ExtractPartName(context.Query) ?? RemoveIntentKeywords(context.Query);

                    if (string.IsNullOrWhiteSpace(queryText))
                    {
                        return CreateFailedStep(step, "SEARCH could not derive a duplicate lookup query.");
                    }

                    var duplicateResult = await _toolExecutor.FindDuplicateAsync(
                        new FindDuplicateRequest(queryText),
                        cancellationToken);

                    context.SearchResult = duplicateResult;

                    return CreateSuccessfulStep(
                        step,
                        $"SEARCH gathered {duplicateResult.Candidates.Count} duplicate candidate(s).",
                        duplicateResult);
                }

                case AgentIntent.AnalyzeBom:
                {
                    var queryText = ExtractPartNumber(context.Query) ?? ExtractPartName(context.Query) ?? RemoveIntentKeywords(context.Query);

                    if (string.IsNullOrWhiteSpace(queryText))
                    {
                        return CreateFailedStep(step, "SEARCH could not derive a BOM target.");
                    }

                    var bomResult = await _toolExecutor.AnalyzeBomAsync(
                        new AnalyzeBomRequest(queryText),
                        cancellationToken);

                    context.BomResult = bomResult;

                    return CreateSuccessfulStep(
                        step,
                        $"SEARCH gathered BOM data for {bomResult.PartNumber}.",
                        bomResult);
                }

                default:
                    return CreateFailedStep(step, "SEARCH could not determine a supported intent.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return CreateFailedStep(step, exception.Message);
        }
    }

    private MultiAgentStepResult ExecuteAnalysisStep(PlannerStep step, ExecutionContext context)
    {
        return context.Intent switch
        {
            AgentIntent.CreatePart => AnalyzeCreatePart(step, context),
            AgentIntent.FindDuplicate => AnalyzeDuplicates(step, context),
            AgentIntent.AnalyzeBom => AnalyzeBom(step, context),
            _ => CreateFailedStep(step, "ANALYSIS could not determine a supported intent.")
        };
    }

    private async Task<MultiAgentStepResult> ExecuteActionStepAsync(
        PlannerStep step,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (context.Intent)
            {
                case AgentIntent.CreatePart:
                {
                    if (context.Policy.RequiresApproval && !context.Approved)
                    {
                        context.ApprovalRequired = true;
                        context.FinalResult = new AgentResponse(
                            AgentIntent.CreatePart,
                            false,
                            "ACTION requires approval before CREATE_PART can run.",
                            RiskLevel: context.Policy.RiskLevel,
                            ApprovalRequired: true);

                        return CreateFailedStep(step, context.FinalResult.Summary);
                    }

                    if (!context.IsReadyForAction || string.IsNullOrWhiteSpace(context.PartNumber) || string.IsNullOrWhiteSpace(context.PartName))
                    {
                        return CreateFailedStep(step, context.AnalysisSummary ?? "ACTION blocked because create-part analysis did not clear execution.");
                    }

                    var createdPart = await _toolExecutor.CreatePartAsync(
                        new CreatePartRequest(context.PartNumber, context.PartName),
                        cancellationToken);

                    context.FinalResult = new AgentResponse(
                        AgentIntent.CreatePart,
                        true,
                        $"Created part {createdPart.PartNumber} ({createdPart.Name}).",
                        CreatedPart: createdPart,
                        RiskLevel: context.Policy.RiskLevel);

                    return CreateSuccessfulStep(step, context.FinalResult.Summary, createdPart);
                }

                case AgentIntent.FindDuplicate:
                {
                    var duplicateResult = context.SearchResult ?? new FindDuplicateResult(context.Query, []);
                    var summary = context.AnalysisSummary ?? "Duplicate analysis completed.";

                    context.FinalResult = new AgentResponse(
                        AgentIntent.FindDuplicate,
                        true,
                        summary,
                        DuplicateResult: duplicateResult,
                        RiskLevel: context.Policy.RiskLevel);

                    return CreateSuccessfulStep(step, summary, duplicateResult);
                }

                case AgentIntent.AnalyzeBom:
                {
                    if (context.Policy.RiskLevel == RiskLevel.Medium)
                    {
                        _logger.LogInformation("Executing MEDIUM risk ACTION step for ANALYZE_BOM query: {Query}", context.Query);
                    }

                    if (context.BomResult is null)
                    {
                        return CreateFailedStep(step, "ACTION could not complete because no BOM analysis data was available.");
                    }

                    var summary = context.AnalysisSummary ?? $"BOM analysis for {context.BomResult.PartNumber} is ready.";

                    context.FinalResult = new AgentResponse(
                        AgentIntent.AnalyzeBom,
                        true,
                        summary,
                        BomAnalysis: context.BomResult,
                        RiskLevel: context.Policy.RiskLevel);

                    return CreateSuccessfulStep(step, summary, context.BomResult);
                }

                default:
                    return CreateFailedStep(step, "ACTION could not determine a supported intent.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return CreateFailedStep(step, exception.Message);
        }
    }

    private MultiAgentStepResult AnalyzeCreatePart(PlannerStep step, ExecutionContext context)
    {
        var duplicateResult = context.SearchResult;

        if (duplicateResult is null)
        {
            return CreateFailedStep(step, "ANALYSIS could not validate create-part safety because search output was missing.");
        }

        var strongestCandidate = duplicateResult.Candidates
            .OrderByDescending(x => x.SimilarityScore)
            .ThenByDescending(x => x.RankingScore)
            .FirstOrDefault();

        if (strongestCandidate is not null && strongestCandidate.SimilarityScore >= 0.90d)
        {
            context.IsReadyForAction = false;
            context.AnalysisSummary = $"Create-part request appears to conflict with existing part {strongestCandidate.PartNumber}.";

            var output = new
            {
                safeToCreate = false,
                reason = context.AnalysisSummary,
                strongestCandidate
            };

            return CreateSuccessfulStep(step, context.AnalysisSummary, output);
        }

        context.IsReadyForAction = true;
        context.AnalysisSummary = "Create-part request passed duplicate screening and is ready for execution.";

        return CreateSuccessfulStep(
            step,
            context.AnalysisSummary,
            new
            {
                safeToCreate = true,
                candidateCount = duplicateResult.Candidates.Count
            });
    }

    private MultiAgentStepResult AnalyzeDuplicates(PlannerStep step, ExecutionContext context)
    {
        var duplicateResult = context.SearchResult;

        if (duplicateResult is null)
        {
            return CreateFailedStep(step, "ANALYSIS could not evaluate duplicates because search output was missing.");
        }

        var topCandidate = duplicateResult.Candidates
            .OrderByDescending(x => x.SimilarityScore)
            .ThenByDescending(x => x.RankingScore)
            .FirstOrDefault();

        context.AnalysisSummary = topCandidate is null
            ? "No duplicate candidates were found during analysis."
            : $"Top duplicate candidate is {topCandidate.PartNumber} with similarity {topCandidate.SimilarityScore:F2}.";

        return CreateSuccessfulStep(
            step,
            context.AnalysisSummary,
            new
            {
                candidateCount = duplicateResult.Candidates.Count,
                topCandidate
            });
    }

    private MultiAgentStepResult AnalyzeBom(PlannerStep step, ExecutionContext context)
    {
        if (context.BomResult is null)
        {
            return CreateFailedStep(step, "ANALYSIS could not summarize BOM results because search output was missing.");
        }

        context.AnalysisSummary =
            $"BOM analysis shows {context.BomResult.ChildCount} child component(s), total child quantity {context.BomResult.TotalChildQuantity}, and {context.BomResult.ParentCount} parent assembly reference(s).";

        return CreateSuccessfulStep(
            step,
            context.AnalysisSummary,
            new
            {
                context.BomResult.ChildCount,
                context.BomResult.ParentCount,
                context.BomResult.TotalChildQuantity,
                topComponents = context.BomResult.Components.Take(3).ToList()
            });
    }

    private static MultiAgentStepResult CreateSuccessfulStep(PlannerStep step, string summary, object output) =>
        new(
            step.Order,
            step.Agent,
            true,
            summary,
            JsonSerializer.Serialize(output, JsonOptions));

    private static MultiAgentStepResult CreateFailedStep(PlannerStep step, string summary) =>
        new(
            step.Order,
            step.Agent,
            false,
            summary,
            "{}");

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

    private static string RemoveIntentKeywords(string query)
    {
        var stripped = IntentKeywordRegex().Replace(query, " ");
        return Regex.Replace(stripped, @"\s+", " ").Trim(' ', ':', '-', '.');
    }

    private static string? ExtractPartNumber(string query)
    {
        var match = PartNumberRegex().Match(query);
        return match.Success ? match.Groups["value"].Value.Trim().Trim('"', '\'') : null;
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
        return createMatch.Success ? createMatch.Groups["name"].Value.Trim().Trim('"', '\'') : null;
    }

    [GeneratedRegex(@"(?:PART\s*NUMBER|PARTNUMBER|PN)\s*[:=]?\s*(?<value>[A-Za-z0-9._/\-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PartNumberRegex();

    [GeneratedRegex(@"NAME\s*[:=]?\s*(?:""(?<quoted>[^""]+)""|'(?<quoted>[^']+)'|(?<value>.+))", RegexOptions.IgnoreCase)]
    private static partial Regex PartNameRegex();

    [GeneratedRegex(@"(?:CREATE_PART|CREATE\s+PART|ADD\s+PART|NEW\s+PART)\s+(?<number>[A-Za-z0-9._/\-]+)\s+(?<name>.+)", RegexOptions.IgnoreCase)]
    private static partial Regex CreatePartFallbackRegex();

    [GeneratedRegex(@"CREATE_PART|CREATE\s+PART|ADD\s+PART|NEW\s+PART|FIND_DUPLICATE|FIND\s+DUPLICATE|ANALYZE_BOM|ANALYZE\s+BOM|DUPLICATE|BOM|FOR", RegexOptions.IgnoreCase)]
    private static partial Regex IntentKeywordRegex();

    private sealed class ExecutionContext
    {
        public ExecutionContext(string query, AgentIntent intent)
        {
            Query = query;
            Intent = intent;
        }

        public ExecutionContext(string query, AgentIntent intent, bool approved, ToolExecutionPolicy policy)
        {
            Query = query;
            Intent = intent;
            Approved = approved;
            Policy = policy;
        }

        public string Query { get; }
        public AgentIntent Intent { get; }
        public bool Approved { get; }
        public ToolExecutionPolicy Policy { get; } = new(AgentIntent.Unknown, RiskLevel.Low, false, "No policy available.");
        public bool ApprovalRequired { get; set; }
        public string? PartNumber { get; set; }
        public string? PartName { get; set; }
        public FindDuplicateResult? SearchResult { get; set; }
        public BomAnalysisResult? BomResult { get; set; }
        public bool IsReadyForAction { get; set; }
        public string? AnalysisSummary { get; set; }
        public AgentResponse? FinalResult { get; set; }
    }
}
