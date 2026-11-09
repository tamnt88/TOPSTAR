using Topstar.Application.Deliverables;
using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class SubmissionTransitionTests
{
    [Theory]
    [InlineData(SubmissionStatuses.NotSubmitted)]
    [InlineData(SubmissionStatuses.NeedRevision)]
    [InlineData(SubmissionStatuses.Submitted)]
    public void Koc_can_submit_only_from_actionable_states(string status)
        => Assert.True(SubmissionTransitions.CanSubmit(status));

    [Theory]
    [InlineData(SubmissionStatuses.Approved)]
    [InlineData(SubmissionStatuses.Rejected)]
    public void Koc_cannot_submit_from_locked_states(string status)
        => Assert.False(SubmissionTransitions.CanSubmit(status));

    [Fact]
    public void Brand_can_review_only_a_submitted_item()
    {
        Assert.True(SubmissionTransitions.CanReview(SubmissionStatuses.Submitted));
        Assert.False(SubmissionTransitions.CanReview(SubmissionStatuses.NotSubmitted));
        Assert.False(SubmissionTransitions.CanReview(SubmissionStatuses.NeedRevision));
        Assert.False(SubmissionTransitions.CanReview(SubmissionStatuses.Approved));
    }

    [Theory]
    [InlineData(SubmissionStatuses.Approved)]
    [InlineData(SubmissionStatuses.NeedRevision)]
    [InlineData(SubmissionStatuses.Rejected)]
    public void Review_decisions_are_explicit(string decision)
        => Assert.True(SubmissionTransitions.IsReviewDecision(decision));

    [Theory]
    [InlineData(SubmissionStatuses.Approved)]
    [InlineData(SubmissionStatuses.Rejected)]
    public void Approved_and_rejected_are_final_results(string status)
        => Assert.True(SubmissionTransitions.IsResolved(status));

    [Fact]
    public void In_progress_campaign_can_complete_when_every_submission_is_resolved()
    {
        Assert.True(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 3, 3));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 3, 2));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 0, 0));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.Completed, 3, 3));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 3, 3, bookingReady: false));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 3, 3, shipmentReady: false));
        Assert.False(CampaignCompletionRules.CanComplete(CampaignStatuses.InProgress, 3, 3, noActiveDisputes: false));
    }
}
