namespace FlowDesk.Infrastructure.Billing;

public sealed class BillingCustomer
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string StripeCustomerId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
