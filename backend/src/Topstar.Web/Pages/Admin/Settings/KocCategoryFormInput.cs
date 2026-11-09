using System.ComponentModel.DataAnnotations;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class KocCategoryFormInput
{
    [Required(ErrorMessage = "Vui lòng nhập tên lĩnh vực.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên lĩnh vực phải từ 2 đến 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã danh mục.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Mã danh mục phải từ 2 đến 50 ký tự.")]
    [RegularExpression("^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Range(0, 9999, ErrorMessage = "Thứ tự hiển thị phải từ 0 đến 9.999.")]
    public int SortOrder { get; set; }

}
