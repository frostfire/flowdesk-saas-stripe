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
            },
            cancellationToken: cancellationToken);

        return session.Url ?? throw new InvalidOperationException("Stripe checkout session URL was empty.");
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
}
