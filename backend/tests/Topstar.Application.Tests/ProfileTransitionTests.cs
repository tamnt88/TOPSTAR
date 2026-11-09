using Topstar.Application.Profiles;

namespace Topstar.Application.Tests;

public sealed class ProfileTransitionTests
{
    [Fact]
    public void Admin_can_review_or_adjust_processed_profiles()
    {
        Assert.True(ProfileTransitions.CanReview(ProfileStatuses.PendingReview));
        Assert.True(ProfileTransitions.CanReview(ProfileStatuses.NeedUpdate));
        Assert.True(ProfileTransitions.CanReview(ProfileStatuses.Approved));
        Assert.True(ProfileTransitions.CanReview(ProfileStatuses.Rejected));
        Assert.True(ProfileTransitions.CanReview(ProfileStatuses.Locked));
        Assert.False(ProfileTransitions.CanReview(ProfileStatuses.Draft));
    }

    [Theory]
    [InlineData(ProfileStatuses.Approved)]
    [InlineData(ProfileStatuses.NeedUpdate)]
    [InlineData(ProfileStatuses.Rejected)]
    public void Supported_review_decisions_are_valid(string decision)
        => Assert.True(ProfileTransitions.IsValidDecision(decision));

    [Theory]
    [InlineData(ProfileStatuses.Draft)]
    [InlineData(ProfileStatuses.PendingReview)]
    [InlineData(ProfileStatuses.Locked)]
    public void Unsupported_review_decisions_are_invalid(string decision)
        => Assert.False(ProfileTransitions.IsValidDecision(decision));

    [Fact]
    public void Complete_pending_profile_can_browse_but_cannot_use_protected_features()
    {
        var access = new ProfileAccessResult(true, true, false, ProfileStatuses.PendingReview, []);

        Assert.True(access.CanBrowseFeatures);
        Assert.False(access.CanUseFeatures);
    }

    [Theory]
    [InlineData(ProfileStatuses.Draft)]
    [InlineData(ProfileStatuses.NeedUpdate)]
    [InlineData(ProfileStatuses.Rejected)]
    [InlineData(ProfileStatuses.Locked)]
    public void Non_reviewable_profile_cannot_browse_protected_portal(string status)
    {
        var access = new ProfileAccessResult(true, true, false, status, []);

        Assert.False(access.CanBrowseFeatures);
    }
}
