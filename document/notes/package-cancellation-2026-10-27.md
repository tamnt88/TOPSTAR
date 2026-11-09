# Hủy gói dịch vụ

Triển khai SQL `164_add_package_cancellation.sql` sau 161/163 và trước binaries. Không tự chuyển tiền các đơn hiện hữu.

- Brand: Gói dịch vụ KOC → Gói đang có → Yêu cầu hủy. Gói Paid chưa gắn chiến dịch được chuyển CancelRequested, không còn dùng tạo chiến dịch.
- Admin: Tài chính → Đơn mua gói → Xem & xét duyệt. Hiển thị lý do Brand, nhập kết quả kiểm tra, duyệt hoặc từ chối.
- Duyệt: hoàn toàn bộ AmountSnapshot vào số dư, chuyển Refunded. Không đổi giao dịch thanh toán gốc. Không gọi hoàn ngân hàng qua cổng thanh toán.
- Từ chối: trở về Paid; giữ nguyên ngày hết hạn.
- Ngưỡng duyệt kép theo chính sách số dư: lần một chỉ ghi lịch sử, người thứ hai mới ghi tiền.
- Gói đã gắn chiến dịch: chặn tự hủy, xử lý cùng luồng dừng chiến dịch và đối soát Admin; không tự hoàn phí dịch vụ đã phát sinh.
- Khóa dòng gói, giao dịch Serializable, sổ số dư bất biến và unique index PackageRefund theo SourceId ngăn hoàn trùng. Tạo chiến dịch chỉ nhận gói Paid.
- Thông báo hệ thống trong giao dịch; email dùng template chung sau commit. Lỗi email được ghi log, không đảo ngược khoản hoàn; chưa có hàng đợi tự gửi lại email.

Kiểm tra: 80 integration checks trên LocalDB riêng đã xóa sau chạy; SQL 164 chạy hai lần. Chưa thử SMTP thực tế hoặc giao diện với phiên đăng nhập thật.
