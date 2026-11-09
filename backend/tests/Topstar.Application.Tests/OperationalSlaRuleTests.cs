using Topstar.Application.Operations;
using Xunit;

namespace Topstar.Application.Tests;

public sealed class OperationalSlaRuleTests
{
    [Theory]
    [InlineData(0,48,true)]
    [InlineData(24,48,true)]
    [InlineData(-1,48,false)]
    [InlineData(24,24,false)]
    [InlineData(48,24,false)]
    [InlineData(0,2161,false)]
    public void Thresholds_follow_canonical_bounds(int high,int critical,bool expected)
        => Assert.Equal(expected,OperationalSlaRules.IsValidThresholds(high,critical));

    [Fact]
    public void Due_boundary_is_inclusive()
    {
        var origin=new DateTime(2026,7,27,0,0,0,DateTimeKind.Utc);
        Assert.False(OperationalSlaRules.IsDue(origin,origin.AddHours(23).AddMinutes(59),24));
        Assert.True(OperationalSlaRules.IsDue(origin,origin.AddHours(24),24));
    }

    [Fact]
    public void Critical_boundary_is_inclusive()
    {
        var origin=new DateTime(2026,7,27,0,0,0,DateTimeKind.Utc);
        Assert.Equal("High",OperationalSlaRules.Severity(origin,origin.AddHours(47).AddMinutes(59),48));
        Assert.Equal("Critical",OperationalSlaRules.Severity(origin,origin.AddHours(48),48));
    }
}
