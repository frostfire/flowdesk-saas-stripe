using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Auth;
using FlowDesk.Contracts.Entitlements;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class StripeWebhookEndpointTests
{
    private const string WebhookSecret = "whsec_test_secret";

    [Fact]
    public async Task StripeWebhook_RejectsInvalidSignature()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var payload = CreateSubscriptionEvent("evt_invalid", "customer.subscription.updated", "sub_invalid");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", "t=1,v1=bad");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StripeWebhook_ProcessesDuplicateEventOnce()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        fakeGateway.SetSnapshot("sub_duplicate", Snapshot(auth.User.Id, "sub_duplicate", SubscriptionStatus.Active));
        var payload = CreateSubscriptionEvent("evt_duplicate", "customer.subscription.updated", "sub_duplicate");

        var first = await PostStripeEventAsync(client, payload);
        var second = await PostStripeEventAsync(client, payload);

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();
        Assert.Equal(1, fakeGateway.GetSubscriptionCallCount);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();
        Assert.Equal(1, await dbContext.StripeWebhookEvents.CountAsync());
    }

    [Fact]
    public async Task StripeWebhook_RetriesUnprocessedEvent()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        fakeGateway.SetSnapshot("sub_retry", Snapshot(auth.User.Id, "sub_retry", SubscriptionStatus.Active));
        fakeGateway.ThrowOnceForSubscription("sub_retry");
        var payload = CreateSubscriptionEvent("evt_retry", "customer.subscription.updated", "sub_retry");

        var first = await PostStripeEventAsync(client, payload);
        var second = await PostStripeEventAsync(client, payload);

        Assert.Equal(HttpStatusCode.InternalServerError, first.StatusCode);
        second.EnsureSuccessStatusCode();
        Assert.Equal(2, fakeGateway.GetSubscriptionCallCount);
        var entitlements = await GetEntitlementsAsync(client, auth);
        Assert.Equal("Active", entitlements.Status);
        Assert.True(entitlements.Entitlements.CanCreateCases);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();
        var record = await dbContext.StripeWebhookEvents.SingleAsync(item => item.StripeEventId == "evt_retry");
        Assert.NotNull(record.ProcessedAt);
        Assert.Null(record.ProcessingError);
    }

    [Fact]
    public async Task StripeWebhook_ReconcilesAgainstCurrentSubscriptionState()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        fakeGateway.SetSnapshot("sub_reconcile", Snapshot(auth.User.Id, "sub_reconcile", SubscriptionStatus.Active));
        var paidPayload = CreateInvoiceEvent("evt_paid", "invoice.paid", "sub_reconcile");

        var paid = await PostStripeEventAsync(client, paidPayload);

        paid.EnsureSuccessStatusCode();
        fakeGateway.SetSnapshot("sub_reconcile", Snapshot(auth.User.Id, "sub_reconcile", SubscriptionStatus.PastDue));
        var failedPayload = CreateInvoiceEvent("evt_failed", "invoice.payment_failed", "sub_reconcile");
        var failed = await PostStripeEventAsync(client, failedPayload);

        failed.EnsureSuccessStatusCode();
        var entitlements = await GetEntitlementsAsync(client, auth);
        Assert.Equal("PastDue", entitlements.Status);
        Assert.False(entitlements.Entitlements.CanCreateCases);
        Assert.False(entitlements.Entitlements.CanViewAnalytics);
    }

    [Fact]
    public async Task StripeWebhook_OutOfOrderEventUsesGatewaySnapshot()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        fakeGateway.SetSnapshot("sub_out_of_order", Snapshot(auth.User.Id, "sub_out_of_order", SubscriptionStatus.Active));
        var failedPayload = CreateInvoiceEvent("evt_out_of_order_failed", "invoice.payment_failed", "sub_out_of_order");

        var response = await PostStripeEventAsync(client, failedPayload);

        response.EnsureSuccessStatusCode();
        var entitlements = await GetEntitlementsAsync(client, auth);
        Assert.Equal("Active", entitlements.Status);
        Assert.True(entitlements.Entitlements.CanCreateCases);
    }

    [Fact]
    public async Task StripeWebhook_PortalPlanChangeUpdatesFromGatewaySnapshot()
    {
        var fakeGateway = new FakeBillingGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        fakeGateway.SetSnapshot("sub_portal_change", Snapshot(auth.User.Id, "sub_portal_change", PlanCode.Pro));
        var firstPayload = CreateSubscriptionEvent("evt_portal_pro", "customer.subscription.updated", "sub_portal_change");
        var first = await PostStripeEventAsync(client, firstPayload);
        first.EnsureSuccessStatusCode();

        fakeGateway.SetSnapshot("sub_portal_change", Snapshot(auth.User.Id, "sub_portal_change", PlanCode.Team));
        var secondPayload = CreateSubscriptionEvent("evt_portal_team", "customer.subscription.updated", "sub_portal_change");
        var second = await PostStripeEventAsync(client, secondPayload);

        second.EnsureSuccessStatusCode();
        var entitlements = await GetEntitlementsAsync(client, auth);
        Assert.Equal("Team", entitlements.Plan);
        Assert.Equal(10, entitlements.Entitlements.MaxSeats);
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeBillingGateway fakeGateway)
    {
        var databaseName = $"flowdesk-webhook-tests-{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["STRIPE_WEBHOOK_SECRET"] = WebhookSecret,
                    });
                });
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<FlowDeskDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<FlowDeskDbContext>>();
                    services.RemoveAll<IBillingGateway>();
                    services.AddSingleton<IBillingGateway>(fakeGateway);
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.AddDbContext<FlowDeskDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName);
                    });
                });
            });
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest($"webhook-{Guid.NewGuid():N}@flowdesk.test", "password1"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("Register response was empty.");
    }

    private static async Task<CurrentEntitlementsResponse> GetEntitlementsAsync(HttpClient client, AuthResponse auth)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            auth.AccessToken);

        return await client.GetFromJsonAsync<CurrentEntitlementsResponse>("/entitlements/me")
            ?? throw new InvalidOperationException("Entitlements response was empty.");
    }

    private static async Task<HttpResponseMessage> PostStripeEventAsync(HttpClient client, string payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", CreateSignature(payload));

        return await client.SendAsync(request);
    }

    private static string CreateSubscriptionEvent(string eventId, string type, string subscriptionId)
    {
        return $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "{{StripeConfiguration.ApiVersion}}",
          "type": "{{type}}",
          "data": {
            "object": {
              "id": "{{subscriptionId}}",
              "object": "subscription"
            }
          }
        }
        """;
    }

    private static string CreateInvoiceEvent(string eventId, string type, string subscriptionId)
    {
        return $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "{{StripeConfiguration.ApiVersion}}",
          "type": "{{type}}",
          "data": {
            "object": {
              "id": "in_{{eventId}}",
              "object": "invoice",
              "subscription": "{{subscriptionId}}"
            }
          }
        }
        """;
    }

    private static string CreateSignature(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();

        return $"t={timestamp},v1={signature}";
    }

    private static BillingSubscriptionSnapshot Snapshot(
        string userId,
        string subscriptionId,
        SubscriptionStatus status)
    {
        return new BillingSubscriptionSnapshot(
            userId,
            "cus_test",
            subscriptionId,
            "price_pro",
            PlanCode.Pro,
            status,
            DateTimeOffset.UtcNow.AddMonths(1),
            CancelAtPeriodEnd: false);
    }

    private static BillingSubscriptionSnapshot Snapshot(
        string userId,
        string subscriptionId,
        PlanCode plan)
    {
        return new BillingSubscriptionSnapshot(
            userId,
            "cus_test",
            subscriptionId,
            $"price_{plan.ToString().ToLowerInvariant()}",
            plan,
            SubscriptionStatus.Active,
            DateTimeOffset.UtcNow.AddMonths(1),
            CancelAtPeriodEnd: false);
    }

    private sealed class FakeBillingGateway : IBillingGateway
    {
        private readonly Dictionary<string, BillingSubscriptionSnapshot> _snapshots = [];
        private readonly HashSet<string> _throwOnceSubscriptions = [];

        public int GetSubscriptionCallCount { get; private set; }

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

        public Task<string> CreatePortalSessionAsync(
            string customerId,
            string returnUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("https://billing.test/portal");
        }

        public Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            GetSubscriptionCallCount++;
            if (_throwOnceSubscriptions.Remove(subscriptionId))
            {
                throw new InvalidOperationException("Subscription lookup failed.");
            }

            return Task.FromResult(_snapshots.GetValueOrDefault(subscriptionId));
        }

        public void SetSnapshot(string subscriptionId, BillingSubscriptionSnapshot snapshot)
        {
            _snapshots[subscriptionId] = snapshot;
        }

        public void ThrowOnceForSubscription(string subscriptionId)
        {
            _throwOnceSubscriptions.Add(subscriptionId);
        }
    }
}
