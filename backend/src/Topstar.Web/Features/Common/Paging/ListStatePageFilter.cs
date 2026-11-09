using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Topstar.Web.Features.Common.Paging;

// Preserve only list state when a page posts back and redirects to itself.
public sealed class ListStatePageFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var result = await next();
        if (HttpMethods.IsPost(context.HttpContext.Request.Method) &&
            result.Result is RedirectToPageResult { PageName: null } redirect)
        {
            redirect.RouteValues ??= new RouteValueDictionary();
            foreach (var item in context.HttpContext.Request.Query)
                if (item.Key.StartsWith("list.", StringComparison.Ordinal) && item.Key.Length <= 160 && item.Value.ToString().Length <= 500)
                    redirect.RouteValues.TryAdd(item.Key, item.Value.ToString());
        }
    }
}
