# Hoàn thiện footer khu vực công khai - 05/08/2026

## Phạm vi

- Thay footer mẫu tiếng Anh và các liên kết giả bằng footer tiếng Việt có phân cấp rõ ràng.
- Logo, tên website, hotline, email, tên miền, địa chỉ và bản quyền được đọc từ cấu hình website trong Admin.
- Hotline, email và website là liên kết thao tác trực tiếp; các liên kết điều hướng trỏ đúng trang đang có trong hệ thống.
- Bổ sung CTA dành cho Brand và KOC.
- Bố cục responsive cho desktop, tablet và mobile.

## Tách biệt giao diện

- Razor: `backend/src/Topstar.Web/Pages/Shared/_Footer.cshtml`.
- CSS riêng: `backend/src/Topstar.Web/wwwroot/css/public/footer.css`.
- Không dùng chung hoặc thay đổi CSS của KOC, Brand và Admin.

## Kiểm tra

- Build toàn solution thành công: 0 cảnh báo, 0 lỗi.
- Website đã khởi động lại tại `http://localhost:5201`.
- Trang chủ trả về HTTP 200 và đã render footer mới.
- Không thay đổi database; không có file SQL cần chạy.

## Hiệu chỉnh sau kiểm tra giao diện

- Khóa font footer về `BeVietnamPro` để không kế thừa font tiêu đề trang chủ.
- Thay icon đăng ký KOC bằng icon `fa-users` có trong bộ Font Awesome nội bộ.
- Đã đối chiếu các liên kết với route Razor thực tế: `/hoc-vien-koc`, `/danh-cho-brand`, `/goi-koc`, `/dang-ky`, `/dang-ky-brand`, `/dang-ky-koc/buoc-1` và `/dang-nhap`.

## Chuẩn hóa luồng đăng ký

- Footer chỉ dùng một form đăng ký tại `/dang-ky`.
- Liên kết KOC/KOL mở `/dang-ky?tab=koc`; liên kết Brand mở `/dang-ky?tab=brand`.
- Loại bỏ các liên kết footer tới form đăng ký vai trò riêng/luồng cũ.
- Logo và liên kết Trang chủ sử dụng URL gốc `/`; các liên kết công khai dùng URL tuyệt đối từ gốc để không bị sai khi mở từ trang con.
