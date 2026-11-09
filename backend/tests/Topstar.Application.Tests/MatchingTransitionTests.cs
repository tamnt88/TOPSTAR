using Topstar.Application.Campaigns;
using Topstar.Application.Matching;

namespace Topstar.Application.Tests;

public sealed class MatchingTransitionTests
{
    [Fact]
    public void Applied_application_of_published_campaign_can_be_shortlisted()
        => Assert.True(MatchingTransitions.CanShortlist(ApplicationStatuses.Applied, CampaignStatuses.Published));

    [Theory]
    [InlineData("Shortlisted", CampaignStatuses.Published)]
    [InlineData(ApplicationStatuses.Applied, CampaignStatuses.Submitted)]
    [InlineData(ApplicationStatuses.Withdrawn, CampaignStatuses.Published)]
    public void Invalid_application_or_campaign_state_cannot_be_shortlisted(string applicationStatus, string campaignStatus)
        => Assert.False(MatchingTransitions.CanShortlist(applicationStatus, campaignStatus));

    [Fact]
    public void Shortlisted_application_of_published_campaign_can_be_confirmed()
        => Assert.True(MatchingTransitions.CanConfirm("Shortlisted", CampaignStatuses.Published));

    [Theory]
    [InlineData(ApplicationStatuses.Applied, CampaignStatuses.Published)]
    [InlineData("Shortlisted", CampaignStatuses.Completed)]
    [InlineData("Confirmed", CampaignStatuses.Published)]
    public void Invalid_application_or_campaign_state_cannot_be_confirmed(string applicationStatus, string campaignStatus)
        => Assert.False(MatchingTransitions.CanConfirm(applicationStatus, campaignStatus));

    [Fact]
    public void Brand_can_select_only_when_policy_allows_and_quota_remains()
    {
        Assert.True(MatchingTransitions.CanBrandSelect(true, false, 2, 3));
        Assert.False(MatchingTransitions.CanBrandSelect(false, false, 0, null));
        Assert.False(MatchingTransitions.CanBrandSelect(true, true, 0, 3));
        Assert.False(MatchingTransitions.CanBrandSelect(true, false, 3, 3));
    }

    [Fact]
    public void Assigned_package_can_finalize_a_non_empty_shortlist()
    {
        Assert.True(MatchingTransitions.CanFinalize(false, false, 2, 2, null));
        Assert.False(MatchingTransitions.CanFinalize(false, false, 0, 0, null));
    }

    [Fact]
    public void Selection_package_requires_at_least_one_choice_within_quota()
    {
        Assert.True(MatchingTransitions.CanFinalize(true, false, 5, 3, 3));
        Assert.False(MatchingTransitions.CanFinalize(true, false, 5, 0, 3));
        Assert.False(MatchingTransitions.CanFinalize(true, false, 5, 4, 3));
        Assert.False(MatchingTransitions.CanFinalize(true, true, 5, 3, 3));
    }

    [Fact]
    public void Package_policies_must_cover_all_budget_values_without_gaps()
    {
        var policies = new[]
        {
            new PackagePolicyRange(0, 4_999_999.99m, true),
            new PackagePolicyRange(5_000_000m, 19_999_999.99m, true),
            new PackagePolicyRange(20_000_000m, null, true)
        };

        Assert.Null(PackagePolicyRules.ValidateCoverage(policies));
    }

    [Theory]
    [InlineData(5_000_000.01, "khoảng trống")]
    [InlineData(4_999_999.99, "chồng lấn")]
    public void Package_policy_gap_or_overlap_is_rejected(decimal secondMin, string expectedMessage)
    {
        var policies = new[]
        {
            new PackagePolicyRange(0, 4_999_999.99m, true),
            new PackagePolicyRange(secondMin, null, true)
        };

        Assert.Contains(expectedMessage, PackagePolicyRules.ValidateCoverage(policies));
    }
}
