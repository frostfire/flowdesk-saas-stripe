namespace FlowDesk.Contracts.Auth;

public sealed record AuthUserResponse(string Id, string Email);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUserResponse User);

public sealed record WhoAmIResponse(string Id, string Email);
