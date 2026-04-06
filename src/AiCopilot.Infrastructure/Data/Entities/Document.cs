namespace AiCopilot.Infrastructure.Data.Entities;

public class Document : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public Part Part { get; set; } = null!;
    public ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();
}
