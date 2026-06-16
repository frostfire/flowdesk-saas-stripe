using FlowDesk.Contracts.Admin;

namespace FlowDesk.Application.Billing;

public interface IBillingDiagnosticsService
{
    Task<AdminBillingDebugResponse> GetForUserAsync(
        string userId,
        int eventCount = 10,
        CancellationToken cancellationToken = default);
}
