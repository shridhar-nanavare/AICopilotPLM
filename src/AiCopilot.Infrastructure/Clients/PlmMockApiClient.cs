using System.Net.Http.Json;
using AiCopilot.Infrastructure.Options;
using AiCopilot.Shared.Models;
using Microsoft.Extensions.Options;

namespace AiCopilot.Infrastructure.Clients;

internal sealed class PlmMockApiClient : IPlmMockApiClient
{
    private static readonly Uri CreatePartPath = new("api/mock/plm/create-part", UriKind.Relative);
    private static readonly Uri FindDuplicatePath = new("api/mock/plm/find-duplicate", UriKind.Relative);
    private static readonly Uri AnalyzeBomPath = new("api/mock/plm/analyze-bom", UriKind.Relative);

    private readonly HttpClient _httpClient;

    public PlmMockApiClient(HttpClient httpClient, IOptions<PlmApiOptions> options)
    {
        _httpClient = httpClient;

        if (!Uri.TryCreate(options.Value.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException($"Invalid PLM API base URL configured: '{options.Value.BaseUrl}'.");
        }

        _httpClient.BaseAddress = baseUri;
    }

    public Task<CreatePartResult> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<CreatePartRequest, CreatePartResult>(CreatePartPath, request, cancellationToken);

    public Task<FindDuplicateResult> FindDuplicateAsync(FindDuplicateRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<FindDuplicateRequest, FindDuplicateResult>(FindDuplicatePath, request, cancellationToken);

    public Task<BomAnalysisResult> AnalyzeBomAsync(AnalyzeBomRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AnalyzeBomRequest, BomAnalysisResult>(AnalyzeBomPath, request, cancellationToken);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(Uri path, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"PLM mock API request to '{path}' failed with status {(int)response.StatusCode}: {content}");
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken)
            ?? throw new InvalidOperationException($"PLM mock API response for '{path}' was empty.");
    }
}
