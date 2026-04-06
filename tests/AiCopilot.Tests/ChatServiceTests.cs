using AiCopilot.Application.Abstractions;
using AiCopilot.Application.Configurations;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Services;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AiCopilot.Tests;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task ProcessQueryAsync_WhenSearchReturnsNoResults_ReturnsSafeResponseAndStoresMessages()
    {
        var dbContext = CreateDbContext();
        var openAiService = new Mock<IOpenAiService>(MockBehavior.Strict);
        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(x => x.SearchAsync("find duplicate part", 3, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SearchResult>());

        var sut = new ChatService(
            openAiService.Object,
            searchService.Object,
            dbContext,
            Options.Create(new CopilotOptions { SearchTopResults = 3 }),
            NullLogger<ChatService>.Instance);

        var response = await sut.ProcessQueryAsync(new ChatRequest(null, "find duplicate part"));

        Assert.Equal("I do not have enough grounded context to answer this question.", response.Summary);
        Assert.Empty(response.Recommendations);
        Assert.Equal(2, await dbContext.ChatMessages.CountAsync());
    }

    [Fact]
    public async Task ProcessQueryAsync_WhenModelReturnsGroundedJson_ReturnsMappedRecommendations()
    {
        var dbContext = CreateDbContext();
        var searchResults = new[]
        {
            new SearchResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "PN-100",
                "Motor Housing",
                "summary.txt",
                "text/plain",
                "generated://parts/PN-100",
                "Motor housing snippet",
                0.94d,
                0.2d,
                5,
                null,
                0.89d)
        };

        var openAiService = new Mock<IOpenAiService>();
        openAiService
            .Setup(x => x.Chat(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                {"answer":"PN-100 is a strong match.","grounded":true,"citations":[{"contextId":"ctx-1","partId":"00000000-0000-0000-0000-000000000001","partNumber":"PN-100","documentId":"00000000-0000-0000-0000-000000000002","storagePath":"generated://parts/PN-100","snippet":"Motor housing snippet"}]}
                """);

        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(x => x.SearchAsync("find motor housing", 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var sut = new ChatService(
            openAiService.Object,
            searchService.Object,
            dbContext,
            Options.Create(new CopilotOptions { SearchTopResults = 2 }),
            NullLogger<ChatService>.Instance);

        var response = await sut.ProcessQueryAsync(new ChatRequest(null, "find motor housing"));

        Assert.Equal("PN-100 is a strong match.", response.Summary);
        Assert.Single(response.Recommendations);
        Assert.Equal("PN-100", response.Recommendations[0].PartNumber);
    }

    [Fact]
    public async Task ProcessQueryAsync_WhenModelReturnsInvalidJson_FallsBackToSafeResponse()
    {
        var dbContext = CreateDbContext();
        var searchResults = new[]
        {
            new SearchResult(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "PN-100",
                "Motor Housing",
                "summary.txt",
                "text/plain",
                "generated://parts/PN-100",
                "Motor housing snippet",
                0.94d,
                0.2d,
                5,
                null,
                0.89d)
        };

        var openAiService = new Mock<IOpenAiService>();
        openAiService
            .Setup(x => x.Chat(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-json");

        var searchService = new Mock<ISearchService>();
        searchService
            .Setup(x => x.SearchAsync("find motor housing", 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var sut = new ChatService(
            openAiService.Object,
            searchService.Object,
            dbContext,
            Options.Create(new CopilotOptions { SearchTopResults = 2 }),
            NullLogger<ChatService>.Instance);

        var response = await sut.ProcessQueryAsync(new ChatRequest(null, "find motor housing"));

        Assert.Equal("I do not have enough grounded context to answer this question.", response.Summary);
        Assert.Single(response.Recommendations);
    }

    private static PlmDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var tenantProvider = new Mock<ICurrentTenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns("tenant-a");

        return new PlmDbContext(options, tenantProvider.Object);
    }
}
