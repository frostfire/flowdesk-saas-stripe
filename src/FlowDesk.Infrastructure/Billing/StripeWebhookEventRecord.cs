namespace FlowDesk.Infrastructure.Billing;

public sealed class StripeWebhookEventRecord
{
    public Guid Id { get; set; }

    public string StripeEventId { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? ProcessingError { get; set; }
}
