using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignFinancialSummaryRuleTests
{
    [Fact]
    public void Actual_cost_includes_package_and_paid_campaign_then_deducts_refund()
        => Assert.Equal(11_900_000m,CampaignFinancialSummaryRules.ActualCost(1_900_000m,12_000_000m,2_000_000m));

    [Fact]
    public void Actual_cost_never_becomes_negative()
        => Assert.Equal(0m,CampaignFinancialSummaryRules.ActualCost(0m,1_000_000m,2_000_000m));

    [Fact]
    public void Escrow_is_budget_less_released_and_approved_refund()
        => Assert.Equal(3_000_000m,CampaignFinancialSummaryRules.EscrowRemaining(10_000_000m,6_000_000m,1_000_000m));

    [Fact]
    public void Escrow_never_becomes_negative()
        => Assert.Equal(0m,CampaignFinancialSummaryRules.EscrowRemaining(10_000_000m,9_000_000m,2_000_000m));
}
