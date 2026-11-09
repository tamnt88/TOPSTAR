using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class CampaignDetailModel(IProfileAccessService profileAccessService, ICampaignService campaignService, ICampaignApplicationService applicationService, IMatchingService matchingService, TopstarDbContext db, ICampaignParticipationEmailService participationEmail) : KocGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty, StringLength(1000)] public string? PitchNote { get; set; }
    [BindProperty, Range(1000, 1_000_000_000)] public decimal? ProposedAmount { get; set; }
    public CampaignDetail? Campaign { get; private set; }
    public Guid? InvitationId { get; private set; }
    public bool WaitingForOffer{get;private set;}
    public string? ParticipationStatus { get; private set; }
    [TempData] public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false, requireApproval: false); if (redirect is not null) return redirect;
        if (string.IsNullOrWhiteSpace(ReturnUrl) || !Url.IsLocalUrl(ReturnUrl)) ReturnUrl = "/Koc/Jobs";
        Campaign = await campaignService.GetPublishedDetailAsync(Id, UserId(), cancellationToken);
        var invitation = await (from participation in db.Participations.AsNoTracking()
                                join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
                                where participation.CampaignId == Id && koc.UserId == UserId()
                                select new { participation.Id, participation.Status }).FirstOrDefaultAsync(cancellationToken);
        InvitationId = invitation?.Id;
        ParticipationStatus = invitation?.Status;
        if(invitation is not null&&await db.Campaigns.AnyAsync(c=>c.Id==Id&&c.CheckoutAtCreation,cancellationToken))
            WaitingForOffer=!await (from a in db.CampaignFundingAllocations join f in db.CampaignFundings on a.FundingId equals f.Id where a.ParticipationId==invitation.Id&&a.Status=="Active"&&(f.Status=="Paid"||f.Status=="PartiallyReleased") select a.Id).AnyAsync(cancellationToken);
        return Campaign is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostApplyAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await applicationService.ApplyAsync(Id, UserId(), PitchNote, ProposedAmount, cancellationToken);
        Message = result.Message; return RedirectToPage(new { id = Id, returnUrl = ReturnUrl });
    }

    public async Task<IActionResult> OnPostFavoriteAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false, requireApproval: false); if (redirect is not null) return redirect;
        var result = await campaignService.ToggleFavoriteAsync(Id, UserId(), cancellationToken);
        Message = result.Message;
        return RedirectToPage(new { id = Id, returnUrl = ReturnUrl });
    }

    public async Task<IActionResult> OnPostAcceptInvitationAsync(Guid participationId, CancellationToken cancellationToken)
    {
        var result = await matchingService.AcceptInvitationAsync(UserId(), participationId, cancellationToken);
        Message = result.Message;
        if (result.Success)
            await participationEmail.SendResponseAsync(participationId, true, null, cancellationToken);
        return RedirectToPage(new { id = Id, returnUrl = ReturnUrl });
    }

    public async Task<IActionResult> OnPostDeclineInvitationAsync(Guid participationId, string? declineReason, CancellationToken cancellationToken)
    {
        var result = await matchingService.DeclineInvitationAsync(UserId(), participationId, declineReason, cancellationToken);
        Message = result.Message;
        if (result.Success)
            await participationEmail.SendResponseAsync(participationId, false, declineReason, cancellationToken);
        return RedirectToPage(new { id = Id, returnUrl = ReturnUrl });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
