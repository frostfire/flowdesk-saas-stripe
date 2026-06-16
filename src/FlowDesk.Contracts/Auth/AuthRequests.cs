namespace FlowDesk.Contracts.Auth;

public sealed record RegisterRequest(string Email, string Password, bool RememberMe = false);

public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
