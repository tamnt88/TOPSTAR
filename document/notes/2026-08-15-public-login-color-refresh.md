# Làm mới màu sắc màn đăng nhập public - 15/08/2026

- Phạm vi: `/dang-nhap`.
- Chỉ thay đổi `wwwroot/css/public/login-theme.css`; không thay đổi Razor, xác thực, validation, database hoặc CSS của KOC/Brand/Admin.
- Giữ navy làm nền tin cậy, bổ sung cyan, tím, coral và vàng ở vai trò điểm nhấn.
- Nền trang chuyển sắc sáng cyan–lavender; panel có gradient navy–xanh–tím và quầng sáng nhẹ.
- Tiêu đề phụ dùng cyan; icon cam kết dùng vàng; CTA đăng nhập dùng gradient navy–tím.
- Input, social button và nút tạo tài khoản có trạng thái focus/hover rõ hơn nhưng vẫn giữ độ tương phản.

## Xác minh

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- Website chạy tại `http://localhost:5201` bằng bản build mới.
- `/dang-nhap`: HTTP 200.
- CSS fingerprint `/css/public/login-theme.6aitximy57.css`: HTTP 200, MIME `text/css`.
- Đã kiểm tra trực tiếp bằng ảnh chụp desktop 1680x930 và mobile 500x900; không tràn ngang.
- Không có SQL cần chạy.
