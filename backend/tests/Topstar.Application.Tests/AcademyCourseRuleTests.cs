using Topstar.Application.Academy;
namespace Topstar.Application.Tests;
public sealed class AcademyCourseRuleTests
{
 [Theory][InlineData("Draft")][InlineData("Published")][InlineData("Archived")]
 public void Status_CanonicalValues_AreValid(string status)=>Assert.True(AcademyCourseRules.IsValidStatus(status));
 [Theory][InlineData("")][InlineData("Deleted")][InlineData("published")]
 public void Status_UnknownValues_AreInvalid(string status)=>Assert.False(AcademyCourseRules.IsValidStatus(status));
 [Fact]public void Course_ValidBoundaries_AreAccepted()=>Assert.True(AcademyCourseRules.IsValid("ABC","Cơ bản",1,"Mô tả đủ dài"));
 [Theory][InlineData(0)][InlineData(105)]
 public void Course_DurationOutsideRange_IsRejected(int weeks)=>Assert.False(AcademyCourseRules.IsValid("Khóa học","Cơ bản",weeks,"Mô tả đủ dài"));
}
