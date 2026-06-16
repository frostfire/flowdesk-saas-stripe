namespace FlowDesk.Domain.Billing;

public sealed record EntitlementSet(
    bool CanCreateCases,
    bool CanViewAnalytics,
    int MaxCases,
    int MaxSeats);
