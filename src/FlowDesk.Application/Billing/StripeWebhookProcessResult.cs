namespace FlowDesk.Application.Billing;

public sealed record StripeWebhookProcessResult(bool IsValid, bool IsDuplicate);
