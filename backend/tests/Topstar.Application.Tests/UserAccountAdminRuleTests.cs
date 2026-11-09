using Topstar.Application.Profiles;
namespace Topstar.Application.Tests;
public sealed class UserAccountAdminRuleTests
{
 [Fact]public void Lock_RequiresActiveApproved()=>Assert.True(UserAccountAdminRules.CanChange(true,"Active",ProfileStatuses.Approved));
 [Theory][InlineData(ProfileStatuses.Approved)][InlineData(ProfileStatuses.NeedUpdate)][InlineData(ProfileStatuses.Rejected)][InlineData(ProfileStatuses.Locked)]
 public void Unlock_RequiresOnlyLockedAccount(string profileStatus)=>Assert.True(UserAccountAdminRules.CanChange(false,"Locked",profileStatus));
 [Theory][InlineData(true,"Locked","Locked")][InlineData(true,"Active","PendingReview")][InlineData(false,"Active","Approved")]
 public void InvalidPairs_AreRejected(bool locked,string user,string profile)=>Assert.False(UserAccountAdminRules.CanChange(locked,user,profile));
}
