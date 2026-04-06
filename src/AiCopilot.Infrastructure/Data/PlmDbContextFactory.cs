using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;
using AiCopilot.Infrastructure.Services;

namespace AiCopilot.Infrastructure.Data;

public class PlmDbContextFactory : IDesignTimeDbContextFactory<PlmDbContext>
{
    public PlmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlmDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PlmDatabase")
            ?? "Host=localhost;Port=5432;Database=aicopilot_plm;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql => npgsql.UseVector());

        return new PlmDbContext(optionsBuilder.Options, new StaticTenantProvider("design-time"));
    }
}
