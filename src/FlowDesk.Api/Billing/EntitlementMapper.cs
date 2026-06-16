using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Entitlements;

namespace FlowDesk.Api.Billing;

internal static class EntitlementMapper
{
    public static CurrentEntitlementsResponse ToResponse(CurrentEntitlements current)
    {
        return new CurrentEntitlementsResponse(
            current.Plan.ToString(),
            current.Status.ToString(),
            new EntitlementSetResponse(
                current.Entitlements.CanCreateCases,
                current.Entitlements.CanViewAnalytics,
                current.Entitlements.MaxCases,
                current.Entitlements.MaxSeats));
    }
}
