using AiCopilot.Application.Abstractions;
using AiCopilot.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AiCopilot.Infrastructure.Services;

internal sealed class PlannerAgent : IPlannerAgent
{
    private readonly ILearningMemoryService _learningMemoryService;
    private readonly ILogger<PlannerAgent> _logger;

    public PlannerAgent(
        ILearningMemoryService learningMemoryService,
        ILogger<PlannerAgent> logger)
    {
        _learningMemoryService = learningMemoryService;
        _logger = logger;
    }

    public async Task<PlannerResponse> CreatePlanAsync(PlannerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var normalizedQuery = request.Query.Trim();

        var reusablePlan = await _learningMemoryService.FindReusablePlanAsync(normalizedQuery, cancellationToken: cancellationToken);
        if (reusablePlan is not null)
        {
            _logger.LogInformation(
                "PlannerAgent reused a stored plan for query: {Query}. MatchedScenario={Scenario} SuccessRate={SuccessRate:F2} Similarity={Similarity:F2}",
                normalizedQuery,
                reusablePlan.Scenario,
                reusablePlan.SuccessRate,
                reusablePlan.SimilarityScore);

            return reusablePlan.Plan;
        }

        var goal = BuildGoal(normalizedQuery);

        var steps = new List<PlannerStep>
        {
            new(
                1,
                PlannerAgentType.Search,
                BuildSearchObjective(normalizedQuery),
                BuildSearchOutput(normalizedQuery)),
            new(
                2,
                PlannerAgentType.Analysis,
                BuildAnalysisObjective(normalizedQuery),
                BuildAnalysisOutput(normalizedQuery)),
            new(
                3,
                PlannerAgentType.Action,
                BuildActionObjective(normalizedQuery),
                BuildActionOutput(normalizedQuery))
        };

        _logger.LogInformation("PlannerAgent created a {StepCount}-step plan for query: {Query}", steps.Count, normalizedQuery);

        return new PlannerResponse(goal, steps);
    }

    private static string BuildGoal(string query)
    {
        return $"Produce an execution plan for: {query}";
    }

    private static string BuildSearchObjective(string query)
    {
        if (ContainsAny(query, "duplicate", "find", "search"))
        {
            return "Gather candidate records, prior context, and relevant PLM entities related to the user's target.";
        }

        if (ContainsAny(query, "bom", "assembly", "component"))
        {
            return "Gather the target part, linked BOM structure, and supporting related entities from PLM.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "Gather the requested part attributes and check PLM for existing conflicting records.";
        }

        return "Gather the entities, history, and context needed to satisfy the request.";
    }

    private static string BuildSearchOutput(string query)
    {
        if (ContainsAny(query, "duplicate"))
        {
            return "A shortlist of possible duplicate parts with identifiers and similarity evidence.";
        }

        if (ContainsAny(query, "bom"))
        {
            return "The target part plus its parent/child BOM relationships.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "Validated part inputs and any detected uniqueness conflicts.";
        }

        return "A normalized working set of relevant PLM records.";
    }

    private static string BuildAnalysisObjective(string query)
    {
        if (ContainsAny(query, "duplicate"))
        {
            return "Evaluate candidate matches, rank confidence, and determine whether a duplicate risk exists.";
        }

        if (ContainsAny(query, "bom"))
        {
            return "Analyze the BOM structure, quantities, and dependencies to produce a concise technical assessment.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "Validate completeness of the requested part data and determine whether creation is safe.";
        }

        return "Interpret the retrieved context and determine the best next action.";
    }

    private static string BuildAnalysisOutput(string query)
    {
        if (ContainsAny(query, "duplicate"))
        {
            return "A decision-ready duplicate assessment with ranked candidates.";
        }

        if (ContainsAny(query, "bom"))
        {
            return "A BOM summary highlighting counts, quantities, and notable structure details.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "A readiness assessment describing whether the part can be created without conflict.";
        }

        return "A synthesized assessment of the gathered context.";
    }

    private static string BuildActionObjective(string query)
    {
        if (ContainsAny(query, "duplicate"))
        {
            return "Present the best duplicate candidates or recommend that no duplicate action is needed.";
        }

        if (ContainsAny(query, "bom"))
        {
            return "Return the final BOM analysis response in a format suitable for the user or downstream workflow.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "Create the part in PLM if validation succeeds, otherwise return a clear blocking reason.";
        }

        return "Execute or present the recommended next step based on the analysis.";
    }

    private static string BuildActionOutput(string query)
    {
        if (ContainsAny(query, "duplicate"))
        {
            return "A final response containing recommended duplicate matches and follow-up action.";
        }

        if (ContainsAny(query, "bom"))
        {
            return "A final BOM analysis result ready for consumption.";
        }

        if (ContainsAny(query, "create", "new", "add"))
        {
            return "A created part record or a structured validation failure.";
        }

        return "A final structured outcome for the request.";
    }

    private static bool ContainsAny(string query, params string[] terms) =>
        terms.Any(term => query.Contains(term, StringComparison.OrdinalIgnoreCase));
}
