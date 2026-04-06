using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;

namespace AiCopilot.Tests;

public sealed class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WhenTopIsLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var openAiService = new Mock<IOpenAiService>();
        var tenantProvider = new Mock<ICurrentTenantProvider>();
        var searchQueryExecutor = new Mock<ISearchQueryExecutor>();
        var sut = new SearchService(
            tenantProvider.Object,
            searchQueryExecutor.Object,
            openAiService.Object,
            NullLogger<SearchService>.Instance);

        var act = () => sut.SearchAsync("motor housing", top: 0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public async Task SearchAsync_WhenCalled_BuildsTenantAwareQueryAndReturnsExecutorResults()
    {
        var expectedResults = new List<SearchResult>
        {
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "PN-100",
                "Motor Housing",
                "summary.txt",
                "text/plain",
                "generated://parts/PN-100",
                "Motor housing summary",
                0.91d,
                0.2d,
                7,
                null,
                0.88d)
        };

        var openAiService = new Mock<IOpenAiService>();
        openAiService
            .Setup(x => x.CreateEmbedding("motor housing", It.IsAny<CancellationToken>()))
            .ReturnsAsync([0.1f, 0.2f, 0.3f]);

        var tenantProvider = new Mock<ICurrentTenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns("tenant-a");

        var searchQueryExecutor = new Mock<ISearchQueryExecutor>();
        searchQueryExecutor
            .Setup(x => x.ExecuteAsync(
                It.Is<string>(sql =>
                    sql.Contains("WHERE e.tenant_id = @tenant_id", StringComparison.Ordinal) &&
                    sql.Contains("AND p.\"PartNumber\" = @part_number", StringComparison.Ordinal)),
                It.Is<IReadOnlyList<NpgsqlParameter>>(parameters =>
                    parameters.Any(p => p.ParameterName == "tenant_id" && p.Value != null && string.Equals(p.Value.ToString(), "tenant-a", StringComparison.Ordinal)) &&
                    parameters.Any(p => p.ParameterName == "part_number" && p.Value != null && string.Equals(p.Value.ToString(), "PN-100", StringComparison.Ordinal)) &&
                    parameters.Any(p => p.ParameterName == "top" && Convert.ToInt32(p.Value) == 3)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        var sut = new SearchService(
            tenantProvider.Object,
            searchQueryExecutor.Object,
            openAiService.Object,
            NullLogger<SearchService>.Instance);

        var result = await sut.SearchAsync(
            "motor housing",
            top: 3,
            filter: new SearchFilter(PartNumber: "PN-100"));

        Assert.Single(result);
        Assert.Equal("PN-100", result[0].PartNumber);
        Assert.Equal(0.88d, result[0].RankingScore);
    }
}
