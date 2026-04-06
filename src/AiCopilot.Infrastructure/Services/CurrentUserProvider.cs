using System.Security.Claims;
using AiCopilot.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AiCopilot.Infrastructure.Services;

internal sealed class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? "system";
}
