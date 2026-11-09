using Topstar.Application.Common;

namespace Topstar.Web.Features.Common.Paging;

public static class ListWindowRequest
{
    public static ListWindow Read(HttpRequest request, string id)
    {
        var prefix = "list." + id + ".";
        int.TryParse(request.Query[prefix + "page"], out var page);
        int.TryParse(request.Query[prefix + "size"], out var size);
        var result = new ListWindow { Page = page, Size = size, Search = request.Query[prefix + "q"], Status = request.Query[prefix + "status"] };
        result.Normalize();
        return result;
    }
}
