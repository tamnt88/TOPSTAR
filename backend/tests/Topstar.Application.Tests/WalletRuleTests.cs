using Topstar.Application.Finance;

namespace Topstar.Application.Tests;

public sealed class WalletRuleTests
{
    [Fact]
    public void Earning_amount_must_be_positive()
    {
        Assert.True(WalletRules.IsValidAmount(1));
        Assert.False(WalletRules.IsValidAmount(0));
        Assert.False(WalletRules.IsValidAmount(-1));
        Assert.False(WalletRules.IsValidAmount(1.01m));
        Assert.False(WalletRules.IsValidAmount(WalletRules.MaximumVndAmount + 1));
    }

    [Theory]
    [InlineData(WalletStatuses.Pending, WalletStatuses.Available)]
    [InlineData(WalletStatuses.Pending, WalletStatuses.OnHold)]
    [InlineData(WalletStatuses.OnHold, WalletStatuses.Available)]
    [InlineData(WalletStatuses.Available, WalletStatuses.OnHold)]
    public void Supported_settlement_transitions_are_allowed(string current, string target)
        => Assert.True(WalletRules.CanTransition(current, target));

    [Theory]
    [InlineData(WalletStatuses.Pending, WalletStatuses.PaidOut)]
    [InlineData(WalletStatuses.Available, WalletStatuses.PaidOut)]
    [InlineData(WalletStatuses.Available, WalletStatuses.Pending)]
    public void Payout_and_reverse_transitions_are_not_in_mvp(string current, string target)
        => Assert.False(WalletRules.CanTransition(current, target));

    [Theory]
    [InlineData(PayoutStatuses.WithdrawRequested, PayoutStatuses.PaidOut)]
    [InlineData(PayoutStatuses.WithdrawRequested, PayoutStatuses.Failed)]
    public void Requested_payout_can_reach_a_final_result(string current, string target)
        => Assert.True(PayoutRules.CanProcess(current, target));

    [Theory]
    [InlineData(PayoutStatuses.PaidOut, PayoutStatuses.Failed)]
    [InlineData(PayoutStatuses.Failed, PayoutStatuses.PaidOut)]
    [InlineData(PayoutStatuses.WithdrawRequested, PayoutStatuses.WithdrawRequested)]
    public void Final_payout_result_cannot_be_reversed(string current, string target)
        => Assert.False(PayoutRules.CanProcess(current, target));

    [Fact]
    public void Only_pending_payout_can_be_cancelled_by_koc()
    {
        Assert.True(PayoutRules.CanCancel(PayoutStatuses.WithdrawRequested));
        Assert.False(PayoutRules.CanCancel(PayoutStatuses.PaidOut));
        Assert.False(PayoutRules.CanCancel(PayoutStatuses.Failed));
        Assert.False(PayoutRules.CanCancel(PayoutStatuses.Cancelled));
    }
}
