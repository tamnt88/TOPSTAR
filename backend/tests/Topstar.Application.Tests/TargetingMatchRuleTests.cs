using Topstar.Application.Matching;

namespace Topstar.Application.Tests;

public sealed class TargetingMatchRuleTests
{
    [Fact] public void No_configured_criteria_is_full_match()=>Assert.Equal(100,TargetingMatchRules.Calculate([]).Score);
    [Fact] public void Calculates_equal_weight_score()=>Assert.Equal(66.67m,TargetingMatchRules.Calculate([new("A",true,true),new("B",true,true),new("C",true,false)]).Score);
    [Fact] public void Ignores_unconfigured_criteria(){var result=TargetingMatchRules.Calculate([new("A",false,false),new("B",true,true)]);Assert.Equal(100,result.Score);Assert.Empty(result.Unmatched);}
    [Fact] public void Keeps_reason_groups(){var result=TargetingMatchRules.Calculate([new("Follower",true,false),new("Tỉnh",true,true)]);Assert.Equal(["Follower"],result.Unmatched);Assert.Equal(["Tỉnh"],result.Matched);}
}
