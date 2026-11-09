# KOC — Trung tâm thông báo (30/07/2026)

## Quy chuẩn đã thống nhất

- Trang thông báo KOC dùng CSS/JS riêng trong `wwwroot/css/koc` và `wwwroot/js/koc`; không dùng chung giao diện Brand/Admin.
- Danh sách có tìm kiếm, phân trang và đổi số dòng bằng jQuery DataTables bản offline.
- Thao tác đọc, đọc tất cả, lưu trữ, khôi phục và mở công việc chạy AJAX; phản hồi qua toast KOC, không dùng `alert()` của trình duyệt.
- Chi tiết thông báo và lịch sử trạng thái hiển thị trong một modal; modal không đóng khi bấm ra vùng ngoài.
- Nội dung dài được rút gọn ở danh sách, chỉ hiển thị đầy đủ trong modal.
- Trạng thái và sự kiện hiển thị tiếng Việt, có icon và màu theo ngữ nghĩa.
- Trạng thái rỗng phải giải thích rõ, không để một khung trắng với một dòng thông báo đơn lẻ.

## Thành phần đã hoàn thiện

- Thống kê: chưa đọc, đang hoạt động, đã lưu trữ.
- Nhóm dữ liệu: tất cả, chưa đọc, đã lưu trữ.
- Tìm kiếm theo tiêu đề/nội dung; chọn 10/20/50 dòng.
- Bảng thông báo, trạng thái, thời gian, thao tác.
- Modal chi tiết và timeline lịch sử.
- AJAX: đánh dấu đã đọc, đọc tất cả, lưu trữ, khôi phục, mở đúng công việc nguồn.
- Trạng thái không có dữ liệu và không tìm thấy kết quả.

## Tệp liên quan

- `backend/src/Topstar.Web/Pages/Koc/Notifications.cshtml`
- `backend/src/Topstar.Web/Pages/Koc/Notifications.cshtml.cs`
- `backend/src/Topstar.Web/wwwroot/js/koc/notifications.js`
- `backend/src/Topstar.Web/wwwroot/css/koc/koc.css`
