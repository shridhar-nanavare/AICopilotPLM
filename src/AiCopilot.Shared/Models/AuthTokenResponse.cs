namespace AiCopilot.Shared.Models;

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime ExpiresUtc,
    string Role,
    string TenantId);
