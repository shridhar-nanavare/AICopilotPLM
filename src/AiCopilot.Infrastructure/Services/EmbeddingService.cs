using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace AiCopilot.Infrastructure.Services;

internal sealed class EmbeddingService : IEmbeddingService
{
    private const string PartSummaryFileName = "__part_summary__.txt";
    private const string PartSummaryContentType = "text/plain";

    private readonly PlmDbContext _dbContext;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(PlmDbContext dbContext, IOpenAiService openAiService, ILogger<EmbeddingService> logger)
    {
        _dbContext = dbContext;
        _openAiService = openAiService;
        _logger = logger;
    }

    public Task ProcessPart(Part part, CancellationToken cancellationToken = default) =>
        ProcessParts([part], 1, cancellationToken);

    public async Task ProcessParts(IReadOnlyList<Part> parts, int batchSize = 16, CancellationToken cancellationToken = default)
    {
        if (parts.Count == 0)
        {
            return;
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        for (var i = 0; i < parts.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var partBatch = parts.Skip(i).Take(batchSize).ToList();
            var partTexts = partBatch.Select(ConvertPartToText).ToList();

            var embeddings = await _openAiService.CreateEmbeddings(partTexts, cancellationToken);

            if (embeddings.Count != partBatch.Count)
            {
                throw new InvalidOperationException($"Embedding count mismatch. Expected {partBatch.Count} but got {embeddings.Count}.");
            }

            for (var index = 0; index < partBatch.Count; index++)
            {
                var part = partBatch[index];
                var text = partTexts[index];

                var summaryStoragePath = GetSummaryStoragePath(part.Id);

                var document = await _dbContext.Documents.FirstOrDefaultAsync(
                    x => x.PartId == part.Id && x.StoragePath == summaryStoragePath,
                    cancellationToken);

                if (document is null)
                {
                    document = new Document
                    {
                        Id = Guid.NewGuid(),
                        PartId = part.Id,
                        FileName = PartSummaryFileName,
                        ContentType = PartSummaryContentType,
                        StoragePath = summaryStoragePath
                    };

                    _dbContext.Documents.Add(document);
                }

                var embedding = new Embedding
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkText = text,
                    Vector = new Vector(embeddings[index].ToArray())
                };

                _dbContext.Embeddings.Add(embedding);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Processed {Count} parts into embeddings (batch {BatchStart}-{BatchEnd}).", partBatch.Count, i + 1, i + partBatch.Count);
        }
    }

    private static string ConvertPartToText(Part part)
    {
        ArgumentNullException.ThrowIfNull(part);

        return $"Part Number: {part.PartNumber}\nName: {part.Name}";
    }

    private static string GetSummaryStoragePath(Guid partId) => $"generated://parts/{partId}/summary";
}
