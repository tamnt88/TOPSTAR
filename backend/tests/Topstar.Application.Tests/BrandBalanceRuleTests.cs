using Topstar.Application.Finance;

namespace Topstar.Application.Tests;

public class BrandBalanceRuleTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1.5)]
    public void Rejects_invalid_currency(decimal amount)
        => Assert.False(BrandBalanceRules.CanSpend(amount, 100000, 100000));

    [Fact]
    public void Spending_cannot_exceed_available_or_invoice_remainder()
    {
        Assert.False(BrandBalanceRules.CanSpend(100001, 100000, 200000));
        Assert.False(BrandBalanceRules.CanSpend(100001, 200000, 100000));
        Assert.True(BrandBalanceRules.CanSpend(100000, 100000, 200000));
    }

    [Fact]
    public void Withdrawal_uses_gross_amount_and_requires_positive_net()
    {
        Assert.False(BrandBalanceRules.CanWithdraw(100001,100000,50000,1000));
        Assert.False(BrandBalanceRules.CanWithdraw(49999,100000,50000,1000));
        Assert.False(BrandBalanceRules.CanWithdraw(50000,100000,50000,50000));
        Assert.True(BrandBalanceRules.CanWithdraw(50000,100000,50000,1000));
    }

    [Fact]
    public void Active_campaign_only_returns_approved_refunds()
        => Assert.Equal(20000,BrandBalanceRules.Refundable(200000,100000,30000,10000,false));

    [Fact]
    public void Cancellation_preserves_allocated_obligations_and_deducts_previous_returns()
    {
        Assert.Equal(120000,BrandBalanceRules.Refundable(200000,100000,30000,10000,true));
        Assert.Equal(0,BrandBalanceRules.Refundable(200000,100000,30000,150000,true));
    }
}
