using Topstar.Application.KocRegistration;

namespace Topstar.Application.Tests;

public sealed class KocCategoryAdminRuleTests
{
    [Fact]
    public void NormalizeCode_ProducesStableSlug()
        => Assert.Equal("lam-dep-skincare", KocCategoryAdminRules.NormalizeCode("  Lam-Dep_Skincare  "));

    [Theory]
    [InlineData("a", "Làm đẹp", 10, "Lý do thay đổi hợp lệ.")]
    [InlineData("lam-dep", "X", 10, "Lý do thay đổi hợp lệ.")]
    [InlineData("lam-dep", "Làm đẹp", -1, "Lý do thay đổi hợp lệ.")]
    [InlineData("lam-dep", "Làm đẹp", 10, "ngắn")]
    public void Validate_RejectsInvalidInput(string code, string name, int order, string reason)
        => Assert.NotNull(KocCategoryAdminRules.Validate(code, name, order, reason));

    [Fact]
    public void Validate_AcceptsValidInput()
        => Assert.Null(KocCategoryAdminRules.Validate("lam-dep", "Làm đẹp", 20, "Cập nhật danh mục nội dung KOC."));

    [Theory]
    [InlineData("")]
    [InlineData("quá ngắn")]
    public void ValidateDeleteReason_RejectsInvalidInput(string reason)
        => Assert.NotNull(KocCategoryAdminRules.ValidateDeleteReason(reason));

    [Fact]
    public void ValidateDeleteReason_AcceptsValidInput()
        => Assert.Null(KocCategoryAdminRules.ValidateDeleteReason("Xóa danh mục tạo để kiểm thử."));
}
