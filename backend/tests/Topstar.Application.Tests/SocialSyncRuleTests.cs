using Topstar.Application.SocialSync;
namespace Topstar.Application.Tests;
public sealed class SocialSyncRuleTests
{
 [Theory][InlineData("Pending")][InlineData("Running")]
 public void OpenStatus_IsRecognized(string status)=>Assert.True(SocialSyncRules.IsOpenJob(status));
 [Theory][InlineData("Succeeded")][InlineData("Failed")][InlineData(null)]
 public void TerminalOrMissingStatus_IsNotOpen(string? status)=>Assert.False(SocialSyncRules.IsOpenJob(status));
}
