namespace FlowDesk.Domain.Billing;

public static class PlanEntitlements
{
    private static readonly EntitlementSet Free = new(
        CanCreateCases: false,
        CanViewAnalytics: false,
        MaxCases: 10,
        MaxSeats: 1);

    private static readonly EntitlementSet Pro = new(
        CanCreateCases: true,
        CanViewAnalytics: true,
        MaxCases: 250,
        MaxSeats: 1);

    private static readonly EntitlementSet Team = new(
        CanCreateCases: true,
        CanViewAnalytics: true,
        MaxCases: 1000,
        MaxSeats: 10);

    public static EntitlementSet ForPlan(PlanCode plan)
    {
        return plan switch
        {
            PlanCode.Pro => Pro,
            PlanCode.Team => Team,
            _ => Free,
        };
    }

    public static EntitlementSet ForSubscription(PlanCode plan, SubscriptionStatus status)
    {
        return status is SubscriptionStatus.Active or SubscriptionStatus.Trialing
            ? ForPlan(plan)
            : Free;
    }
}
