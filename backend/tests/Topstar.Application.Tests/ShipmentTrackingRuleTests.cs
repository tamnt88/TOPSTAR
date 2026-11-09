using Topstar.Application.Shipments;

namespace Topstar.Application.Tests;

public sealed class ShipmentTrackingRuleTests
{
    [Theory]
    [InlineData("PickedUp", true)]
    [InlineData("AtHub", true)]
    [InlineData("OutForDelivery", true)]
    [InlineData("DeliveryAttempt", true)]
    [InlineData("Exception", true)]
    [InlineData("Other", true)]
    [InlineData("Delivered", false)]
    [InlineData("", false)]
    public void EventCode_UsesCanonicalSet(string code, bool expected)
        => Assert.Equal(expected, ShipmentTrackingRules.IsValidEventCode(code));

    [Theory]
    [InlineData(ShipmentStatuses.InTransit, true)]
    [InlineData(ShipmentStatuses.Issue, true)]
    [InlineData(ShipmentStatuses.Created, false)]
    [InlineData(ShipmentStatuses.Delivered, false)]
    [InlineData(ShipmentStatuses.Returned, false)]
    public void ManualCheckpoint_OnlyDuringActiveDelivery(string status, bool expected)
        => Assert.Equal(expected, ShipmentTrackingRules.CanAdd(status));
}
