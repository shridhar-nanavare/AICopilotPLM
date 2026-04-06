using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiCopilot.Tests;

internal sealed class TestPlmDbContext : PlmDbContext
{
    public TestPlmDbContext(
        DbContextOptions<PlmDbContext> options,
        ICurrentTenantProvider currentTenantProvider)
        : base(options, currentTenantProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Embedding>().Ignore(x => x.Vector);
    }
}
