using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Web.Services.Payments;

namespace Topstar.Web.Pages.Brand;

public sealed class OnePayReturnModel(IOnePayService onePay) : PageModel
{
    public OnePayResult Result { get; private set; } = new(false, "Không nhận được kết quả thanh toán.");
    public async Task OnGetAsync(CancellationToken ct) => Result = await onePay.ProcessResponseAsync(Request.Query.ToDictionary(x => x.Key, x => x.Value), ct);
}
