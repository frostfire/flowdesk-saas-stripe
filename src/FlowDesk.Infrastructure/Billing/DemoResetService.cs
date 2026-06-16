using FlowDesk.Application.Billing;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Billing;

public sealed class DemoResetService : IDemoResetService
{
    public const string DemoUserEmail = "demo@flowdesk.test";

    private readonly FlowDeskDbContext _dbContext;
    private readonly IBillingGateway _billingGateway;
    private readonly UserManager<ApplicationUser> _userManager;

    public DemoResetService(
        FlowDeskDbContext dbContext,
        IBillingGateway billingGateway,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _billingGateway = billingGateway;
        _userManager = userManager;
    }

    public async Task<DemoResetResult> ResetAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.Subscriptions.ToListAsync(cancellationToken);
        var canceledSubscriptions = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var subscription in subscriptions)
        {
            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId)
                && subscription.Status is not SubscriptionStatus.Canceled and not SubscriptionStatus.None)
            {
                await _billingGateway.CancelSubscriptionAsync(subscription.StripeSubscriptionId, cancellationToken);
                canceledSubscriptions++;
            }

            subscription.StripeCustomerId = null;
            subscription.StripeSubscriptionId = null;
            subscription.StripePriceId = null;
            subscription.PlanCode = PlanCode.Free;
            subscription.Status = SubscriptionStatus.None;
            subscription.CurrentPeriodEnd = null;
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAt = now;
        }

        var billingCustomers = await _dbContext.BillingCustomers.ToListAsync(cancellationToken);
        var webhookEvents = await _dbContext.StripeWebhookEvents.ToListAsync(cancellationToken);
        _dbContext.BillingCustomers.RemoveRange(billingCustomers);
        _dbContext.StripeWebhookEvents.RemoveRange(webhookEvents);

        await EnsureDemoUserAsync();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DemoResetResult(
            canceledSubscriptions,
            subscriptions.Count,
            billingCustomers.Count,
            webhookEvents.Count,
            DemoUserEmail);
    }

    private async Task EnsureDemoUserAsync()
    {
        var existing = await _userManager.FindByEmailAsync(DemoUserEmail);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = DemoUserEmail,
            Email = DemoUserEmail,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, "password1");
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Demo user seed failed: {errors}");
        }
    }
}
