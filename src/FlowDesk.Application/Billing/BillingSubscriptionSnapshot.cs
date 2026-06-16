using FlowDesk.Domain.Billing;

namespace FlowDesk.Application.Billing;

public sealed record BillingSubscriptionSnapshot(
    string? UserId,
    string? CustomerId,
    string SubscriptionId,
    string? PriceId,
    PlanCode Plan,
    SubscriptionStatus Status,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd);
