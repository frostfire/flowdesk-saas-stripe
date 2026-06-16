namespace FlowDesk.Application.Billing;

public interface IBillingCustomerStore
{
    Task<string?> GetStripeCustomerIdForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
