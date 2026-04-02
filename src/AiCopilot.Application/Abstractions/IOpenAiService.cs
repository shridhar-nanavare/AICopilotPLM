namespace AiCopilot.Application.Abstractions;

public interface IOpenAiService
{
    Task<IReadOnlyList<IReadOnlyList<float>>> CreateEmbeddings(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float>> CreateEmbedding(string text, CancellationToken cancellationToken = default);

    Task<string> Chat(string prompt, string systemPrompt, CancellationToken cancellationToken = default);
}
