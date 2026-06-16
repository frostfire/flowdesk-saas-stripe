using System.Text.Json;
using FlowDesk.Application.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace FlowDesk.Infrastructure.Billing;

public sealed class StripeWebhookService : IStripeWebhookService
{
    private static readonly HashSet<string> RelevantEventTypes =
    [
        "checkout.session.completed",
        "customer.subscription.created",
        "customer.subscription.updated",
        "customer.subscription.deleted",
        "invoice.paid",
        "invoice.payment_failed",
    ];

    private readonly IBillingGateway _billingGateway;
    private readonly FlowDeskDbContext _dbContext;
    private readonly BillingOptions _options;

    public StripeWebhookService(
        IBillingGateway billingGateway,
        FlowDeskDbContext dbContext,
        IOptions<BillingOptions> options)
    {
        _billingGateway = billingGateway;
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.StripeWebhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

        Event stripeEvent;
        try
        {
            // Verify the signature, but don't reject on API-version mismatch: the account's
            // webhook API version can differ from the SDK's pinned version, and this handler
            // only uses the event id/type and re-fetches the live subscription, so the event
            // payload's shape/version is irrelevant.
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _options.StripeWebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return new StripeWebhookProcessResult(IsValid: false, IsDuplicate: false);
        }

        var existing = await _dbContext.StripeWebhookEvents
            .SingleOrDefaultAsync(item => item.StripeEventId == stripeEvent.Id, cancellationToken);
        if (existing?.ProcessedAt is not null)
        {
            return new StripeWebhookProcessResult(IsValid: true, IsDuplicate: true);
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var eventRecord = existing ?? new StripeWebhookEventRecord
        {
            Id = Guid.NewGuid(),
            StripeEventId = stripeEvent.Id,
            Type = stripeEvent.Type,
            ReceivedAt = receivedAt,
        };
        if (existing is null)
        {
            _dbContext.StripeWebhookEvents.Add(eventRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!RelevantEventTypes.Contains(stripeEvent.Type))
        {
            eventRecord.ProcessedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new StripeWebhookProcessResult(IsValid: true, IsDuplicate: false);
        }

        try
        {
            var subscriptionId = ExtractSubscriptionId(payload, stripeEvent.Type);
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var snapshot = await _billingGateway.GetSubscriptionAsync(subscriptionId, cancellationToken);
                if (snapshot is not null)
                {
                    await ApplySnapshotAsync(snapshot, cancellationToken);
                }
            }

            eventRecord.ProcessedAt = DateTimeOffset.UtcNow;
            eventRecord.ProcessingError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new StripeWebhookProcessResult(IsValid: true, IsDuplicate: false);
        }
        catch (Exception ex)
        {
            eventRecord.ProcessingError = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task ApplySnapshotAsync(
        BillingSubscriptionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var userId = snapshot.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            var customer = await _dbContext.BillingCustomers
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.StripeCustomerId == snapshot.CustomerId, cancellationToken);
            userId = customer?.UserId;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(snapshot.CustomerId))
        {
            var customer = await _dbContext.BillingCustomers
                .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
            if (customer is null)
            {
                _dbContext.BillingCustomers.Add(new BillingCustomer
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    StripeCustomerId = snapshot.CustomerId,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            else
            {
                customer.StripeCustomerId = snapshot.CustomerId;
                customer.UpdatedAt = now;
            }
        }

        var subscription = await _dbContext.Subscriptions
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (subscription is null)
        {
            _dbContext.Subscriptions.Add(new SubscriptionRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StripeCustomerId = snapshot.CustomerId,
                StripeSubscriptionId = snapshot.SubscriptionId,
                StripePriceId = snapshot.PriceId,
                PlanCode = snapshot.Plan,
                Status = snapshot.Status,
                CurrentPeriodEnd = snapshot.CurrentPeriodEnd,
                CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd,
                CreatedAt = now,
                UpdatedAt = now,
            });

            return;
        }

        subscription.StripeCustomerId = snapshot.CustomerId;
        subscription.StripeSubscriptionId = snapshot.SubscriptionId;
        subscription.StripePriceId = snapshot.PriceId;
        subscription.PlanCode = snapshot.Plan;
        subscription.Status = snapshot.Status;
        subscription.CurrentPeriodEnd = snapshot.CurrentPeriodEnd;
        subscription.CancelAtPeriodEnd = snapshot.CancelAtPeriodEnd;
        subscription.UpdatedAt = now;
    }

    private static string? ExtractSubscriptionId(string payload, string eventType)
    {
        using var document = JsonDocument.Parse(payload);
        var dataObject = document.RootElement.GetProperty("data").GetProperty("object");

        if (eventType.StartsWith("customer.subscription.", StringComparison.Ordinal))
        {
            return dataObject.GetProperty("id").GetString();
        }

        if (dataObject.TryGetProperty("subscription", out var subscription))
        {
            return subscription.ValueKind == JsonValueKind.String
                ? subscription.GetString()
                : subscription.TryGetProperty("id", out var subscriptionObjectId)
                    ? subscriptionObjectId.GetString()
                    : null;
        }

        return null;
    }
}
