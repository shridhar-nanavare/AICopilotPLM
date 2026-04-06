using System.Data;
using AiCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AiCopilot.Infrastructure.Services;

internal sealed class SearchQueryExecutor : ISearchQueryExecutor
{
    private readonly PlmDbContext _dbContext;

    public SearchQueryExecutor(PlmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SearchResult>> ExecuteAsync(
        string sql,
        IReadOnlyList<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SearchResult>();
        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new SearchResult(
                    reader.GetGuid(reader.GetOrdinal("embedding_id")),
                    reader.GetGuid(reader.GetOrdinal("document_id")),
                    reader.GetGuid(reader.GetOrdinal("part_id")),
                    reader.GetString(reader.GetOrdinal("part_number")),
                    reader.GetString(reader.GetOrdinal("part_name")),
                    reader.GetString(reader.GetOrdinal("file_name")),
                    reader.GetString(reader.GetOrdinal("content_type")),
                    reader.GetString(reader.GetOrdinal("storage_path")),
                    reader.GetString(reader.GetOrdinal("chunk_text")),
                    reader.GetDouble(reader.GetOrdinal("similarity_score")),
                    reader.GetDouble(reader.GetOrdinal("feedback_score")),
                    reader.GetInt32(reader.GetOrdinal("usage_count")),
                    reader.IsDBNull(reader.GetOrdinal("last_used"))
                        ? null
                        : reader.GetFieldValue<DateTime>(reader.GetOrdinal("last_used")),
                    reader.GetDouble(reader.GetOrdinal("ranking_score"))));
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return results;
    }
}
