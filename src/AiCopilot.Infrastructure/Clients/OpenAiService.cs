using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Clients.Models;
using AiCopilot.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiCopilot.Infrastructure.Clients;

internal sealed class OpenAiService : IOpenAiService
{
    private static readonly Uri ChatCompletionsPath = new("v1/chat/completions", UriKind.Relative);
    private static readonly Uri EmbeddingsPath = new("v1/embeddings", UriKind.Relative);

    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(HttpClient httpClient, IOptions<AiProviderOptions> options, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid OpenAI endpoint configured: '{_options.Endpoint}'.");
        }

        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<IReadOnlyList<float>> CreateEmbedding(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await CreateEmbeddings([text], cancellationToken);
        return embeddings[0];
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> CreateEmbeddings(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
        {
            throw new ArgumentException("At least one text input is required.", nameof(texts));
        }

        if (texts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Text inputs cannot contain null/whitespace values.", nameof(texts));
        }

        var request = new OpenAiEmbeddingRequest(_options.EmbeddingModel, texts);
        using var response = await _httpClient.PostAsJsonAsync(EmbeddingsPath, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI embedding request failed with status {StatusCode}. Response: {Response}", response.StatusCode, content);
            throw new HttpRequestException($"OpenAI embedding request failed with status {response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("OpenAI embedding response was empty.");

        var orderedEmbeddings = payload.Data?
            .OrderBy(x => x.Index)
            .Select(x => x.Embedding)
            .ToList();

        if (orderedEmbeddings is null || orderedEmbeddings.Count == 0)
        {
            throw new InvalidOperationException("OpenAI embedding response did not include embedding vectors.");
        }

        if (orderedEmbeddings.Count != texts.Count || orderedEmbeddings.Any(x => x is null || x.Count == 0))
        {
            throw new InvalidOperationException("OpenAI embedding response did not include a complete set of vectors.");
        }

        return orderedEmbeddings.Select(x => x!).ToList();
    }

    public async Task<string> Chat(string prompt, string systemPrompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);

        var request = new OpenAiChatRequest(
            _options.ChatModel,
            [
                new OpenAiChatMessage("system", systemPrompt),
                new OpenAiChatMessage("user", prompt)
            ]);

        using var response = await _httpClient.PostAsJsonAsync(ChatCompletionsPath, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI chat request failed with status {StatusCode}. Response: {Response}", response.StatusCode, content);
            throw new HttpRequestException($"OpenAI chat request failed with status {response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("OpenAI chat response was empty.");

        var message = payload.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("OpenAI chat response did not include a message.");
        }

        return message;
    }
}
