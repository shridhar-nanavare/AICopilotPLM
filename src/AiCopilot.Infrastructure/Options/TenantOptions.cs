namespace AiCopilot.Infrastructure.Options;

public sealed class TenantOptions
{
    public const string SectionName = "Tenant";

    public string DefaultTenantId { get; init; } = "default";
}
