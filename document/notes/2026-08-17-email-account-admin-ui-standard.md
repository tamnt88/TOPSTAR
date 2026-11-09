# Chuẩn hóa quản trị tài khoản email Admin - 2026-08-17

## Phạm vi

- Thay trang gộp danh sách và biểu mẫu cũ bằng luồng CRUD tách trang.
- Danh sách có hero, thống kê, bộ lọc, DataTables, trạng thái và thao tác nhất quán với Admin.
- Thêm/sửa dùng trang `EmailAccountForm`; xóa dùng trang xác nhận `EmailAccountDelete`, không dùng modal.
- Giữ active menu `Cài đặt hệ thống > Tài khoản gửi & nhận email` trên toàn bộ luồng.
- Bổ sung dữ liệu SMTP/IMAP không nhạy cảm vào contract để trang sửa không làm mất cấu hình hiện có. Mật khẩu không được trả ngược ra giao diện.
- Validation dùng chuẩn chung của Admin: tiếng Việt, viền và nền đỏ nhạt, icon cảnh báo, focus trường lỗi đầu tiên.
- CSS và JavaScript đặt riêng tại `wwwroot/css/admin/email-accounts.css` và `wwwroot/js/admin/email-accounts.js`.

## Kiểm tra

- `dotnet build backend/Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test backend/tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build --no-restore`: 271/271 test đạt.
- Website chạy tại `http://localhost:5201`.
- Kiểm tra trực tiếp form thêm mới: active menu đúng; 5 thông báo bắt buộc đều bằng tiếng Việt; trường lỗi có class `is-invalid`, viền đỏ, nền đỏ nhạt, icon hiển thị; focus tại `Input.Name`; không tràn ngang.

## Database

- Không thay đổi database và không có SQL cần chạy cho phạm vi này.

## Điều chỉnh header theo phản hồi

- Bỏ hero card lớn ở đầu trang danh sách.
- Dùng chung cấu trúc `account-crud-header` như trang Quản lý Brand: tiêu đề và mô tả bên trái, nút thêm cỡ nhỏ bên phải.
- Kiểm tra trình duyệt xác nhận header không còn nền, viền hoặc padding dạng card; active menu đúng và trang không tràn ngang.
