namespace Topstar.Web.Features.Common;

public static class PortalStatusLabels
{
    private static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Draft"] = "Bản nháp",
            ["Published"] = "Đã công bố",
            ["NeedEdit"] = "Cần cập nhật",
            ["Pending"] = "Đang chờ xử lý",
            ["PendingReview"] = "Chờ duyệt",
            ["NeedUpdate"] = "Cần cập nhật",
            ["Approved"] = "Đã duyệt",
            ["Rejected"] = "Đã từ chối",
            ["Active"] = "Đang hoạt động",
            ["Inactive"] = "Ngừng hoạt động",
            ["Applied"] = "Đã ứng tuyển",
            ["Shortlisted"] = "Được chọn sơ bộ",
            ["Confirmed"] = "Đã xác nhận",
            ["NotSelected"] = "Không được chọn",
            ["Withdrawn"] = "Đã rút",
            ["Cancelled"] = "Đã hủy",
            ["CancelledByAdmin"] = "Đã hủy bởi TopStar",
            ["NotCreated"] = "Chưa tạo",
            ["Created"] = "Đã tạo",
            ["InTransit"] = "Đang vận chuyển",
            ["Delivered"] = "Đã giao hàng",
            ["Issue"] = "Có sự cố",
            ["Returned"] = "Đã hoàn hàng",
            ["InProgress"] = "Đang thực hiện",
            ["Completed"] = "Đã hoàn tất",
            ["NotSubmitted"] = "Cần nộp",
            ["Submitted"] = "Chờ duyệt",
            ["NeedRevision"] = "Cần chỉnh sửa",
            ["Proposed"] = "Chờ xác nhận",
            ["Rescheduled"] = "Đề xuất đổi lịch",
            ["Paid"] = "Đã thanh toán",
            ["PaidOut"] = "Đã chi trả",
            ["WithdrawRequested"] = "Chờ rút tiền",
            ["PartiallyReleased"] = "Đã giải ngân một phần",
            ["Released"] = "Đã giải ngân",
            ["CancelRequested"] = "Chờ duyệt hủy",
            ["Refunded"] = "Đã hoàn tiền",
            ["Available"] = "Có thể sử dụng",
            ["OnHold"] = "Tạm giữ",
            ["Consumed"] = "Đã sử dụng",
            ["Expired"] = "Đã hết hạn",
            ["BankTransfer"] = "Chuyển khoản ngân hàng",
            ["OnePay"] = "Thanh toán online",
            ["VnPay"] = "Thanh toán online",
            ["BrandBalance"] = "Số dư",
            ["Dispute"] = "Tranh chấp",
            ["DisputeCase"] = "Hồ sơ tranh chấp",
            ["Processing"] = "Đang xử lý",
            ["Failed"] = "Không thành công",
            ["Locked"] = "Đã khóa",
            ["Overdue"] = "Quá hạn",
            ["Void"] = "Đã hủy",
            ["Open"] = "Mới mở",
            ["InReview"] = "Đang xem xét",
            ["Resolved"] = "Đã giải quyết"
        };

    public static string Of(string? status, string fallback = "Đang cập nhật")
        => string.IsNullOrWhiteSpace(status)
            ? fallback
            : Labels.TryGetValue(status.Trim(), out var label) ? label : status.Trim();
}
