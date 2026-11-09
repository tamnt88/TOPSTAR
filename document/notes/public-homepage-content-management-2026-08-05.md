# Trang chủ công khai và quản trị nội dung - 05/08/2026

## Phạm vi hoàn thành

- Thay trang chủ hard-code bằng nội dung động, có giao diện responsive riêng tại `wwwroot/css/public/homepage.css` và hành vi riêng tại `wwwroot/js/public/homepage.js`.
- Hoàn thiện các khối: banner chính, CTA, KOC nổi bật, giới thiệu nền tảng, lợi ích, quy trình triển khai, số liệu tin cậy, đối tác và CTA cuối trang.
- Tạo màn hình Admin `Cài đặt hệ thống > Nội dung trang chủ` tại `/Admin/Settings/HomepageContent`.
- Màn quản trị chia tab theo nhóm công việc: Banner & CTA, Nội dung chính, KOC nổi bật, Số liệu & đối tác, Xuất bản & lịch sử.
- Áp dụng validation chuẩn chung: form `novalidate`, viền đỏ, icon lỗi bên phải, thông báo tiếng Việt bên dưới và focus trường lỗi đầu tiên.
- Nội dung được lưu dạng JSON trong cấu hình website; mỗi lần lưu tạo một phiên bản lịch sử kèm lý do và người thao tác.
- Nếu database chưa cập nhật, trang công khai tự dùng nội dung mặc định an toàn; chức năng lưu Admin chỉ hoạt động sau khi chạy SQL.

## Database cần cập nhật

Chạy đúng một file sau trên database TopstarDb:

`database/sql/129_create_homepage_content_management.sql`

File an toàn khi chạy lặp lại. File bổ sung cột `HomepageContentJson`, bảng lịch sử `HomepageContentHistories`, menu Admin và quyền cho vai trò `ADMIN`.

## Kiểm tra

- `dotnet build backend/Topstar.sln --no-restore`: thành công, 0 lỗi, 0 cảnh báo.
- `dotnet test backend/Topstar.sln --no-build --no-restore`: 262/262 test đạt.
- Runtime `http://localhost:5201/`: HTTP 200, các khối hero, quy trình và CTA render thành công.
- `/Admin/Settings/HomepageContent`: người chưa đăng nhập được chuyển đúng về trang đăng nhập.

## Quy tắc vận hành

- Admin phải nhập lý do thay đổi từ 10 ký tự trước khi lưu.
- Tắt `Xuất bản` sẽ khiến trang công khai dùng bộ nội dung mặc định, tránh hiển thị trang trống.
- URL ảnh sử dụng đường dẫn web hợp lệ; ảnh KOC/hero hiện tái sử dụng kho ảnh có sẵn của hệ thống.
- SEO trang chủ tiếp tục quản lý tại màn SEO hiện hữu để không tạo hai nguồn cấu hình trùng nhau.
# Bổ sung 05/08/2026: giữ giao diện cũ và upload ảnh

- Trang công khai tiếp tục sử dụng hệ class `landing-*`, Swiper KOC và hiệu ứng reveal có sẵn của website; CMS chỉ cung cấp dữ liệu, không thay đổi nhận diện và chuyển động gốc.
- Admin cho phép upload trực tiếp ảnh banner, ảnh KOC nổi bật và logo đối tác; có xem trước, kiểm tra JPG/PNG/WEBP và giới hạn 5 MB.
- JavaScript/CSS của upload nằm riêng trong phạm vi Admin; không dùng chung với CSS/JavaScript Brand/KOC.
## Cập nhật quản trị danh sách động và icon (05/08/2026)

- `Lợi ích nổi bật`, `Các bước triển khai`, `KOC/KOL nổi bật` và `Đối tác` cho phép thêm/xóa trực tiếp trong từng tab; hệ thống tự đánh lại thứ tự và giữ tối thiểu một mục.
- Giới hạn lần lượt: 8 lợi ích, 8 bước, 12 KOC/KOL và 24 đối tác.
- Icon lợi ích không còn yêu cầu nhập class Font Awesome: quản trị viên có thể tải JPG/PNG/WEBP tối đa 5 MB và xem trước ngay. Dữ liệu cũ dùng Font Awesome vẫn tiếp tục hiển thị tương thích.
- Ảnh KOC/KOL, logo đối tác và icon của các dòng mới thêm đều dùng upload AJAX; không hiển thị trường URL ảnh.
- Server kiểm tra số lượng, nội dung bắt buộc và ảnh/logo trước khi tạo phiên bản lịch sử mới.
- Không thay đổi database; các danh sách tiếp tục nằm trong payload JSON có version/audit hiện hữu.
