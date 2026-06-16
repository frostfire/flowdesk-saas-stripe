using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Analytics;
using FlowDesk.Contracts.Auth;
using FlowDesk.Contracts.Billing;
using FlowDesk.Contracts.Cases;
using FlowDesk.Contracts.Entitlements;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class BillingEndpointTests
{
    [Fact]
    public async Task Entitlements_ForFreeUser_ReturnsFree()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.GetFromJsonAsync<CurrentEntitlementsResponse>("/entitlements/me");

        Assert.NotNull(response);
        Assert.Equal("Free", response.Plan);
        Assert.False(response.Entitlements.CanCreateCases);
        Assert.False(response.Entitlements.CanViewAnalytics);
    }

    [Fact]
    public async Task Entitlements_ForActiveProUser_ReturnsPro()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        await SeedSubscriptionAsync(factory, auth.User.Id, PlanCode.Pro, SubscriptionStatus.Active);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.GetFromJsonAsync<CurrentEntitlementsResponse>("/entitlements/me");

        Assert.NotNull(response);
        Assert.Equal("Pro", response.Plan);
        Assert.True(response.Entitlements.CanCreateCases);
        Assert.True(response.Entitlements.CanViewAnalytics);
    }

    [Fact]
    public async Task CheckoutSession_UsesBillingGateway()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/billing/checkout-session",
            new CreateCheckoutSessionRequest("Pro", "http://localhost/success", "http://localhost/cancel"));

        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<BillingSessionResponse>();

        Assert.Equal("https://billing.test/checkout", session?.Url);
    }

    [Fact]
    public async Task PortalSession_UsesBillingGateway()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        await SeedBillingCustomerAsync(factory, auth.User.Id, "cus_portal");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/billing/portal-session",
            new CreatePortalSessionRequest("http://localhost/app"));

        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<BillingSessionResponse>();

        Assert.Equal("https://billing.test/portal", session?.Url);
    }

    [Fact]
    public async Task GatedEndpoints_ReturnForbiddenForFree_AndSuccessForPro()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var freeCreate = await client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Upgrade review", "Needs approval.", "Northwind Components"));
        var freeAnalytics = await client.GetAsync("/analytics/summary");

        Assert.Equal(HttpStatusCode.Forbidden, freeCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, freeAnalytics.StatusCode);

        await SeedSubscriptionAsync(factory, auth.User.Id, PlanCode.Pro, SubscriptionStatus.Active);

        var proCreate = await client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Upgrade review", "Needs approval.", "Northwind Components"));
        var proAnalytics = await client.GetFromJsonAsync<AnalyticsSummaryResponse>("/analytics/summary");

        Assert.Equal(HttpStatusCode.Created, proCreate.StatusCode);
        Assert.NotNull(proAnalytics);
        Assert.Equal(3, proAnalytics.TotalCases);
    }

    [Fact]
    public async Task GatedEndpoints_ReturnForbiddenForPastDueSubscription()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        await SeedSubscriptionAsync(factory, auth.User.Id, PlanCode.Team, SubscriptionStatus.PastDue);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var create = await client.PostAsJsonAsync(
            "/cases",
            new CreateCaseRequest("Past due review", "Payment failed.", "Northwind Components"));
        var analytics = await client.GetAsync("/analytics/summary");
        var entitlements = await client.GetFromJsonAsync<CurrentEntitlementsResponse>("/entitlements/me");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, analytics.StatusCode);
        Assert.Equal("PastDue", entitlements?.Status);
        Assert.False(entitlements?.Entitlements.CanCreateCases);
        Assert.False(entitlements?.Entitlements.CanViewAnalytics);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest($"billing-{Guid.NewGuid():N}@flowdesk.test", "password1"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Register response was empty.");
    }

    private static async Task SeedSubscriptionAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        PlanCode plan,
        SubscriptionStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();
        var existing = await dbContext.Subscriptions.SingleOrDefaultAsync(item => item.UserId == userId);
        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            dbContext.Subscriptions.Add(new SubscriptionRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanCode = plan,
                Status = status,
                StripeCustomerId = $"cus_{Guid.NewGuid():N}",
                StripeSubscriptionId = $"sub_{Guid.NewGuid():N}",
                StripePriceId = $"price_{plan}",
                CurrentPeriodEnd = now.AddMonths(1),
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.PlanCode = plan;
            existing.Status = status;
            existing.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedBillingCustomerAsync(
        WebApplicationFactory<Program> factory,
        string userId,
        string customerId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();
        var now = DateTimeOffset.UtcNow;

        dbContext.BillingCustomers.Add(new BillingCustomer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StripeCustomerId = customerId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await dbContext.SaveChangesAsync();
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"flowdesk-billing-tests-{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<FlowDeskDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<FlowDeskDbContext>>();
                    services.RemoveAll<IBillingGateway>();
                    services.AddSingleton<IBillingGateway, FakeBillingGateway>();
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.AddDbContext<FlowDeskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private sealed class FakeBillingGateway : IBillingGateway
    {
        public Task<string> CreateCheckoutSessionAsync(
            string userId,
            string email,
            PlanCode plan,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("https://billing.test/checkout");
        }

        public Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BillingSubscriptionSnapshot?>(null);
        }

        public Task<string> CreatePortalSessionAsync(
            string customerId,
            string returnUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("https://billing.test/portal");
        }
    }
}
