namespace FlowDesk.Infrastructure.Billing;

public sealed class BillingOptions
{
    public string StripeSecretKey { get; set; } = string.Empty;

    public string StripePricePro { get; set; } = string.Empty;

    public string StripePriceTeam { get; set; } = string.Empty;

    public string StripeWebhookSecret { get; set; } = string.Empty;
}
