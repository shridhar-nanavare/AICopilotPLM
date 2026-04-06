namespace AiCopilot.Infrastructure.Data.Entities;

public class BomItem : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid ParentPartId { get; set; }
    public Guid ChildPartId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedUtc { get; set; }

    public Part ParentPart { get; set; } = null!;
    public Part ChildPart { get; set; } = null!;
}
