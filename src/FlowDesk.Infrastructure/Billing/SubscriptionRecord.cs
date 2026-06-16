using FlowDesk.Domain.Billing;

namespace FlowDesk.Infrastructure.Billing;

public sealed class SubscriptionRecord
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? StripeCustomerId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public string? StripePriceId { get; set; }

    public PlanCode PlanCode { get; set; } = PlanCode.Free;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.None;

    public DateTimeOffset? CurrentPeriodEnd { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
