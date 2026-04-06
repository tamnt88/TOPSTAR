# Ghi chú triển khai Admin Menu CRUD (TopStar)

## Mục tiêu
- Quản lý menu Admin/KOC/Brand theo dạng CRUD đầy đủ, không dùng modal.
- Tách tài nguyên giao diện Admin ra file riêng để dễ bảo trì.

## Đã làm
1. Tách CSS/JS riêng cho Admin
- CSS: `backend/src/Topstar.Web/wwwroot/css/admin/admin.css`
- JS: `backend/src/Topstar.Web/wwwroot/js/admin/admin.js`
- Đã include vào layout Admin: `Pages/Admin/_AdminLayout.cshtml`

2. CRUD Menu chuyển sang page riêng
- Danh sách: `Pages/Admin/Settings/MenuManagement.cshtml`
- Create page: `Pages/Admin/Settings/MenuCreate.cshtml`
- Edit page: `Pages/Admin/Settings/MenuEdit.cshtml`
- Delete xử lý tại list bằng submit form + confirm.

3. Quy tắc Mã menu
- Mã menu chuẩn: `MENU_[PHAMVI]_[MA]`
- `PHAMVI`: `ADMIN`, `KOC`, `BRAND`
- Trên form, user chọn dropdown `Phạm vi` + nhập `Mã menu (đuôi)`.
- Hệ thống tự ghép thành mã hoàn chỉnh.

4. Danh sách menu
- Có lọc theo Từ khóa/Phạm vi/Hiển thị.
- Phân trang kiểu số.
- Mặc định 20 item/trang.
- Bỏ text `Số trang` ở phần chọn page size.

5. Spacing vùng nội dung Admin
- Bổ sung khoảng đệm cho `.admin-main-content` để không dính header/footer.

## Đang làm / cần QA
- Verify end-to-end trên môi trường local đang chạy app (hiện file dll bị lock khi build từ CLI).
- Kiểm tra quyền role khi thao tác menu (nếu cần phân quyền chi tiết hơn theo action).
- Cân nhắc tách toàn bộ block CSS admin còn nằm trong `site.css` sang `admin.css` để sạch triệt để.

## Lưu ý kỹ thuật
- Khi app đang chạy trong Visual Studio, lệnh build CLI có thể fail do lock file `Topstar.Web.dll`/`Topstar.Web.exe`.
- Nếu cần build CLI, dừng process `Topstar.Web` trước.

## Cập nhật mới
- Đã refactor theo quy tắc kiến trúc: không truy vấn DB trong .cshtml; toàn bộ dùng service dùng chung (Application/Infrastructure).
- Xem thêm: document/notes/architecture-critical-rules.md
