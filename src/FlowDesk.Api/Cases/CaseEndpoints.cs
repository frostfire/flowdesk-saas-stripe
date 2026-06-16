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

        group.MapGet("/", async (ClaimsPrincipal user, ICaseFlowClient caseFlow, CancellationToken cancellationToken) =>
        {
            var cases = await caseFlow.ListCasesAsync(user.GetUserId(), cancellationToken);
            return Results.Ok(cases);
        });
        group.MapGet("/{id}", GetCaseAsync);
        group.MapPost("/", CreateCaseAsync);
        group.MapPut("/{id}", UpdateCaseAsync);
        group.MapDelete("/{id}", DeleteCaseAsync);
        group.MapPost("/{id}/approve", ApproveCaseAsync);
        group.MapPost("/{id}/reject", RejectCaseAsync);

        return group;
    }

    private static async Task<IResult> GetCaseAsync(
        string id,
        ClaimsPrincipal user,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        var result = await caseFlow.GetCaseAsync(user.GetUserId(), id, cancellationToken);
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

        var result = await caseFlow.CreateCaseAsync(user.GetUserId(), request, cancellationToken);
        return Results.Created($"/cases/{result.Id}", result);
    }

    private static async Task<IResult> UpdateCaseAsync(
        string id,
        UpdateCaseRequest request,
        ClaimsPrincipal user,
        ICurrentEntitlementService entitlements,
        ICaseFlowClient caseFlow,
        CancellationToken cancellationToken)
    {
        if (!await CanCreateCasesAsync(user, entitlements, cancellationToken))
        {
            return Results.Forbid();
        }

        var result = await caseFlow.UpdateCaseAsync(user.GetUserId(), id, request, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> DeleteCaseAsync(
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

        return await caseFlow.DeleteCaseAsync(user.GetUserId(), id, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
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

        var result = await caseFlow.ApproveCaseAsync(user.GetUserId(), id, cancellationToken);
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

        var result = await caseFlow.RejectCaseAsync(user.GetUserId(), id, cancellationToken);
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
