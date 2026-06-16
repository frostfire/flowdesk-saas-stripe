namespace FlowDesk.Contracts.Analytics;

public sealed record AnalyticsSummaryResponse(
    int TotalCases,
    int PendingCases,
    int ApprovedCases,
    int RejectedCases);
