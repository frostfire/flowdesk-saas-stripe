namespace FlowDesk.Contracts.Entitlements;

public sealed record EntitlementSetResponse(
    bool CanCreateCases,
    bool CanViewAnalytics,
    int MaxCases,
    int MaxSeats);

public sealed record CurrentEntitlementsResponse(
    string Plan,
    string Status,
    EntitlementSetResponse Entitlements);
