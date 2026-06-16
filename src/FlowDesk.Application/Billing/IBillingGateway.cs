using FlowDesk.Domain.Billing;

namespace FlowDesk.Application.Billing;

public interface IBillingGateway
{
    Task<string> CreateCheckoutSessionAsync(
        string userId,
        string email,
        PlanCode plan,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<string> CreatePortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default);
}
