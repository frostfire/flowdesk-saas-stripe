using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowDesk.Contracts.Auth;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task WhoAmI_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/auth/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsToken_AndTokenCanReadWhoAmI()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new RegisterRequest("owner@flowdesk.test", "password1");

        var registerResponse = await client.PostAsJsonAsync("/auth/register", request);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var whoAmI = await client.GetFromJsonAsync<WhoAmIResponse>("/auth/whoami");

        Assert.NotNull(whoAmI);
        Assert.Equal(request.Email, whoAmI.Email);
        Assert.Equal(auth.User.Id, whoAmI.Id);
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
                        options.UseInMemoryDatabase($"flowdesk-auth-tests-{Guid.NewGuid():N}");
                    });
                });
            });
    }
}
