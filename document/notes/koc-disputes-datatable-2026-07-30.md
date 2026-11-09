# KOC disputes DataTable — 2026-07-30

## Quy chuẩn đã áp dụng

- Danh sách tranh chấp KOC tải bằng AJAX và jQuery DataTables offline.
- Phân trang, số dòng, tìm kiếm và lọc trạng thái dùng endpoint `?handler=Data`.
- Tên cột luôn một dòng; dữ liệu dài được rút gọn trong ô và xem đầy đủ ở trang chi tiết.
- Trạng thái và loại tranh chấp luôn hiển thị tiếng Việt, có màu và icon theo ngữ nghĩa.
- Ô chọn chiến dịch/đối tác là combobox có tìm kiếm theo tên chiến dịch hoặc Brand; hỗ trợ bàn phím và giới hạn 50 kết quả mỗi lần hiển thị.
- Không dùng validation mặc định của trình duyệt. Lỗi được hiển thị ngay dưới trường, viền đỏ và icon lỗi theo chuẩn giao diện hệ thống.

## Trạng thái rỗng

- Không hiển thị khung trắng với một dòng chữ đơn lẻ.
- Empty state phải có icon, tiêu đề, hướng dẫn ngắn và nút `Mở tranh chấp`.
- Khi bộ lọc không có kết quả, hiển thị empty state riêng để người dùng biết cần đổi từ khóa hoặc trạng thái.

## Chuẩn trình bày trung tâm hỗ trợ

- Đầu trang dùng hero gradient riêng của KOC, có mô tả ngắn và CTA mở yêu cầu rõ ràng.
- Hiển thị ba chỉ số thiết yếu: tổng yêu cầu, đang xử lý và đã giải quyết.
- Bộ lọc, danh sách và phân trang nằm trong cùng một card; vẫn có khoảng cách thị giác rõ giữa từng vùng.
- Tiêu đề, trạng thái và nút thao tác phải có icon, cỡ chữ dễ đọc và màu mang đúng ý nghĩa nghiệp vụ.
- Không tạo thêm `max-width` riêng bên trong Bootstrap container của layout KOC.

## Trang chi tiết tranh chấp

- KOC dùng giao diện chi tiết riêng; không dùng chung layout thô của Brand/Admin.
- Header phải cho thấy ngay chiến dịch, loại vấn đề, đối tác, trạng thái và mã yêu cầu.
- Nội dung ban đầu, kết luận, trao đổi và bằng chứng được chia thành các vùng rõ ràng.
- Trao đổi hiển thị theo dạng hội thoại; phản hồi của KOC và bộ phận hỗ trợ có màu nhận diện khác nhau.
- Lịch sử trạng thái và dữ liệu tổng quan đặt ở cột phụ để trang ngắn, dễ theo dõi.
- Form phản hồi dùng validation tùy chỉnh dưới trường; không dùng cảnh báo mặc định của trình duyệt.

## Tệp liên quan

- `Pages/Koc/Disputes.cshtml`
- `Pages/Koc/Disputes.cshtml.cs`
- `wwwroot/js/koc/disputes.js`
- `wwwroot/css/koc/koc.css`
