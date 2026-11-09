using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;
using Topstar.Application.Academy;
using System.Security.Claims;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class AcademyModel(IProfileAccessService profileAccessService,IAcademyService academyService) : KocGuardedPageModel(profileAccessService)
{
    public IReadOnlyList<AcademyCourseItem> Courses{get;private set;}=[];
    public AcademyCourseItem? ContinueCourse => Courses.FirstOrDefault(x => x.Enrollment is { CompletedAtUtc: null });
    public AcademyLessonItem? NextLesson => ContinueCourse?.Lessons.FirstOrDefault(x => !x.IsCompleted);
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false, requireApproval: false);
        if(redirect is not null)return redirect;
        Courses=await academyService.GetForKocAsync(UserId(),cancellationToken);
        return Page();
    }
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }
    public async Task<IActionResult> OnPostEnrollAsync(Guid courseId,CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        return Done(await academyService.EnrollAsync(UserId(),courseId,ct));
    }
    public async Task<IActionResult> OnPostCompleteAsync(Guid lessonId,CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        return Done(await academyService.CompleteLessonAsync(UserId(),lessonId,ct));
    }
    private IActionResult Done(AcademyResult result){Message=result.Message;MessageSuccess=result.Success;return RedirectToPage();}
    private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
