# Làm mới màu sắc màn đăng ký public - 15/08/2026

- Phạm vi: `/dang-ky?tab=koc` và `/dang-ky?tab=brand`.
- Chỉ thay đổi `wwwroot/css/public/register-theme.css`; không thay đổi nghiệp vụ, Razor, validation, database hoặc CSS của KOC/Brand/Admin.
- Nền trang dùng gradient sáng kết hợp cyan, coral và tím; giữ diện tích trắng lớn để form dễ đọc.
- Panel giới thiệu dùng gradient tím–xanh có quầng sáng coral/cyan; tiêu đề phụ và icon dùng cyan để tăng sức hút.
- Tab đang chọn và nút submit dùng gradient tím nhiều sắc, tăng chiều sâu bằng shadow vừa phải.
- Input, social button và trạng thái focus được làm sáng, rõ nhưng vẫn giữ tương phản và màu validation.

## Xác minh

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- Website chạy tại `http://localhost:5201` bằng bản build mới.
- `/dang-ky?tab=koc`: HTTP 200.
- CSS fingerprint `/css/public/register-theme.567cfyj0tq.css`: HTTP 200, MIME `text/css`.
- Đã kiểm tra trực tiếp bằng ảnh chụp trình duyệt 1680x930.
- Không có SQL cần chạy.
