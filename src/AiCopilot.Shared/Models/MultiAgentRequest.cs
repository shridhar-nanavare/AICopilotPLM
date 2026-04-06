namespace AiCopilot.Shared.Models;

public sealed record MultiAgentRequest(string Query, bool Approved = false);
