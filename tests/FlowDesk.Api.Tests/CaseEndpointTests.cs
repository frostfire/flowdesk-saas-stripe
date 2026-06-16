using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowDesk.Contracts.Auth;
using FlowDesk.Contracts.Cases;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class CaseEndpointTests
{
    [Fact]
    public async Task ListCases_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/cases");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListCases_WithToken_ReturnsSeededCases()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var cases = await client.GetFromJsonAsync<CaseSummaryResponse[]>("/cases");

        Assert.NotNull(cases);
        Assert.Equal(3, cases.Length);
        Assert.Contains(cases, item => item.Reference == "CF-1001");
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("cases@flowdesk.test", "password1"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Register response was empty.");
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<FlowDeskDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<FlowDeskDbContext>>();
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.AddDbContext<FlowDeskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"flowdesk-case-tests-{Guid.NewGuid():N}");
                    });
                });
            });
    }
}
