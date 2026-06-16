using FlowDesk.Application.Billing;
using FlowDesk.Domain.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Billing;

public sealed class CurrentEntitlementService : ICurrentEntitlementService
{
    private readonly FlowDeskDbContext _dbContext;

    public CurrentEntitlementService(FlowDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CurrentEntitlements> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        var plan = subscription?.PlanCode ?? PlanCode.Free;
        var status = subscription?.Status ?? SubscriptionStatus.None;

        return new CurrentEntitlements(
            plan,
            status,
            PlanEntitlements.ForSubscription(plan, status));
    }
}
