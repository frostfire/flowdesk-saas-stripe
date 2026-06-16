using FlowDesk.Application.Billing;
using FlowDesk.Domain.Billing;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FlowDesk.Infrastructure.Billing;

public sealed class StripeBillingGateway : IBillingGateway
{
    private readonly BillingOptions _options;

    public StripeBillingGateway(IOptions<BillingOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string userId,
        string email,
        PlanCode plan,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var priceId = PriceIdFor(plan);
        if (string.IsNullOrWhiteSpace(_options.StripeSecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        var service = new SessionService(new StripeClient(_options.StripeSecretKey));
        var session = await service.CreateAsync(
            new SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = email,
                ClientReferenceId = userId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["flowdesk_user_id"] = userId,
                    ["flowdesk_plan"] = plan.ToString(),
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        ["flowdesk_user_id"] = userId,
                        ["flowdesk_plan"] = plan.ToString(),
                    },
                },
            },
            cancellationToken: cancellationToken);

        return session.Url ?? throw new InvalidOperationException("Stripe checkout session URL was empty.");
    }

    public async Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.StripeSecretKey))
        {
            throw new InvalidOperationException("Stripe secret key is not configured.");
        }

        var service = new SubscriptionService(new StripeClient(_options.StripeSecretKey));
        var subscription = await service.GetAsync(
            subscriptionId,
            new SubscriptionGetOptions
            {
                Expand = ["items.data.price"],
            },
            cancellationToken: cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        var subscriptionItem = subscription.Items.Data.FirstOrDefault();
        var priceId = subscriptionItem?.Price?.Id;

        return new BillingSubscriptionSnapshot(
            subscription.Metadata.TryGetValue("flowdesk_user_id", out var userId) ? userId : null,
            subscription.CustomerId,
            subscription.Id,
            priceId,
            PlanForPriceId(priceId),
            StatusFor(subscription.Status),
            subscriptionItem?.CurrentPeriodEnd,
            subscription.CancelAtPeriodEnd);
    }

    private string PriceIdFor(PlanCode plan)
    {
        var priceId = plan switch
        {
            PlanCode.Pro => _options.StripePricePro,
            PlanCode.Team => _options.StripePriceTeam,
            _ => throw new InvalidOperationException("Free plan does not use Stripe checkout."),
        };

        if (string.IsNullOrWhiteSpace(priceId))
        {
            throw new InvalidOperationException($"Stripe price id is not configured for {plan}.");
        }

        return priceId;
    }

    private PlanCode PlanForPriceId(string? priceId)
    {
        if (priceId == _options.StripePricePro)
        {
            return PlanCode.Pro;
        }

        if (priceId == _options.StripePriceTeam)
        {
            return PlanCode.Team;
        }

        return PlanCode.Free;
    }

    private static SubscriptionStatus StatusFor(string? status)
    {
        return status switch
        {
            "trialing" => SubscriptionStatus.Trialing,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "incomplete" => SubscriptionStatus.Incomplete,
            "incomplete_expired" => SubscriptionStatus.IncompleteExpired,
            "unpaid" => SubscriptionStatus.Unpaid,
            "paused" => SubscriptionStatus.Paused,
            _ => SubscriptionStatus.None,
        };
    }
}
