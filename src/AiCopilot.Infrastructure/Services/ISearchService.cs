namespace AiCopilot.Infrastructure.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        int top = 5,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default);
}
