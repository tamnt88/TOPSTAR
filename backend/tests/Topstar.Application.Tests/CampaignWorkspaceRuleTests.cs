using Topstar.Application.Billing;
using Topstar.Application.Campaigns;

namespace Topstar.Application.Tests;

public sealed class CampaignWorkspaceRuleTests
{
    private static readonly DateOnly Today = new(2026, 8, 4);

    [Theory]
    [InlineData(false, null, "recruiting", false, "Chưa đủ điều kiện triển khai")]
    [InlineData(false, CampaignFundingStatuses.Paid, "recruiting", false, "Chưa đủ điều kiện triển khai")]
    [InlineData(true, null, "payment", false, "Chưa đủ điều kiện triển khai")]
    [InlineData(true, CampaignFundingStatuses.Pending, "payment", false, "Chưa đủ điều kiện triển khai")]
    [InlineData(true, CampaignFundingStatuses.Paid, "waiting-start", false, "Chờ ngày bắt đầu")]
    [InlineData(true, CampaignFundingStatuses.Paid, "running", true, "Đang triển khai")]
    public void Execution_display_requires_confirmed_shortlist_and_funding(bool finalized, string? funding, string phase, bool canOperate, string label)
    {
        Assert.Equal(canOperate, CampaignWorkspaceRules.CanOperate(CampaignStatuses.InProgress, finalized, funding, phase));
        Assert.Equal(label, CampaignWorkspaceRules.ExecutionStepLabel(CampaignStatuses.InProgress, finalized, funding, phase));
    }

    [Fact]
    public void Completed_campaign_is_displayed_as_complete_but_not_operable()
    {
        Assert.False(CampaignWorkspaceRules.CanOperate(CampaignStatuses.Completed, true, CampaignFundingStatuses.Released, "completed"));
        Assert.Equal("Đã hoàn tất", CampaignWorkspaceRules.ExecutionStepLabel(CampaignStatuses.Completed, true, CampaignFundingStatuses.Released, "completed"));
    }

    [Theory]
    [InlineData(ApplicationStatuses.Applied, true)]
    [InlineData(ApplicationStatuses.Shortlisted, true)]
    [InlineData(ApplicationStatuses.Confirmed, true)]
    [InlineData(ApplicationStatuses.NotSelected, false)]
    [InlineData(ApplicationStatuses.Withdrawn, false)]
    [InlineData(ApplicationStatuses.CancelledByAdmin, false)]
    public void Application_count_only_includes_effective_profiles(string status, bool expected)
        => Assert.Equal(expected, ApplicationStatuses.IsActive(status));

    [Fact]
    public void Funded_campaign_with_missing_shipments_prioritizes_delivery_preparation()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(
            startDate: Today.AddDays(20), requiresShipment: true, shipmentTotal: 0));

        Assert.Equal("preparing", phase.Code);
        Assert.Equal("Shipments", phase.NextPage);
        Assert.Equal("shipments", phase.NextSubPage);
    }

    [Fact]
    public void Fully_prepared_campaign_before_start_date_waits_for_start()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(startDate: Today.AddDays(20)));

        Assert.Equal("waiting-start", phase.Code);
        Assert.Contains("Chờ bắt đầu", phase.Label);
    }

    [Fact]
    public void Submitted_content_after_start_is_the_next_brand_task()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(
            startDate: Today.AddDays(-1), submissionReviewPending: 2));

        Assert.Equal("Duyệt nội dung", phase.NextAction);
        Assert.Equal("deliverables", phase.NextSubPage);
    }

    [Fact]
    public void Not_submitted_content_is_tracked_but_not_presented_as_reviewable()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(
            startDate: Today.AddDays(-1), submissionNotSubmitted: 3));

        Assert.Equal("Theo dõi nội dung", phase.NextAction);
        Assert.Contains("Đang chờ 3 nội dung", phase.Description);
    }

    [Fact]
    public void Completed_and_settled_campaign_links_directly_to_reports()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(status: CampaignStatuses.Completed, fundingStatus: CampaignFundingStatuses.Released));

        Assert.Equal("Reports", phase.NextPage);
        Assert.Equal("reports", phase.NextSubPage);
    }

    [Fact]
    public void Completed_but_unreleased_campaign_stays_in_settlement()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(status: CampaignStatuses.Completed));

        Assert.Equal("settlement", phase.Code);
        Assert.Equal("CampaignFunding", phase.NextPage);
    }

    [Fact]
    public void Open_dispute_has_priority_over_completed_settlement()
    {
        var phase = CampaignWorkspaceRules.Resolve(Input(status: CampaignStatuses.Completed, openDisputes: 1));

        Assert.Equal("warning", phase.Code);
        Assert.Equal("Disputes", phase.NextPage);
    }

    private static CampaignWorkspacePhaseInput Input(
        string status = CampaignStatuses.InProgress,
        DateOnly? startDate = null,
        bool requiresShipment = false,
        int shipmentTotal = 0,
        int submissionNotSubmitted = 0,
        int submissionReviewPending = 0,
        string fundingStatus = CampaignFundingStatuses.Paid,
        int openDisputes = 0) => new(
            status,
            IsShortlistFinalized: true,
            FundingStatus: fundingStatus,
            StartDate: startDate ?? Today,
            Today,
            ConfirmedKocs: 3,
            RequiresBooking: false,
            ScheduleTotal: 0,
            SchedulePending: 0,
            RequiresShipment: requiresShipment,
            ShipmentTotal: shipmentTotal,
            ShipmentIssues: 0,
            SubmissionNotSubmitted: submissionNotSubmitted,
            SubmissionReviewPending: submissionReviewPending,
            SubmissionRevisionPending: 0,
            OpenDisputes: openDisputes);
}
