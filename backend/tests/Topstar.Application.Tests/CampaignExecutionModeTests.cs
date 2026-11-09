using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignExecutionModeTests
{
    [Theory]
    [InlineData(CampaignExecutionModes.FoodReviewOnsite)]
    [InlineData(CampaignExecutionModes.StoreReviewOnsite)]
    [InlineData(CampaignExecutionModes.ServiceReviewOnsite)]
    [InlineData(CampaignExecutionModes.EventOnsite)]
    public void Onsite_modes_require_a_physical_location(string mode)
        => Assert.True(CampaignExecutionModes.IsOnsite(mode));

    [Theory]
    [InlineData(CampaignExecutionModes.RemoteProductReview)]
    [InlineData(CampaignExecutionModes.LivestreamOnline)]
    public void Remote_modes_do_not_require_a_physical_location(string mode)
        => Assert.False(CampaignExecutionModes.IsOnsite(mode));

    [Fact]
    public void Unknown_mode_is_rejected()
        => Assert.False(CampaignExecutionModes.IsValid("Unknown"));
}
