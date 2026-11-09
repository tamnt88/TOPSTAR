using Topstar.Application.Shipments;

namespace Topstar.Application.Tests;

public sealed class ShipmentTransitionTests
{
    [Fact] public void Created_can_be_dispatched() => Assert.True(ShipmentTransitions.CanDispatch(ShipmentStatuses.Created));
    [Fact] public void Returned_can_be_dispatched_again() => Assert.True(ShipmentTransitions.CanDispatch(ShipmentStatuses.Returned));
    [Fact] public void Only_in_transit_can_be_confirmed_delivered() { Assert.True(ShipmentTransitions.CanConfirmDelivered(ShipmentStatuses.InTransit)); Assert.False(ShipmentTransitions.CanConfirmDelivered(ShipmentStatuses.Issue)); }
    [Fact] public void Issue_can_only_resolve_to_in_transit_or_returned() { Assert.True(ShipmentTransitions.CanResolveIssue(ShipmentStatuses.Issue, ShipmentStatuses.InTransit)); Assert.True(ShipmentTransitions.CanResolveIssue(ShipmentStatuses.Issue, ShipmentStatuses.Returned)); Assert.False(ShipmentTransitions.CanResolveIssue(ShipmentStatuses.Issue, ShipmentStatuses.Delivered)); }
}
