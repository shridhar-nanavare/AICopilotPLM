namespace AiCopilot.Application.Abstractions;

public interface ICurrentTenantProvider
{
    string TenantId { get; }
}
