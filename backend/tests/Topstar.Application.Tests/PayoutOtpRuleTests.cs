using Topstar.Application.Finance;

namespace Topstar.Application.Tests;

public sealed class PayoutOtpRuleTests
{
    [Theory]
    [InlineData("000000")]
    [InlineData("123456")]
    [InlineData("999999")]
    public void IsValidCode_AcceptsExactlySixDigits(string code)
        => Assert.True(PayoutOtpRules.IsValidCode(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12A456")]
    [InlineData("１２３４５６")]
    public void IsValidCode_RejectsInvalidInput(string? code)
        => Assert.False(PayoutOtpRules.IsValidCode(code));
}
