using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiCopilot.Application.Abstractions;
using AiCopilot.Infrastructure.Data;
using AiCopilot.Infrastructure.Services;
using AiCopilot.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace AiCopilot.Tests;

internal sealed class ApiIntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    public Mock<ISearchService> SearchServiceMock { get; } = new();
    public Mock<IOpenAiService> OpenAiServiceMock { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PlmDbContext>>();
            services.RemoveAll<PlmDbContext>();
            services.RemoveAll<ISearchService>();
            services.RemoveAll<IOpenAiService>();

            services.AddScoped<PlmDbContext>(serviceProvider =>
            {
                var options = new DbContextOptionsBuilder<PlmDbContext>()
                    .UseInMemoryDatabase(_databaseName)
                    .Options;

                var tenantProvider = serviceProvider.GetRequiredService<ICurrentTenantProvider>();
                return new TestPlmDbContext(options, tenantProvider);
            });

            services.AddSingleton(SearchServiceMock.Object);
            services.AddSingleton(OpenAiServiceMock.Object);
        });
    }

    public async Task SeedAsync(Func<PlmDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlmDbContext>();
        await seed(dbContext);
    }

    public async Task<string> GetAccessTokenAsync(
        HttpClient client,
        string username = "user",
        string password = "user123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/token", new AuthTokenRequest(username, password));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        return payload!.AccessToken;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string username = "user",
        string password = "user123!")
    {
        var client = CreateClient();
        var token = await GetAccessTokenAsync(client, username, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
