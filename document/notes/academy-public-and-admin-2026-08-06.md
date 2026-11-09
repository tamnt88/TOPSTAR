# Học viện KOC – trang công khai và quản trị (06/08/2026)

## Phạm vi hoàn thiện

- `/hoc-vien-koc` lấy nội dung từ cấu hình quản trị và khóa học công khai từ catalog thật.
- Admin `/Admin/Academy` gồm bốn nhiệm vụ: **Nội dung trang**, **Khóa học và bài học**, **Học viên**, **Lịch sử**.
- Nội dung công khai gồm hero, thống kê, khóa học, lộ trình, năng lực cốt lõi, chuẩn vận hành, kết quả, tài nguyên và CTA.
- Các nhóm nội dung động cho phép thêm/xóa; thay đổi được sắp xếp và lưu lịch sử.
- Banner upload qua AJAX, chấp nhận JPG/PNG/WEBP tối đa 5 MB; không yêu cầu nhập URL ảnh.
- Danh sách học viên dùng DataTables, mặc định 10 dòng, có tìm kiếm, sắp xếp và phân trang.
- Form dùng `novalidate` và bẫy lỗi nội tuyến theo chuẩn chung.
- CSS/JavaScript tách riêng tại `wwwroot/css/public/academy.css`, `wwwroot/css/admin/academy.css`, `wwwroot/js/admin/academy.js`.

## Hiệu chỉnh nội dung và giao diện

- Viết lại nội dung mặc định theo hành trình nghề nghiệp KOC: định vị, sản xuất nội dung, làm việc với Brand và đo lường hiệu quả.
- Thay ảnh icon thử nghiệm bằng banner KOC đúng tỷ lệ. Dữ liệu CMS cũ dùng `/images/icon/2.png` được nhận diện và thay bằng bộ nội dung mới.
- Làm lại hero hai cột, tách dải thống kê và chuẩn hóa khoảng cách, card, responsive; không để ảnh hoặc nội dung tràn ngang.
- Sửa toàn bộ chuỗi tiếng Việt bị lỗi mã hóa trong nội dung mặc định và thông báo dịch vụ.

## Quy tắc nghiệp vụ

- Chỉ khóa học `Published` xuất hiện công khai.
- Không xóa khóa học đã có lịch sử; chuyển sang `Archived`.
- Khi KOC đăng ký, hệ thống chụp snapshot khóa học và bài học.
- Hoàn thành đủ bài học snapshot mới cấp mã chứng nhận có thể xác minh.
- Mỗi lần Admin lưu landing phải nhập lý do 10–1.000 ký tự và tạo lịch sử.

## Database cần chạy

Chạy file an toàn, có thể chạy lặp lại:

`database/sql/130_create_academy_landing_content_management.sql`

## Kiểm tra

- Build solution không lỗi.
- Test nghiệp vụ: 262/262 đạt.
- `GET http://localhost:5201/hoc-vien-koc`: HTTP 200.

## Banner layout hotfix (14/08/2026)

- Fixed a legacy `site.css` selector that forced the Academy hero and statistics strip into the same flex row.
- Page-specific styles are now loaded after the shared public theme so Academy layout rules take precedence.
- Added scoped Academy hero rules for desktop, tablet, and mobile; no global KOC, Brand, or Admin CSS was changed.
- Verification: build succeeded with 0 warnings and 0 errors; `/hoc-vien-koc` returned HTTP 200 and was visually checked at 1900x1000.
- No database change or SQL execution is required for this fix.
# Bổ sung 14/08/2026 - Chuẩn hóa container và nhóm chỉ số đầu trang

- Toàn bộ nội dung `/hoc-vien-koc` dùng Bootstrap `.container` dùng chung của public site; không tự khai báo `width`, `max-width` hoặc khoảng lề riêng cho khung trang.
- Nhóm `Lộ trình / Bài học / Hồ sơ` được trình bày thành ba thẻ chỉ số độc lập, dùng gradient tím, xanh cyan và cam để phù hợp tính chất trẻ trung của hệ thống KOC nhưng vẫn giữ nền sáng, độ tương phản và thứ bậc nội dung rõ ràng.
- Mỗi thẻ có icon, tiêu đề, mô tả và họa tiết phụ; desktop hiển thị ba cột, mobile tự chuyển một cột.
- CSS của trang tiếp tục nằm riêng tại `wwwroot/css/public/academy.css`; không đưa quy tắc Academy vào CSS của Brand, KOC hoặc Admin.
