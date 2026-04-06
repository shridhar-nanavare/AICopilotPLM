namespace AiCopilot.Api.Options;

public sealed class AuthUserOptions
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Role { get; init; } = "User";
    public string TenantId { get; init; } = "default";
}
