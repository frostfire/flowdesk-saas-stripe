namespace FlowDesk.Application.Billing;

public interface ICurrentEntitlementService
{
    Task<CurrentEntitlements> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
}
