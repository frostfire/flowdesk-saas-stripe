using FlowDesk.Application.Billing;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Billing;

public sealed class BillingCustomerStore : IBillingCustomerStore
{
    private readonly FlowDeskDbContext _dbContext;

    public BillingCustomerStore(FlowDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetStripeCustomerIdForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _dbContext.BillingCustomers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return customer?.StripeCustomerId;
    }
}
