# Bàn giao cấu hình vận hành Admin - 17/08/2026

## Đã triển khai

- Thêm trang `/Admin/Settings/OperationalSetup` và menu `Cài đặt hệ thống > Cấu hình vận hành`.
- Giao diện được chia thành 5 tab gọn: SLA & hạn mức, Vi phạm, Deliverable, Vận chuyển và Lịch sử; mỗi lần chỉ hiển thị một nhóm.
- Form thêm/sửa đã chuẩn hóa validation tiếng Việt. Khối mức độ/điểm phạt chỉ xuất hiện với nhóm Vi phạm; nút quay lại và hủy trở về đúng tab nguồn.
- Bẫy lỗi form dùng đúng chuẩn Admin chung: ô lỗi có viền đỏ và nền cảnh báo nhạt, icon cảnh báo nằm trong control, thông báo có icon phía trước, focus vào lỗi đầu tiên; cả mức độ và điểm phạt vi phạm được kiểm tra ở client lẫn server.
- Các trang thêm/sửa/xóa đặt `AdminActivePath=/Admin/Settings/OperationalSetup`, vì vậy header active đồng thời `Cài đặt hệ thống` và menu con `Cấu hình vận hành`.
- SLA/hạn mức có thể cấu hình: thời gian sửa đánh giá, thời gian đặt booking tối thiểu, thời lượng booking tối đa, số tài khoản payout tối đa, giới hạn/cửa sổ gửi OTP, hạn OTP và số lần nhập sai.
- Ba nhóm danh mục dùng chung: loại vi phạm (kèm mức độ/điểm phạt mặc định), loại deliverable và checkpoint vận chuyển.
- Thêm/sửa danh mục mở trang `/Admin/Settings/OperationalCatalogForm`, xóa mở trang xác nhận `/Admin/Settings/OperationalCatalogDelete/{id}`, không dùng modal.
- Cấu hình đã được nối vào `ScheduleWorkflowService`, `CampaignReviewService`, màn KOC Reviews, `WalletService`, `UserViolationService`, `CampaignService` và `ShipmentWorkflowService`.
- Mọi lần sửa policy/danh mục ghi bảng history và `AuditLogs`; lịch sử policy và danh mục đều xem ngay trên trang cấu hình. Danh mục đã phát sinh dữ liệu không cho xóa; có thể tạm ẩn để bảo toàn lịch sử.

## Database

- File cần chạy (an toàn, chạy lặp lại): `database/sql/135_create_operational_runtime_setup.sql`.
- Đã chạy trên `DESKTOP-L6JINQQ / TopstarDb` ngày 17/08/2026.
- Seed hiện có: 1 policy runtime, 3 loại vi phạm, 4 loại deliverable, 6 checkpoint vận chuyển.
- Đã sửa dữ liệu seed tiếng Việt bằng `sqlcmd -f 65001`; các tên `Bài đăng hình ảnh`, `Video ngắn` hiển thị đúng Unicode.

## Xác minh

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test Topstar.sln --no-build --no-restore`: 271/271 passed.
- Website chạy tại `http://localhost:5201`.
- Kiểm tra trực tiếp bằng phiên Admin trên Chrome: `/Admin/Settings/OperationalSetup` tải HTTP thành công, title `Cấu hình vận hành - TopStar Admin`, menu active đúng, hiển thị đủ 13 dòng danh mục và 13 nút xóa, CSS card có `border-radius: 18px`, không có alert lỗi. `/Admin/Settings/OperationalCatalogForm` mở trang riêng; `/Admin/Violations` có 3 lựa chọn danh mục từ DB.
- Kiểm tra submit rỗng trên form tạo: hiển thị đúng ba lỗi tiếng Việt cho Mã, Tên hiển thị và Lý do; đổi nhóm sang Deliverable ẩn khối vi phạm, URL quay lại là `#deliverable`, không tràn ngang.
- Kiểm tra lại submit rỗng với nhóm Vi phạm: hiển thị đủ 5 lỗi tiếng Việt (Mã, Tên, Mức độ, Điểm phạt, Lý do), border `rgb(196, 63, 85)`, nền `rgb(255, 250, 251)`, icon trong ô và focus đúng `Input.Code`.

## Lưu ý nghiệp vụ

- Các trạng thái state machine vẫn là hằng số trong code và không mở cho Admin sửa.
- Deliverable/checkpoint/vi phạm mới bắt buộc dùng mã danh mục đang hoạt động. Giá trị lịch sử đang tồn tại vẫn được giữ để hiển thị và chỉnh sửa campaign hiện hữu.
