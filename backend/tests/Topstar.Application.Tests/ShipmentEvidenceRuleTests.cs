using Topstar.Application.Shipments;

namespace Topstar.Application.Tests;

public sealed class ShipmentEvidenceRuleTests
{
    [Theory]
    [InlineData("BRAND", true)]
    [InlineData("KOC", true)]
    [InlineData("Admin", false)]
    [InlineData("", false)]
    public void Role_IsCanonical(string role, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.IsAllowedRole(role));

    [Theory]
    [InlineData("/uploads/shipment-evidence/a.jpg", true)]
    [InlineData("/UPLOADS/SHIPMENT-EVIDENCE/a.webp", true)]
    [InlineData("https://example.com/a.jpg", false)]
    [InlineData("/uploads/other/a.jpg", false)]
    public void StoredUrl_IsRestrictedToEvidenceFolder(string url, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.IsValidStoredUrl(url));

    [Theory]
    [InlineData(0, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(-1, false)]
    public void PerRoleLimit_IsEnforced(int count, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.CanAdd(count));

    [Theory]
    [InlineData("Đã nhận đủ sản phẩm.", true)]
    [InlineData("  Đã nhận đủ sản phẩm.  ", true)]
    [InlineData("Quá ngắn", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ReceiptNote_UsesCanonicalLengthRule(string? note, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.IsValidReceiptNote(note));

    [Theory]
    [InlineData("Kiện hàng bị móp một góc.", true)]
    [InlineData("Ngắn", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IssueNote_UsesCanonicalLengthRule(string? note, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.IsValidIssueNote(note));

    [Theory]
    [InlineData("Ảnh kiện hàng còn nguyên vẹn.", true)]
    [InlineData("  Ảnh kiện hàng còn nguyên vẹn.  ", true)]
    [InlineData("Quá ngắn", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EvidenceCaption_IsRequiredAndUsesCanonicalLengthRule(string? caption, bool expected)
        => Assert.Equal(expected, ShipmentEvidenceRules.IsValidEvidenceCaption(caption));
}
