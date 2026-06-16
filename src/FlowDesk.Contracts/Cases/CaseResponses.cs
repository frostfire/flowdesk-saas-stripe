namespace FlowDesk.Contracts.Cases;

public sealed record CaseSummaryResponse(
    string Id,
    string Reference,
    string Title,
    string CustomerName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CaseDetailResponse(
    string Id,
    string Reference,
    string Title,
    string Description,
    string CustomerName,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
