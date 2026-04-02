namespace AiCopilot.Application.Configurations;

public sealed class CopilotOptions
{
    public const string SectionName = "Copilot";

    public string DefaultSystemPrompt { get; init; } = "You are AiCopilot.";

    public int MaxPromptLength { get; init; } = 2_000;

    public int SearchTopResults { get; init; } = 5;
}
