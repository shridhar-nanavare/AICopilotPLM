using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Infrastructure.Services;
using AiCopilot.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AiCopilot.Tests;

public sealed class PredictionServiceTests
{
    [Fact]
    public async Task PredictAsync_WhenPartFeaturesDoNotExist_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var sut = new PredictionService(dbContext);

        var act = () => sut.PredictAsync(new PredictionRequest(Guid.NewGuid()));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task PredictAsync_WhenFailureSignalsAreHigh_ReturnsHighRiskAndMatureLifecycle()
    {
        var partId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.PartFeatures.Add(new PartFeature
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-a",
            PartId = partId,
            FailureRate = 0.25d,
            UsageCount = 1200,
            Lifecycle = "mature",
            Cost = 15000m
        });
        await dbContext.SaveChangesAsync();

        var sut = new PredictionService(dbContext);

        var result = await sut.PredictAsync(new PredictionRequest(partId));

        Assert.Equal("HIGH", result.FailureRisk);
        Assert.Equal("MATURE", result.Lifecycle);
        Assert.True(result.FailureRiskScore >= 0.70d);
        Assert.Contains(result.Reasons, reason => reason.Contains("very high", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PredictAsync_WhenSignalsAreLow_ReturnsLowRiskAndGrowthLifecycle()
    {
        var partId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.PartFeatures.Add(new PartFeature
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-a",
            PartId = partId,
            FailureRate = 0.01d,
            UsageCount = 20,
            Lifecycle = "active",
            Cost = 200m
        });
        await dbContext.SaveChangesAsync();

        var sut = new PredictionService(dbContext);

        var result = await sut.PredictAsync(new PredictionRequest(partId));

        Assert.Equal("LOW", result.FailureRisk);
        Assert.Equal("GROWTH", result.Lifecycle);
        Assert.InRange(result.FailureRiskScore, 0d, 0.39d);
    }

    private static PlmDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var tenantProvider = new Mock<ICurrentTenantProvider>();
        tenantProvider.SetupGet(x => x.TenantId).Returns("tenant-a");

        return new TestPlmDbContext(options, tenantProvider.Object);
    }
}
