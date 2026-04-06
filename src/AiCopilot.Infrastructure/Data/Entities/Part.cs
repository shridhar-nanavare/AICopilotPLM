namespace AiCopilot.Infrastructure.Data.Entities;

public class Part : ITenantEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }

    public ICollection<BomItem> ParentBomItems { get; set; } = new List<BomItem>();
    public ICollection<BomItem> ChildBomItems { get; set; } = new List<BomItem>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public PartFeature? Features { get; set; }
    public DigitalTwinState? DigitalTwinState { get; set; }
}
