using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Web.Services.Payments;

namespace Topstar.Web.Pages.Brand;

[AllowAnonymous]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class VnPayReturnModel(VnPayService service,ILogger<VnPayReturnModel> logger):PageModel
{
    public VnPayResult Result{get;private set;}=new(false,"Chưa có kết quả thanh toán.");
    public async Task OnGetAsync(CancellationToken ct)
    {
        try{Result=await service.ProcessAsync(Request.Query.ToDictionary(x=>x.Key,x=>x.Value),false,ct);}
        catch(Exception ex){logger.LogError(ex,"VNPay browser return could not be read.");Result=new(false,"Chưa kiểm tra được kết quả. Vui lòng xem trạng thái tại trang Tài chính.");}
    }
}
