using System.Security.Claims;
using FlowDesk.Api.Billing;
using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Admin;

namespace FlowDesk.Api.Admin;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/admin").RequireAuthorization();
        group.MapGet("/billing-debug", GetBillingDebugAsync);
        group.MapPost("/reset-demo", ResetDemoAsync);

        return group;
    }

    private static async Task<IResult> GetBillingDebugAsync(
        ClaimsPrincipal user,
        IBillingDiagnosticsService diagnostics,
        CancellationToken cancellationToken)
    {
        var response = await diagnostics.GetForUserAsync(user.GetUserId(), cancellationToken: cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> ResetDemoAsync(
        IDemoResetService resetService,
        CancellationToken cancellationToken)
    {
        var result = await resetService.ResetAsync(cancellationToken);

        return Results.Ok(new ResetDemoResponse(
            result.CanceledSubscriptions,
            result.ResetSubscriptions,
            result.DeletedBillingCustomers,
            result.DeletedWebhookEvents,
            result.DemoUserEmail));
    }
}
