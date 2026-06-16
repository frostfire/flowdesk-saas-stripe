using FlowDesk.Application.CaseFlow;

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

        return group;
    }
}
