using FlowDesk.Domain.Billing;
using Xunit;

namespace FlowDesk.Api.Tests;

public sealed class PlanEntitlementTests
{
    [Theory]
    [InlineData(PlanCode.Free, false, false, 10, 1)]
    [InlineData(PlanCode.Pro, true, true, 250, 1)]
    [InlineData(PlanCode.Team, true, true, 1000, 10)]
    public void ForPlan_ReturnsExpectedEntitlements(
        PlanCode plan,
        bool canCreateCases,
        bool canViewAnalytics,
        int maxCases,
        int maxSeats)
    {
        var entitlements = PlanEntitlements.ForPlan(plan);

        Assert.Equal(canCreateCases, entitlements.CanCreateCases);
        Assert.Equal(canViewAnalytics, entitlements.CanViewAnalytics);
        Assert.Equal(maxCases, entitlements.MaxCases);
        Assert.Equal(maxSeats, entitlements.MaxSeats);
    }

    [Fact]
    public void ForSubscription_ReturnsFreeForPastDue()
    {
        var entitlements = PlanEntitlements.ForSubscription(PlanCode.Team, SubscriptionStatus.PastDue);

        Assert.False(entitlements.CanCreateCases);
        Assert.False(entitlements.CanViewAnalytics);
    }
}
