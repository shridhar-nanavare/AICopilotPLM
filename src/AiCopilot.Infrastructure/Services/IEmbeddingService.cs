using AiCopilot.Infrastructure.Data.Entities;

namespace AiCopilot.Infrastructure.Services;

public interface IEmbeddingService
{
    Task ProcessPart(Part part, CancellationToken cancellationToken = default);

    Task ProcessParts(IReadOnlyList<Part> parts, int batchSize = 16, CancellationToken cancellationToken = default);
}
