using FlowDesk.Contracts.Cases;

namespace FlowDesk.Application.CaseFlow;

public interface ICaseFlowClient
{
    Task<IReadOnlyList<CaseSummaryResponse>> ListCasesAsync(string userId, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> GetCaseAsync(string userId, string id, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse> CreateCaseAsync(
        string userId,
        CreateCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> UpdateCaseAsync(
        string userId,
        string id,
        UpdateCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCaseAsync(string userId, string id, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> ApproveCaseAsync(string userId, string id, CancellationToken cancellationToken = default);

    Task<CaseDetailResponse?> RejectCaseAsync(string userId, string id, CancellationToken cancellationToken = default);
}
