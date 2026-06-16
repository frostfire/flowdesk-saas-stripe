using FlowDesk.Domain.Billing;

namespace FlowDesk.Application.Billing;

public sealed record CurrentEntitlements(
    PlanCode Plan,
    SubscriptionStatus Status,
    EntitlementSet Entitlements);
