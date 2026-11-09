using Topstar.Application.Billing;
namespace Topstar.Application.Tests;
public sealed class BillingRuleTests
{
 [Theory][InlineData(1000000,150000)][InlineData(1,0)][InlineData(10,2)]public void Calculates_fifteen_percent(decimal budget,decimal expected)=>Assert.Equal(expected,BillingRules.CalculateFee(budget));
 [Fact]public void Supports_paid_from_pending_or_overdue(){Assert.True(BillingRules.CanTransition(BillingStatuses.Pending,BillingStatuses.Paid));Assert.True(BillingRules.CanTransition(BillingStatuses.Overdue,BillingStatuses.Paid));}
 [Fact]public void Does_not_reopen_final_invoice(){Assert.False(BillingRules.CanTransition(BillingStatuses.Paid,BillingStatuses.Pending));Assert.False(BillingRules.CanTransition(BillingStatuses.Void,BillingStatuses.Pending));}
 [Fact]public void Configurable_fee_respects_fixed_minimum_and_maximum(){Assert.Equal(120000m,BillingRules.CalculateFee(1000000m,.1m,20000m,null,null));Assert.Equal(150000m,BillingRules.CalculateFee(1000000m,.1m,0,150000m,null));Assert.Equal(80000m,BillingRules.CalculateFee(1000000m,.1m,0,null,80000m));}
 [Fact]public void Equal_allocation_is_exact_and_deterministic(){var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};var values=CampaignFundingRules.SplitEqual(100m,ids);Assert.Equal(100m,values.Values.Sum());Assert.Equal(34m,values[ids[0]]);Assert.Equal(33m,values[ids[1]]);Assert.Equal(33m,values[ids[2]]);Assert.Null(CampaignFundingRules.ValidateAllocations(100m,values,ids));}
 [Fact]public void Custom_allocation_must_match_budget_and_official_kocs(){var first=Guid.NewGuid();var second=Guid.NewGuid();Assert.NotNull(CampaignFundingRules.ValidateAllocations(100m,new Dictionary<Guid,decimal>{{first,60m},{second,30m}},new[]{first,second}));Assert.NotNull(CampaignFundingRules.ValidateAllocations(100m,new Dictionary<Guid,decimal>{{first,100m}},new[]{first,second}));}
}
