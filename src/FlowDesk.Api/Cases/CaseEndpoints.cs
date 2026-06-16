using System.Security.Claims;
using FlowDesk.Api.Billing;
using FlowDesk.Application.Billing;
using FlowDesk.Application.CaseFlow;
using FlowDesk.Contracts.Cases;

namespace FlowDesk.Api.Cases;

public static class CaseEndpoints
{
    public static RouteGroupBuilder MapCaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/cases").RequireAuthorization();

        group.MapGet("/", async (ICaseFlowClient caseFlow, CancellationToken cancellationToken) =>
        {
            var cases = await caseFlow.ListCasesAsync(cancellationToken);
            return Results.Ok(cases);
        });
        group.MapGet("/{id}", GetCaseAsync);
        group.MapPost("/", CreateCaseAsync);
        group.MapPost("/{id}/approve", ApproveCaseAsync);
        group.MapPost("/{id}/reject", RejectCaseAsync);

        return group;
    }

    private static async Task<IResult> GetCaseAsync(
        string id,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        var result = await caseFlow.GetCaseAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateCaseAsync(
        CreateCaseRequest request,
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        if (!await CanCreateCasesAsync(user, entitlements, cancellationToken))
        {
            return Results.Forbid();
        }

        var result = await caseFlow.CreateCaseAsync(request, cancellationToken);
        return Results.Created($"/cases/{result.Id}", result);
    }

    private static async Task<IResult> ApproveCaseAsync(
        string id,
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        if (!await CanCreateCasesAsync(user, entitlements, cancellationToken))
        {
            return Results.Forbid();
        }

        var result = await caseFlow.ApproveCaseAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> RejectCaseAsync(
        string id,
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        if (!await CanCreateCasesAsync(user, entitlements, cancellationToken))
        {
            return Results.Forbid();
        }

        var result = await caseFlow.RejectCaseAsync(id, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<bool> CanCreateCasesAsync(
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        CancellationToken cancellationToken)
    {
        var current = await entitlements.GetForUserAsync(user.GetUserId(), cancellationToken);
        return current.Entitlements.CanCreateCases;
    }
}
