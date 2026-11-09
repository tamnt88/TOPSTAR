using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignTargetingRuleTests
{
    [Fact] public void Empty_targeting_is_valid() => Assert.Null(CampaignTargetingRules.Validate(new()));
    [Fact] public void Rejects_unknown_gender() => Assert.Equal("Gender", CampaignTargetingRules.Validate(new() { Gender = "Unknown" }));
    [Fact] public void Rejects_reversed_followers() => Assert.Equal("Followers", CampaignTargetingRules.Validate(new() { MinimumFollowers = 100, MaximumFollowers = 99 }));
    [Fact] public void Rejects_engagement_above_one_hundred() => Assert.Equal("Engagement", CampaignTargetingRules.Validate(new() { MaximumEngagementRate = 101 }));
    [Fact] public void Accepts_inclusive_boundaries() => Assert.Null(CampaignTargetingRules.Validate(new() { Gender = "Female", MinimumFollowers = 0, MaximumFollowers = 0, MinimumEngagementRate = 0, MaximumEngagementRate = 100 }));
}
