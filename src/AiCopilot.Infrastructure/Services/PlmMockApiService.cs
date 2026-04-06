using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Services;

internal sealed class PlmMockApiService : IPlmMockApiService
{
    private readonly PlmDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISearchService _searchService;

    public PlmMockApiService(
        PlmDbContext dbContext,
        IEmbeddingService embeddingService,
        ISearchService searchService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _searchService = searchService;
    }

    public async Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PartNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        var partNumber = request.PartNumber.Trim();
        var name = request.Name.Trim();

        var existingPart = await _dbContext.Parts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PartNumber == partNumber, cancellationToken);

        if (existingPart is not null)
        {
            throw new InvalidOperationException($"Part '{partNumber}' already exists.");
        }

        var part = new Part
        {
            Id = Guid.NewGuid(),
            PartNumber = partNumber,
            Name = name
        };

        _dbContext.Parts.Add(part);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _embeddingService.ProcessPart(part, cancellationToken);

        return new CreatePartResult(part.Id, part.PartNumber, part.Name);
    }

    public async Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.QueryText);

        var searchResults = await _searchService.SearchAsync(
            request.QueryText.Trim(),
            top: 5,
            filter: new SearchFilter(StoragePathPrefix: "generated://parts/"),
            cancellationToken: cancellationToken);

        var candidates = searchResults
            .GroupBy(x => x.PartId)
            .Select(group => group
                .OrderByDescending(x => x.RankingScore)
                .ThenByDescending(x => x.SimilarityScore)
                .First())
            .Select(x => new DuplicateCandidate(
                x.EmbeddingId,
                x.PartId,
                x.PartNumber,
                x.PartName,
                x.SimilarityScore,
                x.RankingScore))
            .ToList();

        return new FindDuplicateResult(request.QueryText.Trim(), candidates);
    }

    public async Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.QueryText);

        var query = request.QueryText.Trim();
        var part = await _dbContext.Parts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PartNumber == query || EF.Functions.ILike(x.Name, $"%{query}%"),
                cancellationToken)
            ?? throw new InvalidOperationException($"Part matching '{query}' was not found.");

        var childItems = await _dbContext.Bom
            .AsNoTracking()
            .Where(x => x.ParentPartId == part.Id)
            .Include(x => x.ChildPart)
            .OrderByDescending(x => x.Quantity)
            .ToListAsync(cancellationToken);

        var parentCount = await _dbContext.Bom
            .AsNoTracking()
            .Where(x => x.ChildPartId == part.Id)
            .Select(x => x.ParentPartId)
            .Distinct()
            .CountAsync(cancellationToken);

        var components = childItems
            .Select(x => new BomComponentSummary(
                x.ChildPartId,
                x.ChildPart.PartNumber,
                x.ChildPart.Name,
                x.Quantity))
            .ToList();

        return new BomAnalysisResult(
            part.Id,
            part.PartNumber,
            part.Name,
            childItems.Count,
            parentCount,
            childItems.Sum(x => x.Quantity),
            components);
    }
}
