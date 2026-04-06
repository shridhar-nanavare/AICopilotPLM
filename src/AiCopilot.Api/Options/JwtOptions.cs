using System.ComponentModel.DataAnnotations;

namespace AiCopilot.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "AiCopilot";

    [Required]
    public string Audience { get; init; } = "AiCopilotClients";

    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = "change-this-signing-key-in-production-12345";

    public int ExpirationMinutes { get; init; } = 120;
}
