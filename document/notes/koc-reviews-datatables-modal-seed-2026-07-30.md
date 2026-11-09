# KOC Reviews - DataTables, modal và dữ liệu kiểm thử

Ngày cập nhật: 30/07/2026

## Quy chuẩn đã áp dụng

- Hai tab `Brand đánh giá bạn` và `Bạn đánh giá Brand` dùng cùng một cấu trúc danh sách.
- Dữ liệu tải bằng AJAX và jQuery DataTables bản offline trong `wwwroot/lib/datatables`.
- Phân trang, tìm kiếm, khoảng ngày, số sao và trạng thái được xử lý phía server; không tải toàn bộ dữ liệu rồi lọc ở trình duyệt.
- Mỗi dòng chỉ hiển thị thông tin cần quét nhanh: đối tác, chiến dịch, số sao, thời gian, trạng thái.
- Nội dung dài và toàn bộ thao tác được mở trong modal. Modal không đóng khi nhấp ra vùng ngoài.
- Form trong modal dùng kiểm tra lỗi nội tuyến: viền đỏ, icon lỗi và thông báo ngay dưới trường; không dùng validation popup mặc định của trình duyệt.
- Sau thao tác thành công, DataTable hiện tại được tải lại bằng AJAX và giữ nguyên trang.

## Quy tắc nghiệp vụ được giữ nguyên

- Mỗi bên chỉ được đánh giá một lần cho một chiến dịch đã hoàn tất.
- KOC được chỉnh đánh giá Brand trong 24 giờ nếu đánh giá chưa có phản hồi hoặc khiếu nại.
- Phản hồi đánh giá là dữ liệu append-only.
- Khiếu nại, bổ sung và rút khiếu nại đều đi qua service hiện hữu và tiếp tục lưu lịch sử/audit.
- Danh sách không xóa hay sửa trực tiếp dữ liệu lịch sử.

## Dữ liệu DEV/QA

Chạy theo thứ tự trong SSMS:

1. `database/sql/84_seed_large_dev_activity_dataset.sql` nếu chưa có bộ dữ liệu nền.
2. `database/sql/101_seed_koc_review_tabs_dataset.sql`.
3. `database/sql/102_verify_koc_review_tabs_dataset.sql`.

Nếu chạy bằng `sqlcmd`, bắt buộc thêm `-f 65001` cho mọi file chứa tiếng Việt.
Không chạy thiếu tham số này vì chuỗi Unicode có thể bị ghi thành mojibake.

Nếu bộ seed từng bị chạy sai code page, chạy một lần:

`sqlcmd -S DESKTOP-L6JINQQ -d TopstarDb -U sa -P 123456 -C -b -f 65001 -i database/sql/103_repair_koc_review_seed_unicode.sql`

Tài khoản kiểm thử: `seed.koc.0001@seed.topstar.local`.

Script 101 tạo bộ dữ liệu lặp lại an toàn:

- 30 đánh giá Brand gửi cho KOC.
- 22 đánh giá KOC đã gửi cho Brand.
- 8 chiến dịch đang chờ KOC đánh giá Brand.
- Một phần đánh giá nhận được có phản hồi để kiểm tra bộ lọc trạng thái và modal.
- Có `ReviewStatusHistories`; lịch sử cũ không bị ghi đè.

Không chạy các script seed trên production.

## Tệp triển khai chính

- `Pages/Koc/Reviews.cshtml`
- `Pages/Koc/Reviews.cshtml.cs`
- `Pages/Shared/_ReviewTable.cshtml`
- `Pages/Shared/ReviewTableViewModel.cs`
- `wwwroot/js/koc/reviews.js`
- `wwwroot/css/koc/koc.css`
- `database/sql/101_seed_koc_review_tabs_dataset.sql`
- `database/sql/102_verify_koc_review_tabs_dataset.sql`
- `database/sql/103_repair_koc_review_seed_unicode.sql`
