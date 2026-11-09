using Topstar.Application.Reviews;

namespace Topstar.Application.Tests;

public sealed class ReviewReplyRuleTests
{
    [Theory]
    [InlineData("Cảm ơn bạn.")]
    [InlineData("  OK  ")]
    public void IsValidReply_AcceptsTrimmedContent(string body)
        => Assert.True(ReviewRules.IsValidReply(body));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void IsValidReply_RejectsMissingOrTooShortContent(string? body)
        => Assert.False(ReviewRules.IsValidReply(body));

    [Fact]
    public void IsValidReply_RejectsMoreThanTwoThousandCharacters()
        => Assert.False(ReviewRules.IsValidReply(new string('a', 2001)));
}
