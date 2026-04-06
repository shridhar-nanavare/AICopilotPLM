using Npgsql;

namespace AiCopilot.Infrastructure.Services;

internal interface ISearchQueryExecutor
{
    Task<IReadOnlyList<SearchResult>> ExecuteAsync(
        string sql,
        IReadOnlyList<NpgsqlParameter> parameters,
        CancellationToken cancellationToken = default);
}
