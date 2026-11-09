namespace Topstar.Application.Academy;

public interface IAcademyLandingService
{
    Task<AcademyLandingContent> GetAsync(CancellationToken cancellationToken = default);
    Task<AcademyLandingSaveResult> SaveAsync(AcademyLandingContent content, string reason, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademyLandingVersionItem>> GetHistoryAsync(CancellationToken cancellationToken = default);
}

public sealed record AcademyLandingContent(
    string Eyebrow, string HeroTitle, string HeroHighlight, string HeroDescription, string HeroImageUrl,
    string PrimaryCtaText, string PrimaryCtaUrl, string SecondaryCtaText, string SecondaryCtaUrl,
    string RoadmapTitle, string RoadmapDescription, string TrustTitle, string TrustDescription,
    string QualityTitle, string OutcomeTitle, string OutcomeDescription, string ResourceTitle,
    string ResourceDescription, string FinalCtaTitle, string FinalCtaDescription, string FinalCtaText,
    string FinalCtaUrl, bool IsPublished, IReadOnlyList<AcademyLandingStat> Stats,
    IReadOnlyList<AcademyLandingRoadmap> Roadmap, IReadOnlyList<AcademyLandingCard> TrustItems,
    IReadOnlyList<AcademyLandingCard> QualityItems, IReadOnlyList<AcademyLandingCard> Outcomes,
    IReadOnlyList<AcademyLandingResource> Resources)
{
    public static AcademyLandingContent Default => new(
        "Học viện dành cho nhà sáng tạo nội dung",
        "Xây nền tảng vững chắc", "Phát triển sự nghiệp KOC",
        "Học theo lộ trình thực hành, hiểu cách làm việc với Brand và xây dựng hồ sơ năng lực đáng tin cậy trên TopStar.",
        "/images/banner/banner-1.webp",
        "Xem chương trình học", "#academy-courses",
        "Bắt đầu với TopStar", "/Public/Register?type=koc",
        "Lộ trình từ người mới đến KOC chuyên nghiệp",
        "Mỗi giai đoạn tập trung vào một nhóm năng lực thiết yếu để bạn tiến bộ có định hướng.",
        "Năng lực được Brand quan tâm",
        "Không chỉ học cách làm nội dung, bạn còn rèn tác phong và khả năng vận hành một chiến dịch thực tế.",
        "Học đúng chuẩn, làm việc minh bạch",
        "Bạn nhận được gì sau lộ trình?",
        "Kiến thức được chuyển hóa thành năng lực có thể trình bày trong hồ sơ và áp dụng khi cộng tác.",
        "Tài nguyên đồng hành",
        "Mẫu brief, checklist và tài liệu thực hành được tổ chức theo từng chặng học.",
        "Sẵn sàng bắt đầu hành trình KOC?",
        "Tạo tài khoản để xây hồ sơ, tham gia khóa học và tiếp cận cơ hội hợp tác phù hợp.",
        "Tạo tài khoản KOC", "/Public/Register?type=koc", true,
        [new("Lộ trình", "Theo năng lực thực tế", 1, true), new("Bài học", "Tập trung vào thực hành", 2, true), new("Hồ sơ", "Ghi nhận quá trình phát triển", 3, true)],
        [new("01", "Hiểu nghề và định vị", "Xác định thế mạnh, nhóm nội dung và hình ảnh cá nhân phù hợp.", 1, true), new("02", "Sản xuất nội dung", "Đọc brief, xây kịch bản, quay dựng và hoàn thiện nội dung đúng mục tiêu.", 2, true), new("03", "Làm việc với Brand", "Nắm deliverable, deadline, quy trình duyệt, nghiệm thu và cách phối hợp chuyên nghiệp.", 3, true), new("04", "Đo lường và phát triển", "Đọc chỉ số, rút kinh nghiệm và nâng chất lượng hồ sơ qua từng chiến dịch.", 4, true)],
        [new("fa-solid fa-lightbulb", "Tư duy nội dung", "Biến mục tiêu truyền thông thành ý tưởng rõ ràng, phù hợp với người xem.", 1, true), new("fa-solid fa-video", "Kỹ năng sản xuất", "Xây kịch bản và hoàn thiện nội dung theo đúng định dạng bàn giao.", 2, true), new("fa-solid fa-chart-line", "Đọc hiểu hiệu quả", "Theo dõi lượt xem, tương tác và chất lượng nội dung bằng dữ liệu.", 3, true), new("fa-solid fa-handshake", "Tác phong hợp tác", "Phản hồi rõ ràng, đúng hẹn và chủ động trong suốt chiến dịch.", 4, true)],
        [new("fa-solid fa-shield-halved", "Quy trình có lưu vết", "Brief, deadline, phiên bản nội dung và phản hồi đều được ghi nhận trên hệ thống.", 1, true), new("fa-solid fa-scale-balanced", "Bảo vệ quyền lợi hai bên", "TopStar hỗ trợ xử lý phát sinh dựa trên dữ liệu và bằng chứng minh bạch.", 2, true)],
        [new("fa-solid fa-id-card", "Hồ sơ năng lực rõ ràng", "Thể hiện nhóm nội dung, kinh nghiệm và lịch sử cộng tác trong một hồ sơ thống nhất.", 1, true), new("fa-solid fa-briefcase", "Sẵn sàng nhận chiến dịch", "Hiểu quy trình từ tuyển chọn, booking, giao nhận đến duyệt nội dung và nghiệm thu.", 2, true)],
        [new("fa-solid fa-book-open", "Bài học theo chủ đề", 1, true), new("fa-solid fa-list-check", "Checklist thực hành", 2, true), new("fa-solid fa-file-lines", "Mẫu brief và kịch bản", 3, true), new("fa-solid fa-circle-play", "Video hướng dẫn", 4, true)]);
}

public sealed record AcademyLandingStat(string Value, string Label, int DisplayOrder, bool IsVisible);
public sealed record AcademyLandingRoadmap(string Stage, string Title, string Description, int DisplayOrder, bool IsVisible);
public sealed record AcademyLandingCard(string Icon, string Title, string Description, int DisplayOrder, bool IsVisible);
public sealed record AcademyLandingResource(string Icon, string Title, int DisplayOrder, bool IsVisible);
public sealed record AcademyLandingVersionItem(Guid Id, string Reason, Guid? ActorUserId, DateTime CreatedAtUtc);
public sealed record AcademyLandingSaveResult(bool Success, string Message);
