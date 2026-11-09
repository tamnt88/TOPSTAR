using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignTransitionTests
{
    [Theory]
    [InlineData(CampaignStatuses.Draft, true)]
    [InlineData(CampaignStatuses.NeedEdit, true)]
    [InlineData(CampaignStatuses.Submitted, true)]
    [InlineData(CampaignStatuses.Published, true)]
    [InlineData(CampaignStatuses.InProgress, true)]
    [InlineData(CampaignStatuses.Completed, false)]
    [InlineData(CampaignStatuses.Cancelled, false)]
    [InlineData(CampaignStatuses.Rejected, false)]
    [InlineData("Unknown", false)]
    public void Images_can_only_change_before_work_starts(string status, bool permitted)
    {
        Assert.Equal(permitted, CampaignTransitions.CanEditImages(status, false));
        Assert.False(CampaignTransitions.CanEditImages(status, true));
    }

    [Theory]
    [InlineData(CampaignStatuses.Draft)]
    [InlineData(CampaignStatuses.NeedEdit)]
    public void Editable_statuses_can_be_updated_and_submitted(string status)
    {
        Assert.True(CampaignTransitions.CanEdit(status));
        Assert.True(CampaignTransitions.CanSubmit(status));
    }

    [Theory]
    [InlineData(CampaignStatuses.Submitted)]
    [InlineData(CampaignStatuses.Published)]
    [InlineData(CampaignStatuses.Rejected)]
    [InlineData(CampaignStatuses.InProgress)]
    [InlineData(CampaignStatuses.Completed)]
    public void Non_editable_statuses_cannot_be_updated_or_submitted(string status)
    {
        Assert.False(CampaignTransitions.CanEdit(status));
        Assert.False(CampaignTransitions.CanSubmit(status));
    }

    [Fact]
    public void Only_submitted_campaign_can_be_reviewed()
    {
        Assert.True(CampaignTransitions.CanReview(CampaignStatuses.Submitted));
        Assert.False(CampaignTransitions.CanReview(CampaignStatuses.Draft));
        Assert.False(CampaignTransitions.CanReview(CampaignStatuses.Published));
    }

    [Fact]
    public void Only_published_campaign_accepts_applications()
    {
        Assert.True(CampaignTransitions.CanApply(CampaignStatuses.Published));
        Assert.False(CampaignTransitions.CanApply(CampaignStatuses.Submitted));
        Assert.False(CampaignTransitions.CanApply(CampaignStatuses.Completed));
    }
}
