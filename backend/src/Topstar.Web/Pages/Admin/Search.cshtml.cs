using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles="Admin")]
public sealed class SearchModel(TopstarDbContext db):PageModel
{
    public string Query{get;private set;}=string.Empty;
    public IReadOnlyList<SearchResult> Results{get;private set;}=[];
    public sealed record SearchResult(string Type,string Title,string Detail,string Status,string Url,string Icon);

    public async Task OnGetAsync(string? q,CancellationToken ct)
    {
        Query=(q??string.Empty).Trim();
        if(Query.Length<2)return;
        if(Query.Length>100)Query=Query[..100];
        var term=Query;
        var results=new List<SearchResult>();

        results.AddRange(await(from b in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on b.UserId equals u.Id
            where b.CompanyName.Contains(term)||b.ContactName.Contains(term)||u.Email.Contains(term)||b.ContactPhone.Contains(term)
            orderby b.CompanyName select new SearchResult("Brand",b.CompanyName,u.Email+" · "+b.ContactName,b.Status,$"/Admin/Accounts/Brand?query={Uri.EscapeDataString(term)}","fa-regular fa-building")).Take(8).ToListAsync(ct));
        results.AddRange(await(from k in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on k.UserId equals u.Id
            where k.DisplayName.Contains(term)||u.Email.Contains(term)||(u.Phone!=null&&u.Phone.Contains(term))
            orderby k.DisplayName select new SearchResult("KOC",k.DisplayName,u.Email,k.Status,$"/Admin/Accounts/KOC?query={Uri.EscapeDataString(term)}","fa-solid fa-user-star")).Take(8).ToListAsync(ct));
        results.AddRange(await db.Campaigns.AsNoTracking().Where(x=>x.Title.Contains(term))
            .OrderByDescending(x=>x.UpdatedAtUtc??x.CreatedAtUtc).Select(x=>new SearchResult("Chiến dịch",x.Title,x.Id.ToString(),x.Status,$"/Admin/MatchingDetail?id={x.Id}","fa-solid fa-bullhorn")).Take(8).ToListAsync(ct));
        results.AddRange(await db.DisputeCases.AsNoTracking().Where(x=>x.Subject.Contains(term)||x.Description.Contains(term))
            .OrderByDescending(x=>x.UpdatedAtUtc??x.CreatedAtUtc).Select(x=>new SearchResult("Tranh chấp",x.Subject,x.Category,x.Status,$"/Admin/DisputeDetail?id={x.Id}","fa-solid fa-scale-balanced")).Take(8).ToListAsync(ct));
        Results=results.Take(25).ToList();
    }
}
