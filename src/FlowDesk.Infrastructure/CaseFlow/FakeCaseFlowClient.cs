using FlowDesk.Application.CaseFlow;
using FlowDesk.Contracts.Cases;

namespace FlowDesk.Infrastructure.CaseFlow;

public sealed class FakeCaseFlowClient : ICaseFlowClient
{
    private static readonly CaseDetailResponse[] Cases =
    [
        new(
            "case-1001",
            "CF-1001",
            "Invoice approval delay",
            "Customer needs the pending invoice approval reviewed before renewal.",
            "Northwind Components",
            "Pending",
            new DateTimeOffset(2026, 6, 11, 9, 15, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 14, 16, 30, 0, TimeSpan.Zero)),
        new(
            "case-1002",
            "CF-1002",
            "Seat count review",
            "Ops asked for seat usage to be checked before plan expansion.",
            "Aperture Services",
            "InReview",
            new DateTimeOffset(2026, 6, 12, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 15, 10, 45, 0, TimeSpan.Zero)),
        new(
            "case-1003",
            "CF-1003",
            "Contract exception",
            "Finance flagged a contract exception for approval routing.",
            "Contoso Health",
            "Approved",
            new DateTimeOffset(2026, 6, 13, 11, 20, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 15, 18, 10, 0, TimeSpan.Zero)),
    ];

    public Task<IReadOnlyList<CaseSummaryResponse>> ListCasesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CaseSummaryResponse> cases = Cases
            .Select(ToSummary)
            .ToArray();

        return Task.FromResult(cases);
    }

    public Task<CaseDetailResponse?> GetCaseAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = Cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result);
    }

    public Task<CaseDetailResponse> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        var result = new CaseDetailResponse(
            "case-local",
            "CF-LOCAL",
            request.Title,
            request.Description,
            request.CustomerName,
            "Pending",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        return Task.FromResult(result);
    }

    public Task<CaseDetailResponse?> ApproveCaseAsync(string id, CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(id, "Approved");
    }

    public Task<CaseDetailResponse?> RejectCaseAsync(string id, CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(id, "Rejected");
    }

    private static Task<CaseDetailResponse?> UpdateStatusAsync(string id, string status)
    {
        var result = Cases.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (result is null)
        {
            return Task.FromResult<CaseDetailResponse?>(null);
        }

        return Task.FromResult<CaseDetailResponse?>(result with { Status = status, UpdatedAt = DateTimeOffset.UtcNow });
    }

    private static CaseSummaryResponse ToSummary(CaseDetailResponse detail)
    {
        return new CaseSummaryResponse(
            detail.Id,
            detail.Reference,
            detail.Title,
            detail.CustomerName,
            detail.Status,
            detail.CreatedAt,
            detail.UpdatedAt);
    }
}
