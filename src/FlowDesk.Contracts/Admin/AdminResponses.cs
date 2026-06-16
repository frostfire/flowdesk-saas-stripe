namespace FlowDesk.Contracts.Admin;

public sealed record AdminBillingDebugResponse(
    AdminSubscriptionStateResponse Subscription,
    IReadOnlyList<AdminStripeWebhookEventResponse> WebhookEvents);

public sealed record AdminSubscriptionStateResponse(
    string Plan,
    string Status,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string? StripePriceId,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    DateTimeOffset? UpdatedAt);

public sealed record AdminStripeWebhookEventResponse(
    string StripeEventId,
    string Type,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ProcessedAt,
    string? ProcessingError);

public sealed record ResetDemoResponse(
    int CanceledSubscriptions,
    int ResetSubscriptions,
    int DeletedBillingCustomers,
    int DeletedWebhookEvents,
    string DemoUserEmail);
