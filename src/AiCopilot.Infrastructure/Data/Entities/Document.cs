namespace AiCopilot.Infrastructure.Data.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public Part Part { get; set; } = null!;
    public ICollection<Embedding> Embeddings { get; set; } = new List<Embedding>();
}
