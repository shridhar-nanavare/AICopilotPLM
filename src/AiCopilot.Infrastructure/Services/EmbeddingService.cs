using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace AiCopilot.Infrastructure.Services;

internal sealed class EmbeddingService : IEmbeddingService
{
    private const int DefaultSinglePartBatchSize = 1;
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

    public Task ProcessPart(Part part, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(part);
        return ProcessParts([part], DefaultSinglePartBatchSize, cancellationToken);
    }

    public async Task ProcessParts(IReadOnlyList<Part> parts, int batchSize = 16, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parts);

        if (parts.Count == 0)
        {
            return;
        }

        if (parts.Any(part => part is null))
        {
            throw new ArgumentException("Parts cannot contain null entries.", nameof(parts));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        for (var i = 0; i < parts.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var partBatch = parts
                .Skip(i)
                .Take(batchSize)
                .Select(part => new PartEmbeddingPayload(part, ConvertPartToText(part), GetSummaryStoragePath(part.Id)))
                .ToList();

            var partTexts = partBatch
                .Select(x => x.Text)
                .ToList();

            var embeddings = await _openAiService.CreateEmbeddings(partTexts, cancellationToken);

            if (embeddings.Count != partBatch.Count)
            {
                throw new InvalidOperationException($"Embedding count mismatch. Expected {partBatch.Count} but got {embeddings.Count}.");
            }

            var partIds = partBatch
                .Select(x => x.Part.Id)
                .ToList();

            var summaryStoragePaths = partBatch
                .Select(x => x.StoragePath)
                .ToList();

            var existingDocuments = await _dbContext.Documents
                .Include(x => x.Embeddings)
                .Where(x => partIds.Contains(x.PartId) && summaryStoragePaths.Contains(x.StoragePath))
                .ToListAsync(cancellationToken);

            var documentsByPartId = existingDocuments
                .GroupBy(x => x.PartId)
                .ToDictionary(x => x.Key, x => x.OrderBy(document => document.CreatedUtc).ThenBy(document => document.Id).ToList());

            for (var index = 0; index < partBatch.Count; index++)
            {
                var payload = partBatch[index];
                var part = payload.Part;
                var text = payload.Text;
                var summaryStoragePath = payload.StoragePath;

                documentsByPartId.TryGetValue(part.Id, out var documentsForPart);
                var document = documentsForPart?.FirstOrDefault();

                if (documentsForPart is { Count: > 1 })
                {
                    _dbContext.Documents.RemoveRange(documentsForPart.Skip(1));
                }

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
                    documentsByPartId[part.Id] = [document];
                }
                else
                {
                    document.FileName = PartSummaryFileName;
                    document.ContentType = PartSummaryContentType;
                    document.StoragePath = summaryStoragePath;

                    if (document.Embeddings.Count > 0)
                    {
                        _dbContext.Embeddings.RemoveRange(document.Embeddings);
                        document.Embeddings.Clear();
                    }
                }

                var embedding = new Embedding
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkText = text,
                    Vector = new Vector(embeddings[index].ToArray()),
                    FeedbackScore = 0d,
                    UsageCount = 0,
                    LastUsed = null
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

    private sealed record PartEmbeddingPayload(Part Part, string Text, string StoragePath);
}
