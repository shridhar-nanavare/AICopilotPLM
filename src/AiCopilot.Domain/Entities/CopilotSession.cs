namespace AiCopilot.Domain.Entities;

public sealed class CopilotSession
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string UserId { get; init; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public bool IsActive { get; private set; } = true;

    public void EndSession() => IsActive = false;
}
