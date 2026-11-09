using Topstar.Application.Campaigns;
namespace Topstar.Application.Tests;
public class CampaignLifecycleRuleTests
{
    [Theory]
    [InlineData("Published")]
    [InlineData("InProgress")]
    public void Pause_requires_no_started_obligations(string status)
    {
        Assert.True(CampaignLifecycleRules.CanPause(status,false));
        Assert.False(CampaignLifecycleRules.CanPause(status,true));
    }
    [Fact]
    public void Cannot_cancel_over_work_or_uncertain_payment()
    {
        Assert.True(CampaignLifecycleRules.CanCancel("Paused",false,false));
        Assert.False(CampaignLifecycleRules.CanCancel("Published",true,false));
        Assert.False(CampaignLifecycleRules.CanCancel("Published",false,true));
        Assert.False(CampaignLifecycleRules.CanCancel("Completed",false,false));
    }
    [Fact]
    public void Resume_only_restores_a_valid_operational_status()
    {
        Assert.True(CampaignLifecycleRules.CanResume("Paused","Published"));
        Assert.False(CampaignLifecycleRules.CanResume("Cancelled","Published"));
        Assert.False(CampaignLifecycleRules.CanResume("Paused","Completed"));
    }
}
