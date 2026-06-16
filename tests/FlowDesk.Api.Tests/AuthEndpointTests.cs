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

    [Fact]
    public async Task Refresh_WithRememberMeCookie_ReturnsNewAccessToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new RegisterRequest("remember@flowdesk.test", "password1", RememberMe: true);

        var registerResponse = await client.PostAsJsonAsync("/auth/register", request);
        registerResponse.EnsureSuccessStatusCode();
        var cookie = GetRefreshCookie(registerResponse);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", cookie);
        var refreshResponse = await client.SendAsync(refreshRequest);

        refreshResponse.EnsureSuccessStatusCode();
        var auth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(request.Email, auth.User.Email);
    }

    [Fact]
    public async Task Refresh_WithoutRememberMeCookie_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("session@flowdesk.test", "password1"));
        registerResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsync("/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        Assert.False(registerResponse.Headers.TryGetValues("Set-Cookie", out _));
    }

    [Fact]
    public async Task RefreshToken_CannotBeUsedAsBearerToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("bearer-refresh@flowdesk.test", "password1", RememberMe: true));
        registerResponse.EnsureSuccessStatusCode();
        var cookie = GetRefreshCookie(registerResponse);
        var refreshToken = cookie.Split('=', 2)[1];

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        var whoAmI = await client.GetAsync("/auth/whoami");

        Assert.Equal(HttpStatusCode.Unauthorized, whoAmI.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsRefreshCookie()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("logout@flowdesk.test", "password1", RememberMe: true));
        registerResponse.EnsureSuccessStatusCode();
        var cookie = GetRefreshCookie(registerResponse);

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        var logoutResponse = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Contains(logoutResponse.Headers.GetValues("Set-Cookie"), value => value.StartsWith("flowdesk_refresh=;", StringComparison.Ordinal));
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"flowdesk-auth-tests-{Guid.NewGuid():N}";

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
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private static string GetRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("flowdesk_refresh=", StringComparison.Ordinal));

        return setCookie.Split(';', 2)[0];
    }
}
