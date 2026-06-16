using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Admin;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Billing;

public sealed class BillingDiagnosticsService : IBillingDiagnosticsService
{
    private readonly FlowDeskDbContext _dbContext;

    public BillingDiagnosticsService(FlowDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminBillingDebugResponse> GetForUserAsync(
        string userId,
        int eventCount = 10,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var events = await _dbContext.StripeWebhookEvents
            .AsNoTracking()
            .OrderByDescending(item => item.ReceivedAt)
            .Take(eventCount)
            .Select(item => new AdminStripeWebhookEventResponse(
                item.StripeEventId,
                item.Type,
                item.ReceivedAt,
                item.ProcessedAt,
                item.ProcessingError))
            .ToListAsync(cancellationToken);

        return new AdminBillingDebugResponse(
            ToSubscriptionResponse(subscription),
            events);
    }

    private static AdminSubscriptionStateResponse ToSubscriptionResponse(SubscriptionRecord? subscription)
    {
        if (subscription is null)
        {
            return new AdminSubscriptionStateResponse(
                PlanCode.Free.ToString(),
                SubscriptionStatus.None.ToString(),
                StripeCustomerId: null,
                StripeSubscriptionId: null,
                StripePriceId: null,
                CurrentPeriodEnd: null,
                CancelAtPeriodEnd: false,
                UpdatedAt: null);
        }

        return new AdminSubscriptionStateResponse(
            subscription.PlanCode.ToString(),
            subscription.Status.ToString(),
            subscription.StripeCustomerId,
            subscription.StripeSubscriptionId,
            subscription.StripePriceId,
            subscription.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd,
            subscription.UpdatedAt);
    }
}
