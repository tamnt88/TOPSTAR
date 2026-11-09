# Đồng bộ màu màn đăng ký - 05/08/2026

## Phạm vi

- Màn đăng ký dùng chung tại `/dang-ky`, gồm hai tab KOC/KOL và Brand/Doanh nghiệp.
- Không thay đổi nghiệp vụ, validation hoặc luồng đăng ký hiện có.

## Điều chỉnh giao diện

- Tách CSS riêng tại `wwwroot/css/public/register-theme.css` để không ảnh hưởng màn đăng nhập, Admin, Brand và KOC.
- Thay bảng màu vàng/cam cũ bằng tím chủ đạo, lavender nhạt, nền trắng và chữ xanh tím đậm theo giao diện công khai TopStar.
- Đồng bộ màu panel giới thiệu, tab vai trò, nút đăng ký, ô nhập, trạng thái focus và nút đăng ký mạng xã hội.
- Chuẩn hóa font nội dung màn đăng ký về BeVietnamPro; bỏ ảnh hưởng của font tiêu đề trang chủ trên form.
- Giữ nguyên màu đỏ của trạng thái validation theo quy chuẩn chung.

## Kiểm tra

- Build solution thành công: 0 lỗi, 0 cảnh báo.
- `/dang-ky?tab=koc`: HTTP 200, tab KOC/KOL active đúng.
- `/dang-ky?tab=brand`: HTTP 200, tab Brand/Doanh nghiệp active đúng.
- CSS theme riêng: HTTP 200.
- Website đang chạy tại `http://localhost:5201`.

## Database

- Không thay đổi database, không có file SQL cần chạy.
