using Topstar.Application.Profiles;
namespace Topstar.Application.Tests;
public sealed class UserViolationRuleTests
{
 [Theory][InlineData("Low",true)][InlineData("Medium",true)][InlineData("High",true)][InlineData("Critical",false)]
 public void Severity_IsCanonical(string value,bool expected)=>Assert.Equal(expected,UserViolationRules.IsValidSeverity(value));
 [Theory][InlineData(0,true)][InlineData(100,true)][InlineData(-1,false)][InlineData(101,false)]
 public void Penalty_IsBounded(int value,bool expected)=>Assert.Equal(expected,UserViolationRules.IsValidPenalty(value));
 [Theory][InlineData(80,0,80)][InlineData(80,15,65)][InlineData(20,50,0)]
 public void EffectiveScore_ClampsAtZero(decimal score,int penalty,decimal expected)=>Assert.Equal(expected,UserViolationRules.EffectiveScore(score,penalty));
}
