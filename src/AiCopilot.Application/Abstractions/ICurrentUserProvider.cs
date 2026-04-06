namespace AiCopilot.Application.Abstractions;

public interface ICurrentUserProvider
{
    string UserName { get; }
}
