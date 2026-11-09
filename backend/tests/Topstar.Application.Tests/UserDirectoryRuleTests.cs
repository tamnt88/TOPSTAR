using Topstar.Application.Profiles;

namespace Topstar.Application.Tests;

public sealed class UserDirectoryRuleTests
{
    [Theory]
    [InlineData(null, UserDirectorySorts.Newest)]
    [InlineData("", UserDirectorySorts.Newest)]
    [InlineData("unexpected", UserDirectorySorts.Newest)]
    [InlineData(UserDirectorySorts.Name, UserDirectorySorts.Name)]
    [InlineData(UserDirectorySorts.Score, UserDirectorySorts.Score)]
    public void NormalizeSort_AllowsOnlyCanonicalValues(string? input, string expected)
        => Assert.Equal(expected, UserDirectorySorts.Normalize(input));
}
