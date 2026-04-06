namespace AiCopilot.Shared.Models;

public sealed record AgentRequest(string Query, bool Approved = false);
