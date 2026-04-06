using System.Text;
using AiCopilot.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;

namespace AiCopilot.Infrastructure.Services;

internal sealed class SearchService : ISearchService
{
    private const double UsageNormalizationCap = 100d;
    private const double RecencyDecayWindowDays = 30d;

    private readonly ICurrentTenantProvider _currentTenantProvider;
    private readonly ISearchQueryExecutor _searchQueryExecutor;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        ICurrentTenantProvider currentTenantProvider,
        ISearchQueryExecutor searchQueryExecutor,
        IOpenAiService openAiService,
        ILogger<SearchService> logger)
    {
        _currentTenantProvider = currentTenantProvider;
        _searchQueryExecutor = searchQueryExecutor;
        _openAiService = openAiService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int top = 5,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(top), "Top must be greater than zero.");
        }

        var queryEmbedding = await _openAiService.CreateEmbedding(query, cancellationToken);
        var queryVector = new Vector(queryEmbedding.ToArray());

        var sql = new StringBuilder(
            """
            SELECT
                e."Id" AS embedding_id,
                e."DocumentId" AS document_id,
                d."PartId" AS part_id,
                p."PartNumber" AS part_number,
                p."Name" AS part_name,
                d."FileName" AS file_name,
                d."ContentType" AS content_type,
                d."StoragePath" AS storage_path,
                e."ChunkText" AS chunk_text,
                GREATEST(0.0, LEAST(1.0, 1 - (e."Vector" <=> @query_vector))) AS similarity_score,
                GREATEST(0.0, LEAST(1.0, e.feedback_score)) AS feedback_score,
                e.usage_count AS usage_count,
                e.last_used AS last_used,
                (
                    0.6 * GREATEST(0.0, LEAST(1.0, 1 - (e."Vector" <=> @query_vector))) +
                    0.2 * GREATEST(0.0, LEAST(1.0, e.feedback_score)) +
                    0.1 * GREATEST(
                        0.0,
                        LEAST(
                            1.0,
                            LN(1 + e.usage_count) / LN(1 + @usage_normalization_cap)
                        )
                    ) +
                    0.1 * EXP(
                        -GREATEST(
                            EXTRACT(EPOCH FROM (NOW() - COALESCE(e.last_used, e."CreatedUtc"))),
                            0
                        ) / @recency_decay_window_seconds
                    )
                ) AS ranking_score
            FROM embeddings e
            INNER JOIN documents d ON d."Id" = e."DocumentId"
            INNER JOIN parts p ON p."Id" = d."PartId"
            WHERE e.tenant_id = @tenant_id
            """);

        var parameters = new List<NpgsqlParameter>
        {
            new("tenant_id", _currentTenantProvider.TenantId),
            new("query_vector", queryVector),
            new("usage_normalization_cap", UsageNormalizationCap),
            new("recency_decay_window_seconds", RecencyDecayWindowDays * 24d * 60d * 60d)
        };

        AppendFilterSql(sql, parameters, filter);

        sql.AppendLine("ORDER BY ranking_score DESC, similarity_score DESC");
        sql.AppendLine("LIMIT @top");
        parameters.Add(new NpgsqlParameter("top", top));

        var results = await _searchQueryExecutor.ExecuteAsync(sql.ToString(), parameters, cancellationToken);

        _logger.LogInformation(
            "Search completed for query length {QueryLength} with {ResultCount} ranked results.",
            query.Length,
            results.Count);

        return results;
    }

    private static void AppendFilterSql(
        StringBuilder sql,
        List<NpgsqlParameter> parameters,
        SearchFilter? filter)
    {
        if (filter is null)
        {
            return;
        }

        if (filter.PartId.HasValue)
        {
            sql.AppendLine("AND d.\"PartId\" = @part_id");
            parameters.Add(new NpgsqlParameter("part_id", filter.PartId.Value));
        }

        if (filter.PartIds is { Count: > 0 })
        {
            sql.AppendLine("AND d.\"PartId\" = ANY(@part_ids)");
            parameters.Add(new NpgsqlParameter<Guid[]>("part_ids", filter.PartIds.ToArray()));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartNumber))
        {
            sql.AppendLine("AND p.\"PartNumber\" = @part_number");
            parameters.Add(new NpgsqlParameter("part_number", filter.PartNumber.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartNameContains))
        {
            sql.AppendLine("AND p.\"Name\" ILIKE @part_name");
            parameters.Add(new NpgsqlParameter("part_name", $"%{filter.PartNameContains.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.FileName))
        {
            sql.AppendLine("AND d.\"FileName\" = @file_name");
            parameters.Add(new NpgsqlParameter("file_name", filter.FileName.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.ContentType))
        {
            sql.AppendLine("AND d.\"ContentType\" = @content_type");
            parameters.Add(new NpgsqlParameter("content_type", filter.ContentType.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.StoragePathPrefix))
        {
            sql.AppendLine("AND d.\"StoragePath\" LIKE @storage_path_prefix");
            parameters.Add(new NpgsqlParameter("storage_path_prefix", $"{filter.StoragePathPrefix.Trim()}%"));
        }
    }
}
