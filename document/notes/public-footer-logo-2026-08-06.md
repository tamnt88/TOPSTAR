# Cập nhật logo footer 06/08/2026

- Bỏ nền trắng, padding, bo góc và khung khỏi logo; logo trong suốt hiển thị trực tiếp trên nền footer.
- Cân lại chiều rộng logo thành 158px và khoảng cách với khối nhận diện thương hiệu.
- Giữ tỷ lệ ảnh nguyên bản, bổ sung bóng đổ nhẹ không tạo mảng nền.
- Build toàn solution thành công, trang chủ tại `http://localhost:5201` trả HTTP 200 và CSS fingerprint mới đã được phục vụ.
- Không thay đổi database; không có file SQL cần chạy.

## Hiệu chỉnh tương phản

- Logo được chuyển sang sắc trắng khi hiển thị trên nền footer xanh đậm để không bị chìm, vẫn giữ nền trong suốt.
- Icon điều hướng, liên hệ và cam kết được chuẩn hóa về màu vàng nhấn của footer; trạng thái hover dùng nền vàng và chữ xanh đậm.
