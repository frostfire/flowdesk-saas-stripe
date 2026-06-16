namespace FlowDesk.Contracts.Billing;

public sealed record CreateCheckoutSessionRequest(string Plan, string SuccessUrl, string CancelUrl);

public sealed record CreatePortalSessionRequest(string ReturnUrl);
