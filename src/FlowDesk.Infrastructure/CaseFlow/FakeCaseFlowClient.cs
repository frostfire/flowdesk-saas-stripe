using System.Collections.Concurrent;
using FlowDesk.Application.CaseFlow;
using FlowDesk.Contracts.Cases;

namespace FlowDesk.Infrastructure.CaseFlow;

public sealed class FakeCaseFlowClient : ICaseFlowClient
{
    private readonly ConcurrentDictionary<string, List<CaseDetailResponse>> _casesByUser = new();

    private static readonly CaseDetailResponse[] SeedCases =
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

    public Task<IReadOnlyList<CaseSummaryResponse>> ListCasesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var store = GetStore(userId);
        CaseSummaryResponse[] cases;
        lock (store)
        {
            cases = store.Select(ToSummary).ToArray();
        }

        return Task.FromResult<IReadOnlyList<CaseSummaryResponse>>(cases);
    }

    public Task<CaseDetailResponse?> GetCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var store = GetStore(userId);
        CaseDetailResponse? result;
        lock (store)
        {
            result = store.FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(result);
    }

    public Task<CaseDetailResponse> CreateCaseAsync(
        string userId,
        CreateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var store = GetStore(userId);
        var now = DateTimeOffset.UtcNow;
        var result = new CaseDetailResponse(
            $"case-{Guid.NewGuid():N}",
            $"CF-{Random.Shared.Next(2000, 9999)}",
            request.Title,
            request.Description,
            request.CustomerName,
            "Pending",
            now,
            now);

        lock (store)
        {
            store.Add(result);
        }

        return Task.FromResult(result);
    }

    public Task<CaseDetailResponse?> UpdateCaseAsync(
        string userId,
        string id,
        UpdateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var store = GetStore(userId);
        lock (store)
        {
            var index = store.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return Task.FromResult<CaseDetailResponse?>(null);
            }

            var updated = store[index] with
            {
                Title = request.Title,
                Description = request.Description,
                CustomerName = request.CustomerName,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            store[index] = updated;
            return Task.FromResult<CaseDetailResponse?>(updated);
        }
    }

    public Task<bool> DeleteCaseAsync(string userId, string id, CancellationToken cancellationToken = default)
    {
        var store = GetStore(userId);
        lock (store)
        {
            var removed = store.RemoveAll(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(removed > 0);
        }
    }

    public Task<CaseDetailResponse?> ApproveCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(userId, id, "Approved");
    }

    public Task<CaseDetailResponse?> RejectCaseAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusAsync(userId, id, "Rejected");
    }

    private Task<CaseDetailResponse?> UpdateStatusAsync(string userId, string id, string status)
    {
        var store = GetStore(userId);
        lock (store)
        {
            var index = store.FindIndex(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return Task.FromResult<CaseDetailResponse?>(null);
            }

            var updated = store[index] with { Status = status, UpdatedAt = DateTimeOffset.UtcNow };
            store[index] = updated;
            return Task.FromResult<CaseDetailResponse?>(updated);
        }
    }

    private List<CaseDetailResponse> GetStore(string userId)
    {
        return _casesByUser.GetOrAdd(userId, _ => SeedCases.Select(item => item with { }).ToList());
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
