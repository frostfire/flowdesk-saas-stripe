namespace FlowDesk.Contracts.Cases;

public sealed record CreateCaseRequest(string Title, string Description, string CustomerName);

public sealed record UpdateCaseRequest(string Title, string Description, string CustomerName);
