using Topstar.Application.Campaigns;
using Topstar.Application.Reviews;

namespace Topstar.Application.Tests;

public sealed class ReviewRuleTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Rating_between_one_and_five_is_valid(byte rating)
        => Assert.True(ReviewRules.IsValidRating(rating));

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_outside_one_and_five_is_invalid(byte rating)
        => Assert.False(ReviewRules.IsValidRating(rating));

    [Fact]
    public void Review_is_available_only_after_campaign_completion()
    {
        Assert.True(ReviewRules.CanReviewCampaign(CampaignStatuses.Completed));
        Assert.False(ReviewRules.CanReviewCampaign(CampaignStatuses.InProgress));
        Assert.False(ReviewRules.CanReviewCampaign(CampaignStatuses.Published));
    }

    [Fact]
    public void Review_moderation_supports_hide_and_restore_only()
    {
        Assert.True(ReviewRules.CanModerate("Completed", "Hidden"));
        Assert.True(ReviewRules.CanModerate("Hidden", "Completed"));
        Assert.False(ReviewRules.CanModerate("Completed", "Completed"));
        Assert.False(ReviewRules.CanModerate("Hidden", "Pending"));
    }
}
