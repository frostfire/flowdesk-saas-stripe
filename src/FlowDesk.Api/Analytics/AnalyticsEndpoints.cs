using System.Security.Claims;
using FlowDesk.Api.Billing;
using FlowDesk.Application.Billing;
using FlowDesk.Application.CaseFlow;
using FlowDesk.Contracts.Analytics;

namespace FlowDesk.Api.Analytics;

public static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/analytics").RequireAuthorization();
        group.MapGet("/summary", GetSummaryAsync);

        return group;
    }

    private static async Task<IResult> GetSummaryAsync(
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        var current = await entitlements.GetForUserAsync(user.GetUserId(), cancellationToken);
        if (!current.Entitlements.CanViewAnalytics)
        {
            return Results.Forbid();
        }

        var cases = await caseFlow.ListCasesAsync(cancellationToken);
        var summary = new AnalyticsSummaryResponse(
            cases.Count,
            cases.Count(item => IsStatus(item.Status, "Pending") || IsStatus(item.Status, "InReview")),
            cases.Count(item => IsStatus(item.Status, "Approved")),
            cases.Count(item => IsStatus(item.Status, "Rejected")));

        return Results.Ok(summary);
    }

    private static bool IsStatus(string value, string status)
    {
        return value.Equals(status, StringComparison.OrdinalIgnoreCase);
    }
}
