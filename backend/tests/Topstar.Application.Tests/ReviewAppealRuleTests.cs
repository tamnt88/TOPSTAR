using Topstar.Application.Reviews;

namespace Topstar.Application.Tests;

public sealed class ReviewAppealRuleTests
{
    [Fact]
    public void AppealReason_RequiresMeaningfulLength()
    {
        Assert.False(ReviewRules.IsValidAppealReason("quá ngắn"));
        Assert.True(ReviewRules.IsValidAppealReason("Nội dung review không đúng với biên bản nghiệm thu."));
        Assert.False(ReviewRules.IsValidAppealReason(new string('a', 2001)));
    }

    [Theory]
    [InlineData("Open", "Accepted", true)]
    [InlineData("Open", "Rejected", true)]
    [InlineData("Accepted", "Rejected", false)]
    [InlineData("Rejected", "Accepted", false)]
    [InlineData("Open", "Open", false)]
    public void AppealLifecycle_IsTerminalAfterDecision(string current, string target, bool expected)
        => Assert.Equal(expected, ReviewRules.CanResolveAppeal(current, target));
}
