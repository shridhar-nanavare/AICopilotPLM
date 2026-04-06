using System.Security.Claims;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AiCopilot.Infrastructure.Services;

internal sealed class CurrentTenantProvider : ICurrentTenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TenantOptions _tenantOptions;

    public CurrentTenantProvider(IHttpContextAccessor httpContextAccessor, IOptions<TenantOptions> tenantOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantOptions = tenantOptions.Value;
    }

    public string TenantId
    {
        get
        {
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("tenant_id");
            return string.IsNullOrWhiteSpace(tenantId) ? _tenantOptions.DefaultTenantId : tenantId.Trim();
        }
    }
}
