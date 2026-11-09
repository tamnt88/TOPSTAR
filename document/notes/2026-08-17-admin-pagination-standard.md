# Chuẩn hóa phân trang toàn Admin - 17/08/2026

## Thay đổi

- Áp dụng style phân trang dùng chung trong `wwwroot/css/admin/admin.css` cho DataTables và pagination render phía server.
- Các nút được nối liền, viền xám nhạt, trang hiện tại dùng màu tím Admin và chỉ bo góc ở hai đầu thanh.
- Bốn nút điều hướng dùng icon: trang đầu, trang trước, trang sau và trang cuối; không còn text `Đầu`/`Cuối`.
- Icon vẫn có `aria-label` và `title` tiếng Việt để hỗ trợ accessibility và tooltip.
- `admin.js` lắng nghe `init.dt`, `draw.dt`, `page.dt` và DOM động nên áp dụng được cho DataTables khởi tạo sau layout hoặc render lại khi chuyển trang/lọc dữ liệu.
- Layout Admin dùng `asp-append-version` cho `admin.css` và `admin.js` để trình duyệt nhận bản mới, tránh cache asset cũ.

## Xác minh

- Build solution: thành công, 0 warning, 0 error.
- `node --check wwwroot/js/admin/admin.js`: đạt.
- Test Application: 271/271 đạt.
- Kiểm tra trực tiếp bằng Chrome sau khi DataTables render tại `/Admin/Settings/KocCategories`:
  - Bốn nút điều hướng không còn text.
  - Icon Font Awesome hiển thị đúng.
  - `aria-label`/`title`: `Trang đầu`, `Trang trước`, `Trang sau`, `Trang cuối`.
  - Kích thước nút điều hướng: 36 x 38 px; trang active có nền tím `rgb(111, 54, 200)`.

Không thay đổi database và không cần chạy SQL.
