using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class SearchService : ISearchService
{
    private readonly PlmDbContext _dbContext;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(PlmDbContext dbContext, IOpenAiService openAiService, ILogger<SearchService> logger)
    {
        _dbContext = dbContext;
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

        var embeddingsQuery = _dbContext.Embeddings
            .AsNoTracking()
            .AsQueryable();

        embeddingsQuery = ApplyFilter(embeddingsQuery, filter);

        var matches = await embeddingsQuery
            .Select(embedding => new SearchMatch(
                embedding.Id,
                embedding.DocumentId,
                embedding.Document.PartId,
                embedding.Document.Part.PartNumber,
                embedding.Document.Part.Name,
                embedding.Document.FileName,
                embedding.Document.ContentType,
                embedding.Document.StoragePath,
                embedding.ChunkText,
                embedding.Vector.CosineDistance(queryVector)))
            .OrderBy(match => match.Distance)
            .Take(top)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Search completed for query length {QueryLength} with {ResultCount} results.",
            query.Length,
            matches.Count);

        return matches
            .Select(match => new SearchResult(
                match.EmbeddingId,
                match.DocumentId,
                match.PartId,
                match.PartNumber,
                match.PartName,
                match.FileName,
                match.ContentType,
                match.StoragePath,
                match.ChunkText,
                1d - match.Distance))
            .ToList();
    }

    private static IQueryable<Data.Entities.Embedding> ApplyFilter(
        IQueryable<Data.Entities.Embedding> query,
        SearchFilter? filter)
    {
        if (filter is null)
        {
            return query;
        }

        if (filter.PartId.HasValue)
        {
            query = query.Where(x => x.Document.PartId == filter.PartId.Value);
        }

        if (filter.PartIds is { Count: > 0 })
        {
            query = query.Where(x => filter.PartIds.Contains(x.Document.PartId));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartNumber))
        {
            var partNumber = filter.PartNumber.Trim();
            query = query.Where(x => x.Document.Part.PartNumber == partNumber);
        }

        if (!string.IsNullOrWhiteSpace(filter.PartNameContains))
        {
            var partName = filter.PartNameContains.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Document.Part.Name, $"%{partName}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.FileName))
        {
            var fileName = filter.FileName.Trim();
            query = query.Where(x => x.Document.FileName == fileName);
        }

        if (!string.IsNullOrWhiteSpace(filter.ContentType))
        {
            var contentType = filter.ContentType.Trim();
            query = query.Where(x => x.Document.ContentType == contentType);
        }

        if (!string.IsNullOrWhiteSpace(filter.StoragePathPrefix))
        {
            var storagePathPrefix = filter.StoragePathPrefix.Trim();
            query = query.Where(x => x.Document.StoragePath.StartsWith(storagePathPrefix));
        }

        return query;
    }

    private sealed record SearchMatch(
        Guid EmbeddingId,
        Guid DocumentId,
        Guid PartId,
        string PartNumber,
        string PartName,
        string FileName,
        string ContentType,
        string StoragePath,
        string ChunkText,
        double Distance);
}
