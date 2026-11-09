# Chuẩn hóa giao diện Audit Logs - 17/08/2026

- Đổi tiêu đề tiếng Việt `Nhật ký hệ thống`, bổ sung hero và 4 thẻ thống kê lấy từ dữ liệu thực.
- Toolbar lọc gọn theo chuẩn Admin: tìm kiếm, đối tượng, hành động, khoảng ngày, nút Lọc và icon Đặt lại.
- Giữ nút Xuất CSV riêng; payload nhạy cảm tiếp tục không hiển thị.
- Bảng chuẩn hóa tên cột, thời gian UTC+7, actor/role, badge hành động tiếng Việt và mã kỹ thuật phụ trợ.
- Phân trang server-side dùng nút icon đầu/trước/sau/cuối, không dùng text; giữ nguyên bộ lọc khi chuyển trang.
- CSS tách riêng tại `wwwroot/css/admin/audit-logs.css`.
- Build thành công 0 warning/0 error. Kiểm tra trực tiếp `/Admin/AuditLogs`: menu cha/con active, 4 summary cards, 50 dòng, 7 control lọc, 9 nút phân trang, hai nút đầu/cuối không có text, không tràn ngang.
