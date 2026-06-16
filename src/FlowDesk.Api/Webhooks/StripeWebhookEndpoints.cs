using FlowDesk.Application.Billing;
using FlowDesk.Contracts.Webhooks;

namespace FlowDesk.Api.Webhooks;

public static class StripeWebhookEndpoints
{
    public static RouteGroupBuilder MapStripeWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/webhooks");
        group.MapPost("/stripe", HandleStripeWebhookAsync).AllowAnonymous();

        return group;
    }

    private static async Task<IResult> HandleStripeWebhookAsync(
        HttpRequest request,
        IStripeWebhookService webhooks,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValue("Stripe-Signature", out var signature))
        {
            return Results.BadRequest();
        }

        using var reader = new StreamReader(request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var result = await webhooks.ProcessAsync(payload, signature.ToString(), cancellationToken);

        return result.IsValid
            ? Results.Ok(new WebhookReceivedResponse(true))
            : Results.BadRequest();
    }
}
