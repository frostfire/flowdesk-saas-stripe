using System.Security.Claims;
using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Billing;
using FlowDesk.Domain.Billing;

namespace FlowDesk.Api.Billing;

public static class BillingEndpoints
{
    public static RouteGroupBuilder MapBillingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/entitlements/me", GetCurrentEntitlementsAsync)
            .RequireAuthorization();

        var group = endpoints.MapGroup("/billing").RequireAuthorization();
        group.MapPost("/checkout-session", CreateCheckoutSessionAsync);

        return group;
    }

    private static async Task<IResult> GetCurrentEntitlementsAsync(
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        CancellationToken cancellationToken)
    {
        var current = await entitlements.GetForUserAsync(user.GetUserId(), cancellationToken);
        return Results.Ok(EntitlementMapper.ToResponse(current));
    }

    private static async Task<IResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        ClaimsPrincipal user,
        IBillingGateway billingGateway,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PlanCode>(request.Plan, ignoreCase: true, out var plan) || plan == PlanCode.Free)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["plan"] = ["Choose Pro or Team."],
            });
        }

        var url = await billingGateway.CreateCheckoutSessionAsync(
            user.GetUserId(),
            user.GetEmail(),
            plan,
            request.SuccessUrl,
            request.CancelUrl,
            cancellationToken);

        return Results.Ok(new BillingSessionResponse(url));
    }
}
