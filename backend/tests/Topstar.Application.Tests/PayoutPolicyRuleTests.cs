using Topstar.Application.Finance;

namespace Topstar.Application.Tests;

public sealed class PayoutPolicyRuleTests
{
    [Theory]
    [InlineData(100000, 0, 0, 0)]
    [InlineData(100000, 10000, 0.02, 12000)]
    [InlineData(125001, 0, 0.015, 1875)]
    public void CalculateFee_RoundsAndCombinesFee(decimal amount, decimal fixedFee, decimal rate, decimal expected)
        => Assert.Equal(expected, PayoutPolicyRules.CalculateFee(amount, fixedFee, rate));

    [Fact]
    public void Validate_AcceptsSupportedPolicy()
        => Assert.Null(PayoutPolicyRules.Validate(100000, 3, 0, 0.01m));

    [Theory]
    [InlineData(0, 3, 0, 0)]
    [InlineData(100000, 0, 0, 0)]
    [InlineData(100000, 21, 0, 0)]
    [InlineData(100000, 3, -1, 0)]
    [InlineData(100000, 3, 0, 0.21)]
    public void Validate_RejectsUnsafePolicy(decimal minimum, int dailyLimit, decimal fixedFee, decimal rate)
        => Assert.NotNull(PayoutPolicyRules.Validate(minimum, dailyLimit, fixedFee, rate));
}
