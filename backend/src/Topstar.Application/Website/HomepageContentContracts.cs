namespace Topstar.Application.Website;

public interface IHomepageContentService
{
    Task<HomepageContent> GetAsync(CancellationToken cancellationToken = default);
    Task<HomepageSaveResult> SaveAsync(HomepageContent content, string reason, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomepageVersionItem>> GetHistoryAsync(CancellationToken cancellationToken = default);
}

public sealed record HomepageContent(
    string TrustLabel,
    string HeroTitle,
    string HeroDescription,
    string PrimaryCtaText,
    string PrimaryCtaUrl,
    string SecondaryCtaText,
    string SecondaryCtaUrl,
    string HeroImageUrl,
    string AboutEyebrow,
    string AboutTitle,
    string AboutDescription,
    string ProcessTitle,
    string PartnerTitle,
    string PartnerDescription,
    string MetricsTitle,
    string FinalCtaTitle,
    string FinalCtaDescription,
    string FinalPrimaryCtaText,
    string FinalPrimaryCtaUrl,
    string FinalSecondaryCtaText,
    string FinalSecondaryCtaUrl,
    bool ShowMetrics,
    bool ShowPartners,
    bool ShowFeaturedCreators,
    bool IsPublished,
    IReadOnlyList<HomepageCreatorItem> Creators,
    IReadOnlyList<HomepageFeatureItem> Features,
    IReadOnlyList<HomepageProcessItem> ProcessSteps,
    IReadOnlyList<HomepageMetricItem> Metrics,
    IReadOnlyList<HomepagePartnerItem> Partners)
{
    public static HomepageContent Default => new(
        "Đáng tin cậy bởi 500+ thương hiệu",
        "Kết nối với hàng ngàn KOL trong vài phút",
        "Nền tảng quản trị chiến dịch KOC/KOL toàn diện: chọn gói, tuyển chọn, vận hành, duyệt nội dung và đo lường hiệu quả trong một hệ thống.",
        "Mua gói KOC ngay", "/Public/BrandPublic",
        "Đăng ký miễn phí", "/Public/RegisterKocStep1",
        "/images/icon/1.png",
        "Nền tảng dành cho tăng trưởng",
        "Triển khai chiến dịch nhanh, minh bạch và có thể đo lường",
        "TopStar kết nối Brand với mạng lưới KOC đã xác thực, đồng thời chuẩn hóa toàn bộ quy trình từ brief đến nghiệm thu và đối soát.",
        "Bắt đầu chiến dịch chỉ trong 4 bước",
        "Được tin dùng bởi các thương hiệu hàng đầu",
        "Hệ sinh thái đối tác đa ngành đồng hành cùng TopStar.",
        "Hiệu quả được chứng minh bằng dữ liệu",
        "Sẵn sàng tăng tốc cùng TopStar?",
        "Bắt đầu chiến dịch hoặc xây dựng sự nghiệp KOC chuyên nghiệp ngay hôm nay.",
        "Dành cho Brand", "/Public/BrandPublic",
        "Trở thành KOC", "/Public/RegisterKocStep1",
        true, true, true, true,
        new[]
        {
            new HomepageCreatorItem("Elisa Trần", "Lifestyle", "214K người theo dõi", "/images/koc/trang-nhungdocx-1635528230350.webp", 1, true),
            new HomepageCreatorItem("Ella Suri", "Beauty", "176K người theo dõi", "/images/koc/anp-0693-min-7988.jpg", 2, true),
            new HomepageCreatorItem("Quang Huy", "Business", "76K người theo dõi", "/images/koc/nguoi-mau-nam-viet-nam_461.avif", 3, true),
            new HomepageCreatorItem("Mia Quỳnh", "Fashion", "142K người theo dõi", "/images/koc/nguoi-mau-thoi-trang.webp", 4, true)
        },
        new[]
        {
            new HomepageFeatureItem("fa-solid fa-users", "Mạng lưới KOC chất lượng", "Hồ sơ được xác thực, có dữ liệu hiệu suất và lịch sử hợp tác minh bạch.", 1, true),
            new HomepageFeatureItem("fa-solid fa-chart-line", "Đo lường rõ ràng", "Theo dõi tiến độ, nội dung, tương tác và hiệu quả tài chính trên một dashboard.", 2, true),
            new HomepageFeatureItem("fa-solid fa-shield-halved", "Quy trình an toàn", "Ký quỹ, duyệt nội dung, giao nhận, nghiệm thu và audit được quản lý xuyên suốt.", 3, true)
        },
        new[]
        {
            new HomepageProcessItem("01", "Chọn gói phù hợp", "Chọn mô hình tuyển chọn và quy mô KOC phù hợp ngân sách.", 1, true),
            new HomepageProcessItem("02", "Tạo brief chiến dịch", "Khai báo mục tiêu, nội dung bàn giao và tiêu chí KOC.", 2, true),
            new HomepageProcessItem("03", "Tuyển chọn và vận hành", "Chốt KOC, ký quỹ, booking, giao nhận và duyệt nội dung.", 3, true),
            new HomepageProcessItem("04", "Nghiệm thu và báo cáo", "Đối soát kết quả, giải ngân và theo dõi hiệu quả chiến dịch.", 4, true)
        },
        new[]
        {
            new HomepageMetricItem("500+", "Thương hiệu", 1, true),
            new HomepageMetricItem("10.000+", "KOC/KOL", 2, true),
            new HomepageMetricItem("15.000+", "Chiến dịch", 3, true),
            new HomepageMetricItem("98%", "Khách hàng hài lòng", 4, true)
        },
        new[]
        {
            new HomepagePartnerItem("Shopee", "/images/brand/shopee.png", "#", 1, true),
            new HomepagePartnerItem("Lazada", "/images/brand/lazada.svg", "#", 2, true),
            new HomepagePartnerItem("Grab", "/images/brand/grab.png", "#", 3, true),
            new HomepagePartnerItem("TikTok", "/images/brand/tiktok.svg", "#", 4, true),
            new HomepagePartnerItem("Samsung", "/images/brand/samsung.svg", "#", 5, true),
            new HomepagePartnerItem("FPT", "/images/brand/fpt.png", "#", 6, true)
        });
}

public sealed record HomepageCreatorItem(string Name, string Category, string Followers, string ImageUrl, int DisplayOrder, bool IsVisible);
public sealed record HomepageFeatureItem(string IconClass, string Title, string Description, int DisplayOrder, bool IsVisible);
public sealed record HomepageProcessItem(string StepNumber, string Title, string Description, int DisplayOrder, bool IsVisible);
public sealed record HomepageMetricItem(string Value, string Label, int DisplayOrder, bool IsVisible);
public sealed record HomepagePartnerItem(string Name, string LogoUrl, string Url, int DisplayOrder, bool IsVisible);
public sealed record HomepageVersionItem(Guid Id, string Reason, Guid? ActorUserId, DateTime CreatedAtUtc);
public sealed record HomepageSaveResult(bool Success, string Message);
