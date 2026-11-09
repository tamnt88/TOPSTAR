# Brand & KOC mobile-first — 15/09/2026

## Mục tiêu

Tối ưu các thao tác hằng ngày cho Brand và KOC dùng điện thoại, không thay đổi dữ liệu nghiệp vụ hoặc quyền truy cập.

## Đã hoàn thành

- Brand có bottom navigation cố định gồm `Việc cần làm`, `Chiến dịch`, `Thông báo`, `Tài khoản`.
- Mục Thông báo Brand hiển thị badge chưa đọc từ database.
- `Việc cần làm` dẫn thẳng tới hàng đợi hành động trên Dashboard.
- KOC đổi điểm vào Dashboard thành `Việc cần làm`; giữ các lối tắt `Tìm việc`, `Công việc`, `Lịch`, `Ví` theo trạng thái hồ sơ/phê duyệt.
- Cả hai portal hỗ trợ safe-area đáy màn hình, chừa khoảng nội dung để bottom navigation không che CTA.
- Modal có khoảng an toàn đáy; CTA trong nội dung có vùng chạm tối thiểu 44px.
- Active state của Brand bao phủ cả các màn hình con của chiến dịch.
- Desktop giữ nguyên hệ điều hướng đầy đủ.

## Nghiệm thu

- Build: 0 warning, 0 error.
- Unit test: 280/280 đạt.
- Brand browser audit: 36/36 lượt desktop/mobile đạt.
- KOC browser audit: 24/24 lượt desktop/mobile đạt.
- Brand test xác minh bottom navigation hiển thị đúng 4 mục và không che cuối trang.
- Website chạy tại `http://localhost:5201`.

## Production

- Cần kiểm tra thêm trên thiết bị thật iOS Safari và Android Chrome với bàn phím ảo, chế độ standalone/PWA và cỡ chữ hệ thống lớn.
- Push notification, deep link từ email/SMS và badge nền phụ thuộc cấu hình production.
## Tinh chỉnh giao diện KOC trên điện thoại

- Sửa empty state của danh sách đầu việc: không còn bị ép theo chiều rộng cột STT, ẩn header/phân trang vô nghĩa khi không có dữ liệu và hiển thị trạng thái trống toàn chiều rộng.
- Sắp lại card chiến dịch dưới 576px: tag nằm thành một hàng riêng, hai hành động dùng lưới rõ ràng, nút không xuống chữ và đạt chiều cao chạm 44px.
- Giữ nguyên bố cục nhiều cột trên desktop.

