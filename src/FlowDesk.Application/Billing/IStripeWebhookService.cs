namespace FlowDesk.Application.Billing;

public interface IStripeWebhookService
{
    Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);
}
