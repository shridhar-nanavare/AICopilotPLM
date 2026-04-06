using System.Net;
using System.Net.Http.Json;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Infrastructure.Services;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Pgvector;

namespace AiCopilot.Tests;

public sealed class ApiEndpointIntegrationTests : IAsyncLifetime
{
    private readonly ApiIntegrationTestFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PostApiChat_WhenAuthorized_ReturnsResponseStructureAndStatusCodeOk()
    {
        var embeddingId = Guid.NewGuid();

        _factory.SearchServiceMock
            .Setup(x => x.SearchAsync("recommend motor housing", 5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new SearchResult(
                    embeddingId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "PN-100",
                    "Motor Housing",
                    "summary.txt",
                    "text/plain",
                    "generated://parts/PN-100",
                    "Motor housing summary",
                    0.92d,
                    0.3d,
                    4,
                    null,
                    0.87d)
            ]);

        _factory.OpenAiServiceMock
            .Setup(x => x.Chat(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                {"answer":"PN-100 is the best recommendation.","grounded":true,"citations":[{"contextId":"ctx-1","partId":"11111111-1111-1111-1111-111111111111","partNumber":"PN-100","documentId":"22222222-2222-2222-2222-222222222222","storagePath":"generated://parts/PN-100","snippet":"Motor housing summary"}]}
                """);

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest(null, "recommend motor housing"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload!.SessionId);
        Assert.Equal("PN-100 is the best recommendation.", payload.Summary);
        Assert.Single(payload.Recommendations);
        Assert.Equal("PN-100", payload.Recommendations[0].PartNumber);
    }

    [Fact]
    public async Task PostApiChat_WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest(null, "recommend motor housing"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostApiFeedback_WhenRecommendationIsSubmitted_CompletesEndToEndFlow()
    {
        var sessionId = Guid.NewGuid();
        var partId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var embeddingId = Guid.NewGuid();

        await _factory.SeedAsync(async dbContext =>
        {
            dbContext.ChatSessions.Add(new ChatSession
            {
                Id = sessionId,
                TenantId = "default"
            });

            dbContext.Parts.Add(new Part
            {
                Id = partId,
                TenantId = "default",
                PartNumber = "PN-200",
                Name = "Cooling Plate"
            });

            dbContext.Documents.Add(new Document
            {
                Id = documentId,
                TenantId = "default",
                PartId = partId,
                FileName = "summary.txt",
                ContentType = "text/plain",
                StoragePath = "generated://parts/PN-200"
            });

            dbContext.Embeddings.Add(new Embedding
            {
                Id = embeddingId,
                TenantId = "default",
                DocumentId = documentId,
                ChunkText = "Cooling plate summary",
                Vector = new Vector(new[] { 0.1f, 0.2f, 0.3f }),
                FeedbackScore = 0d
            });

            await dbContext.SaveChangesAsync();
        });

        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/feedback",
            new FeedbackRequest(embeddingId, 0.9d, sessionId, "Strong recommendation"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<FeedbackResponse>();

        Assert.NotNull(payload);
        Assert.Equal(embeddingId, payload!.EmbeddingId);
        Assert.Equal(0.9d, payload.FeedbackScore);

        await _factory.SeedAsync(async dbContext =>
        {
            var feedbackCount = await dbContext.Feedback.CountAsync();
            var embedding = await dbContext.Embeddings.FirstAsync(x => x.Id == embeddingId);

            Assert.Equal(1, feedbackCount);
            Assert.Equal(0.9d, embedding.FeedbackScore);
        });
    }
}
