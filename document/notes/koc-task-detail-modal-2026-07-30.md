# KOC - Danh sách đầu việc và modal chi tiết

Ngày cập nhật: 30/07/2026.

## Quy chuẩn đã chốt

- Danh sách đầu việc tiếp tục tải bằng jQuery DataTables và AJAX phía máy chủ.
- Tên cột phải hiển thị đầy đủ trên desktop; không dùng ellipsis cho tiêu đề cột.
- Cột `Thao tác` có chiều rộng tối thiểu riêng và nút biểu tượng mắt để xem chi tiết.
- Chi tiết đầu việc mở trong một modal tĩnh duy nhất (`backdrop: static`, không đóng bằng phím Escape), không bung child-row làm dài bảng.
- Modal duy nhất chia bốn tab: `Tổng quan`, `Thông tin chiến dịch`, `Nộp nội dung`, `Lịch sử`.
- Tab `Thông tin chiến dịch` lấy dữ liệu thật và hiển thị Brand, nội dung rich text, tổng ngân sách dự kiến, gói, thời gian, yêu cầu booking/gửi sản phẩm cùng đầu việc được giao.
- Nội dung chiến dịch do Brand nhập bằng CKEditor offline, được làm sạch HTML ở server rồi mới render cho KOC; không ép thành text một dòng và không render HTML chưa qua sanitizer.
- `Campaigns.Objective` dùng `NVARCHAR(MAX)`; chạy `database/sql/100_expand_campaign_objective_for_rich_content.sql` trước khi lưu brief dài.
- Chữ trong hero gradient phải có độ tương phản trắng rõ ràng, không để quy tắc heading toàn cục ghi đè thành màu đen.
- Tổng ngân sách phải được giải thích là ngân sách toàn chiến dịch, không mặc định là thù lao của một KOC.
- Các nút tại phần tổng quan chỉ chuyển tab trong cùng modal; không mở modal lồng nhau.
- Tab nộp nội dung tự ẩn khi trạng thái đầu việc không cho phép KOC nộp hoặc thay thế bài.
- `Ghi chú cho Brand` là trường bắt buộc khi nộp hoặc thay thế bài. Lỗi phải hiển thị ngay dưới ô bằng viền đỏ và thông báo tiếng Việt; không dùng validation popup mặc định của trình duyệt.
- Quy tắc bắt buộc ghi chú phải được kiểm tra ở cả JavaScript, Razor Page handler và service để không thể bỏ qua bằng cách gọi request trực tiếp.
- Mỗi lỗi nhập liệu phải nằm ngay dưới đúng trường hoặc nhóm lựa chọn, có biểu tượng cảnh báo và trạng thái `aria-invalid`; không dồn lỗi của link/tệp xuống cuối form.
- Khi có nhiều trường sai, hiển thị đồng thời toàn bộ lỗi trong một lần kiểm tra và focus vào trường sai đầu tiên.
- Validation, lịch sử phiên bản và xử lý dữ liệu nhạy cảm được giữ trong cùng luồng modal.

## Kiểm tra

- `node --check wwwroot/js/koc/tasks.js`: đạt.
- `dotnet build Topstar.Web.csproj --no-restore`: đạt, 0 warning, 0 error.
- Website đã chạy lại tại `http://localhost:5201`.
- Asset `GET /js/koc/tasks.js`: HTTP 200 và chứa logic `taskDetailModal`.
