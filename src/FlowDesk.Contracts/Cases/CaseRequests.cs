namespace FlowDesk.Contracts.Cases;

public sealed record CreateCaseRequest(string Title, string Description, string CustomerName);
