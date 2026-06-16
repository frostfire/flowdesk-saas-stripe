namespace FlowDesk.Domain.Billing;

public enum SubscriptionStatus
{
    None,
    Trialing,
    Active,
    PastDue,
    Canceled,
    Incomplete,
    IncompleteExpired,
    Unpaid,
    Paused,
}
