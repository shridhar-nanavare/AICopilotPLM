using AiCopilot.Application.Abstractions;

namespace AiCopilot.Infrastructure.Services;

internal sealed class StaticUserProvider : ICurrentUserProvider
{
    public StaticUserProvider(string userName)
    {
        UserName = string.IsNullOrWhiteSpace(userName) ? "system" : userName.Trim();
    }

    public string UserName { get; }
}
