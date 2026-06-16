namespace FlowDesk.Application.Billing;

public interface IDemoResetService
{
    Task<DemoResetResult> ResetAsync(CancellationToken cancellationToken = default);
}

public sealed record DemoResetResult(
    int CanceledSubscriptions,
    int ResetSubscriptions,
    int DeletedBillingCustomers,
    int DeletedWebhookEvents,
    string DemoUserEmail);
