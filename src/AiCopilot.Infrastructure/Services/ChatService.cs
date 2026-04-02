using System.Text.Json;
using AiCopilot.Application.Abstractions;
using AiCopilot.Application.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCopilot.Infrastructure.Services;

internal sealed class ChatService : IChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IOpenAiService _openAiService;
    private readonly ISearchService _searchService;
    private readonly CopilotOptions _options;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IOpenAiService openAiService,
        ISearchService searchService,
        IOptions<CopilotOptions> options,
        ILogger<ChatService> logger)
    {
        _openAiService = openAiService;
        _searchService = searchService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var results = await _searchService.SearchAsync(
            query,
            _options.SearchTopResults,
            cancellationToken: cancellationToken);

        if (results.Count == 0)
        {
            return SerializeSafeResponse(
                answer: "I do not have enough grounded context to answer this question.",
                grounded: false,
                citations: []);
        }

        var context = results.Select((result, index) => new ContextItem(
            Id: $"ctx-{index + 1}",
            result.PartId,
            result.PartNumber,
            result.PartName,
            result.DocumentId,
            result.FileName,
            result.StoragePath,
            result.ChunkText,
            result.SimilarityScore,
            result.RankingScore)).ToList();

        var userPrompt = BuildUserPrompt(query, context);

        var modelResponse = await _openAiService.Chat(
            userPrompt,
            BuildSystemPrompt(),
            cancellationToken);

        return ValidateOrFallback(modelResponse, context);
    }

    private string BuildSystemPrompt()
    {
        return
            """
            You are a grounded PLM copilot.
            Answer only from the provided context.
            Do not use outside knowledge, guess, infer unstated facts, or fabricate part/document details.
            If the context is insufficient, say so clearly.
            Return valid JSON only.
            The JSON schema is:
            {
              "answer": "string",
              "grounded": true,
              "citations": [
                {
                  "contextId": "string",
                  "partId": "guid",
                  "partNumber": "string",
                  "documentId": "guid",
                  "storagePath": "string",
                  "snippet": "string"
                }
              ]
            }
            Rules:
            - Every factual statement in answer must be supported by at least one citation.
            - grounded must be false when the context is insufficient.
            - When grounded is false, answer must explain that the context is insufficient.
            - citations must reference only context items that were provided.
            - Do not include markdown, prose before JSON, or code fences.
            """;
    }

    private static string BuildUserPrompt(string query, IReadOnlyList<ContextItem> context)
    {
        var payload = new
        {
            query,
            context
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private string ValidateOrFallback(string modelResponse, IReadOnlyList<ContextItem> context)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ChatResponsePayload>(modelResponse, JsonOptions);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Answer))
            {
                throw new InvalidOperationException("Chat response payload was empty.");
            }

            var validContextIds = context
                .Select(x => x.Id)
                .ToHashSet(StringComparer.Ordinal);

            var validatedCitations = new List<ChatCitationPayload>();

            if (payload.Citations is not null)
            {
                foreach (var citation in payload.Citations)
                {
                    if (citation is null || string.IsNullOrWhiteSpace(citation.ContextId))
                    {
                        continue;
                    }

                    if (!validContextIds.Contains(citation.ContextId))
                    {
                        _logger.LogWarning("Discarding ungrounded citation referencing unknown context id {ContextId}.", citation.ContextId);
                        continue;
                    }

                    validatedCitations.Add(citation);
                }
            }

            var grounded = payload.Grounded && validatedCitations.Count > 0;

            if (!grounded)
            {
                return SerializeSafeResponse(
                    answer: "I do not have enough grounded context to answer this question.",
                    grounded: false,
                    citations: []);
            }

            var normalized = new ChatResponsePayload(
                payload.Answer.Trim(),
                true,
                validatedCitations);

            return JsonSerializer.Serialize(normalized, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Model returned invalid JSON. Falling back to safe response.");
            return SerializeSafeResponse(
                answer: "I do not have enough grounded context to answer this question.",
                grounded: false,
                citations: []);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Model returned an invalid grounded response. Falling back to safe response.");
            return SerializeSafeResponse(
                answer: "I do not have enough grounded context to answer this question.",
                grounded: false,
                citations: []);
        }
    }

    private static string SerializeSafeResponse(
        string answer,
        bool grounded,
        IReadOnlyList<ChatCitationPayload> citations)
    {
        return JsonSerializer.Serialize(
            new ChatResponsePayload(answer, grounded, citations),
            JsonOptions);
    }

    private sealed record ContextItem(
        string Id,
        Guid PartId,
        string PartNumber,
        string PartName,
        Guid DocumentId,
        string FileName,
        string StoragePath,
        string Snippet,
        double SimilarityScore,
        double RankingScore);

    private sealed record ChatResponsePayload(
        string Answer,
        bool Grounded,
        IReadOnlyList<ChatCitationPayload>? Citations);

    private sealed record ChatCitationPayload(
        string ContextId,
        Guid PartId,
        string PartNumber,
        Guid DocumentId,
        string StoragePath,
        string Snippet);
}
