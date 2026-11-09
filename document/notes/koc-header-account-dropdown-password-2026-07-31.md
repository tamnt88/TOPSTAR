# KOC header account dropdown & change password — 2026-07-31

## Đã hoàn thiện

- Gom khu vực tài khoản bên phải header KOC vào một dropdown thống nhất.
- Dropdown gồm:
  - Tóm tắt tài khoản KOC.
  - Mở hồ sơ cá nhân.
  - Đổi mật khẩu.
  - Đăng xuất.
- Đổi mật khẩu hiển thị bằng modal và không đóng khi click ra ngoài hoặc nhấn `Escape`.
- Modal dùng bẫy lỗi nội tuyến, không dùng validation mặc định của trình duyệt.
- Có nút hiện/ẩn riêng cho từng ô mật khẩu.
- Kiểm tra ở cả client và server:
  - Mật khẩu hiện tại bắt buộc và phải chính xác.
  - Mật khẩu mới từ 8–128 ký tự.
  - Bắt buộc có chữ hoa, chữ thường và chữ số.
  - Không được trùng mật khẩu hiện tại.
  - Xác nhận mật khẩu phải trùng khớp.
- Server dùng lại chuẩn PBKDF2 hiện hữu, không xử lý hash tại Razor/JavaScript.
- Ghi `AuthEvent` cho cả lần đổi mật khẩu thành công và thất bại do sai mật khẩu hiện tại.
- Phản hồi thao tác qua toast KOC thống nhất.

## File chính

- `backend/src/Topstar.Web/Pages/Koc/_KocLayout.cshtml`
- `backend/src/Topstar.Web/Pages/Koc/AccountSecurity.cshtml`
- `backend/src/Topstar.Web/Pages/Koc/AccountSecurity.cshtml.cs`
- `backend/src/Topstar.Web/wwwroot/js/koc/koc.js`
- `backend/src/Topstar.Web/wwwroot/css/koc/koc.css`
- `backend/src/Topstar.Application/Auth/AuthContracts.cs`
- `backend/src/Topstar.Application/Auth/AuthService.cs`
- `backend/src/Topstar.Infrastructure/Auth/EfAuthRepository.cs`

## Kiểm tra

- `dotnet build backend/src/Topstar.Web/Topstar.Web.csproj`: thành công, 0 warning, 0 error.
- `node --check backend/src/Topstar.Web/wwwroot/js/koc/koc.js`: thành công.
- CSS KOC trả HTTP 200.
- Website đang chạy tại `http://localhost:5201`.

## Database

- Không cần chạy SQL cho thay đổi này.
- Chức năng dùng bảng người dùng và lịch sử xác thực hiện có.
