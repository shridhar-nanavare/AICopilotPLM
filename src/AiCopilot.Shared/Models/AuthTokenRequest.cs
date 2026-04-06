namespace AiCopilot.Shared.Models;

public sealed record AuthTokenRequest(
    string Username,
    string Password);
