using System.ComponentModel.DataAnnotations;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class PackagePolicyForm
{
    [Range(1,double.MaxValue)]public decimal? ReferenceBudget{get;set;}
    [Required(ErrorMessage = "Vui lòng nhập mã gói.")]
    [RegularExpression("^[A-Za-z0-9_]{2,80}$", ErrorMessage = "Mã chỉ gồm chữ, số và dấu gạch dưới.")]
    public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập tên gói."), StringLength(150)]
    public string Name { get; set; } = string.Empty;
    [Range(1, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0.")] public decimal SalePrice { get; set; }
    [StringLength(500)] public string? ImageUrl { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mô tả gói."), StringLength(1000, MinimumLength = 10, ErrorMessage = "Mô tả phải dài 10–1.000 ký tự.")] public string Description { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập ít nhất một quyền lợi."), StringLength(8000)] public string Benefits { get; set; } = string.Empty;
    [Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 đến 3.650 ngày.")] public int? ValidityDays { get; set; }
    public bool IsFeatured { get; set; }
    [Range(0, 10000)] public int DisplayOrder { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Ngân sách tối thiểu không hợp lệ.")] public decimal MinBudgetAmount { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Ngân sách tối đa không hợp lệ.")] public decimal? MaxBudgetAmount { get; set; }
    public bool BrandSelectionEnabled { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập số KOC được xác nhận tối đa.")]
    [Range(1, 100, ErrorMessage = "Số KOC tối đa từ 1 đến 100.")] public int? SelectionQuota { get; set; }
    public bool IsActive { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập lý do."), StringLength(1000, MinimumLength = 5, ErrorMessage = "Lý do phải dài 5–1.000 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
