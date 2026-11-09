using Topstar.Application;

namespace Topstar.Application.Tests;

public sealed class AccountActivationOtpRuleTests
{
    [Theory]
    [InlineData("000000")]
    [InlineData("123456")]
    public void Otp_AcceptsSixAsciiDigits(string code)
        => Assert.True(AccountActivationRules.IsValidOtp(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12A456")]
    [InlineData("１２３４５６")]
    public void Otp_RejectsInvalidOrUnicodeDigits(string? code)
        => Assert.False(AccountActivationRules.IsValidOtp(code));

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(-1, false)]
    public void ResendWindow_EnforcesChallengeLimit(int recentCount, bool expected)
        => Assert.Equal(expected, AccountActivationRules.CanCreateChallenge(recentCount));
}
