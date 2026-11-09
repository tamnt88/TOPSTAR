using Topstar.Application.Disputes;
namespace Topstar.Application.Tests;
public sealed class DisputeTransitionTests
{
 [Fact]public void Only_open_case_can_be_taken(){Assert.True(DisputeTransitions.CanTake(DisputeStatuses.Open));Assert.False(DisputeTransitions.CanTake(DisputeStatuses.InReview));}
 [Fact]public void Active_case_can_be_decided(){Assert.True(DisputeTransitions.CanResolve(DisputeStatuses.Open));Assert.True(DisputeTransitions.CanResolve(DisputeStatuses.InReview));Assert.False(DisputeTransitions.CanResolve(DisputeStatuses.Resolved));}
 [Fact]public void Creator_can_only_cancel_open_case(){Assert.True(DisputeTransitions.CanCancel(DisputeStatuses.Open));Assert.False(DisputeTransitions.CanCancel(DisputeStatuses.InReview));}
 [Fact]public void Earnings_release_requires_final_decision(){Assert.True(DisputeTransitions.CanReleaseEarnings(DisputeStatuses.Resolved));Assert.True(DisputeTransitions.CanReleaseEarnings(DisputeStatuses.Rejected));Assert.False(DisputeTransitions.CanReleaseEarnings(DisputeStatuses.InReview));}
}
