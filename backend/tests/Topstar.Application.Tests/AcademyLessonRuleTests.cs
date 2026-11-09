using Topstar.Application.Academy;

namespace Topstar.Application.Tests;

public sealed class AcademyLessonRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://academy.topstar.vn/bai-1")]
    [InlineData("http://localhost/lesson")]
    public void ValidLesson_AcceptsEmptyOrHttpContentUrl(string? url)
        => Assert.True(AcademyCourseRules.IsValidLesson("Bài học đầu tiên", url, 0));

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://files.example.com/lesson")]
    [InlineData("/relative/path")]
    public void ValidLesson_RejectsUnsafeOrRelativeUrl(string url)
        => Assert.False(AcademyCourseRules.IsValidLesson("Bài học đầu tiên", url, 1));

    [Fact]
    public void ValidLesson_RejectsInvalidOrder()
        => Assert.False(AcademyCourseRules.IsValidLesson("Bài học đầu tiên", null, -1));
}
