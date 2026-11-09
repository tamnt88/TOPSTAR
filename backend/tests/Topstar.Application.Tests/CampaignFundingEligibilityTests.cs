using Topstar.Application.Billing;
using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignFundingEligibilityTests
{
    [Theory]
    [InlineData(300000, 15000000, true)]
    [InlineData(15000000, 15000000, true)]
    [InlineData(15000001, 15000000, false)]
    [InlineData(0, 15000000, false)]
    [InlineData(-1, 15000000, false)]
    [InlineData(1.5, 15000000, false)]
    [InlineData(1, 0, false)]
    public void Prepayment_is_a_positive_whole_amount_within_remaining_budget(double amount, double remaining, bool expected)
        => Assert.Equal(expected, CampaignFundingRules.IsValidPrepayment((decimal)amount, (decimal)remaining));

    [Fact]
    public void Payment_step_requires_saved_nonempty_valid_allocations_and_draft_status()
    {
        var item=System.Text.Json.JsonSerializer.Deserialize<CampaignFundingItem>("""
        {"Id":"11111111-1111-1111-1111-111111111111","CampaignBudget":100,"Status":"Draft","ConfirmedKocCount":2,"DesiredKocCount":2,"IsShortlistFinalized":true,"CampaignStatus":"Published","SavedAllocationsMatch":true,"Allocations":[{"ParticipationId":"22222222-2222-2222-2222-222222222222","Amount":50},{"ParticipationId":"33333333-3333-3333-3333-333333333333","Amount":50}]}
        """)!;
        Assert.True(item.CanSubmit);
        Assert.False((item with {Id=null}).CanSubmit);
        Assert.False((item with {Allocations=[]}).CanSubmit);
        Assert.False((item with {SavedAllocationsMatch=false}).CanSubmit);
        Assert.True((item with {IsShortlistFinalized=false}).CanSubmit);
        Assert.True((item with {ConfirmedKocCount=1}).CanSubmit);
        Assert.False((item with {Status=CampaignFundingStatuses.Paid}).CanEditAllocations);
        Assert.False((item with {Status=CampaignFundingStatuses.Cancelled}).CanSubmit);
        Assert.True((item with {Status=CampaignFundingStatuses.Cancelled}).CanEditAllocations);
    }

    [Fact]
    public void New_staged_batch_can_be_edited_before_its_amount_is_entered()
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<CampaignFundingItem>("""
        {"CampaignBudget":0,"TotalCampaignBudget":2000000,"PreviouslyAllocatedBudget":0,"Status":"Draft","ConfirmedKocCount":1,"DesiredKocCount":3,"CampaignStatus":"Published","Allocations":[{"ParticipationId":"22222222-2222-2222-2222-222222222222","Amount":0}]}
        """)!;
        Assert.Null(item.EligibilityError);
        Assert.True(item.CanEditAllocations);
        Assert.False(item.CanSubmit);
        Assert.Equal(2000000m, item.EligibilityBudget);
    }

    [Fact]
    public void Full_prepaid_escrow_can_be_submitted_before_any_koc_is_confirmed()
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<CampaignFundingItem>("""
        {"Id":"11111111-1111-1111-1111-111111111111","CampaignBudget":2000000,"AllocationMode":"Prepaid","Status":"Draft","ConfirmedKocCount":0,"DesiredKocCount":3,"CampaignStatus":"Published","SavedAllocationsMatch":false,"Allocations":[]}
        """)!;
        Assert.NotNull(item.EligibilityError);
        Assert.False(item.CanEditAllocations);
        Assert.True(item.CanSubmit);
        Assert.False((item with { CampaignBudget=0 }).CanSubmit);
    }

    [Fact]
    public void Paid_prepaid_reserve_allows_later_koc_allocation_without_new_payment()
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<CampaignFundingItem>("""
        {"Id":"11111111-1111-1111-1111-111111111111","CampaignBudget":1500000,"AllocationMode":"Prepaid","Status":"Paid","ConfirmedKocCount":1,"DesiredKocCount":3,"CampaignStatus":"Published","IsAllocatingPrepaidReserve":true,"Allocations":[{"ParticipationId":"22222222-2222-2222-2222-222222222222","Amount":0}]}
        """)!;
        Assert.True(item.CanEditAllocations);
        Assert.False(item.CanSubmit);
    }
    [Theory]
    [InlineData(CampaignStatuses.Published, 100, false, 2, 0, false)]
    [InlineData(CampaignStatuses.Published, 100, false, 2, 2, true)]
    [InlineData(CampaignStatuses.Published, 100, true, 2, 1, true)]
    [InlineData(CampaignStatuses.Published, 100, true, 0, 1, false)]
    [InlineData(CampaignStatuses.Draft, 100, true, 2, 2, false)]
    [InlineData(CampaignStatuses.Completed, 100, true, 2, 2, false)]
    [InlineData(CampaignStatuses.Cancelled, 100, true, 2, 2, false)]
    [InlineData(CampaignStatuses.Published, 0, true, 2, 2, false)]
    [InlineData(CampaignStatuses.Published, 1, true, 2, 2, false)]
    [InlineData(CampaignStatuses.Published, 100, true, 2, 2, true)]
    [InlineData(CampaignStatuses.InProgress, 100, true, 2, 2, true)]
    public void Allocation_requires_approved_campaign_at_least_one_confirmed_koc_and_valid_budget(string status, int budget, bool finalized, int desired, int confirmed, bool valid)
        => Assert.Equal(valid, CampaignFundingRules.ValidateEligibility(status,budget,finalized,desired,confirmed) is null);

    [Theory]
    [InlineData("8500000",true,8500000)]
    [InlineData("8.500.000",true,8500000)]
    [InlineData("-100",false,0)]
    [InlineData("1.5",false,0)]
    [InlineData("1,5",false,0)]
    [InlineData("1e6",false,0)]
    [InlineData("",false,0)]
    public void Money_parser_accepts_only_whole_VND(string raw,bool valid,int amount)
    {
        Assert.Equal(valid,CampaignFundingRules.TryParseVnd(raw,out var parsed));
        if(valid)Assert.Equal((decimal)amount,parsed);
    }

    [Fact]
    public void Empty_lists_cannot_validate_even_with_zero_budget()
        => Assert.NotNull(CampaignFundingRules.ValidateAllocations(0,new Dictionary<Guid,decimal>(),Array.Empty<Guid>()));

    [Theory]
    [InlineData(0,100)]
    [InlineData(-10,110)]
    [InlineData(50,49)]
    [InlineData(51,50)]
    [InlineData(49.5,50.5)]
    public void Every_recipient_needs_positive_whole_amount_and_exact_total(double first,double second)
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};
        Assert.NotNull(CampaignFundingRules.ValidateAllocations(100,new Dictionary<Guid,decimal>{{ids[0],(decimal)first},{ids[1],(decimal)second}},ids));
    }

    [Fact]
    public void Equal_split_distributes_remainder_without_losing_money()
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
        var amounts=CampaignFundingRules.SplitEqual(101,ids);
        Assert.Equal(101m,amounts.Values.Sum());
        Assert.Null(CampaignFundingRules.ValidateAllocations(101,amounts,ids));
    }
}
