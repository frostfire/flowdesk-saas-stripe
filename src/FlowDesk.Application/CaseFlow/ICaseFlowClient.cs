using FlowDesk.Contracts.Cases;

namespace FlowDesk.Application.CaseFlow;

public interface ICaseFlowClient
{
    Task<IReadOnlyList<CaseSummaryResponse>> ListCasesAsync(CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> GetCaseAsync(string id, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> ApproveCaseAsync(string id, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> RejectCaseAsync(string id, CancellationToken cancellationToken = default);
}
