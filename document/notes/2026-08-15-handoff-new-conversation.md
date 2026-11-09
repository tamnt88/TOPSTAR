# Bàn giao conversation TOPSTAR - 15/08/2026

## Mục đích

Tài liệu này là điểm tiếp tục chính thức khi mở conversation mới. Không được kết luận hệ thống hoàn thành 100% nếu chưa build, chạy website và kiểm tra trực tiếp các luồng liên quan.

## Phạm vi đã phát triển

- Brand: hồ sơ, mua gói, tài chính gói, chiến dịch, shortlist/tuyển chọn KOC, workspace chiến dịch, phân bổ thù lao, ký quỹ/thanh toán chiến dịch, booking, giao nhận, duyệt nội dung, hỗ trợ, lịch sử và báo cáo.
- Admin: quản lý Brand/KOC, hồ sơ chờ duyệt, vi phạm, chiến dịch, chính sách phí, ký quỹ/đối soát/giải ngân, phương thức thanh toán và quản trị nội dung public.
- Thanh toán hỗ trợ chuyển khoản và thanh toán trực tuyến. Giao diện người dùng dùng cụm từ "Thanh toán trực tuyến"; OnePAY chỉ xuất hiện trong cấu hình/kỹ thuật quản trị.
- Mô hình quỹ chiến dịch hỗ trợ chia đều hoặc nhập thù lao riêng từng KOC; phí nền tảng do Admin cấu hình và dữ liệu đã chốt phải lưu snapshot/audit.
- Public đã được mở rộng cho trang chủ, `/hoc-vien-koc`, `/danh-cho-brand`, đăng nhập/đăng ký và nội dung quản trị tương ứng.

## Lỗi ưu tiên cao nhất đang mở

`/danh-cho-brand` có lúc chỉ header còn CSS, phần nội dung body rơi về HTML thô. Đây là lỗi tải/ghép CSS hoặc layout, chưa được xem là hoàn tất.

Cần kiểm tra theo thứ tự:

1. Layout Razor thực tế của trang và việc render `@section Styles`/`@section Scripts`.
2. Đường dẫn CSS phải là URL từ web root, không phụ thuộc đường dẫn tương đối theo route.
3. Chỉ giữ một bundle CSS public có thẩm quyền; loại bỏ import/override vá tạm hoặc trùng lặp.
4. Network phải trả CSS HTTP 200 và đúng MIME; Console không có lỗi tải asset.
5. Dừng đúng tiến trình cũ, build, chạy lại đúng website ở `http://localhost:5201`, rồi hard refresh.
6. Kiểm tra đồng thời `/`, `/hoc-vien-koc`, `/danh-cho-brand`, đăng nhập và đăng ký để tránh sửa trang này làm hỏng trang khác.

## Chuẩn giao diện và nghiệp vụ bắt buộc

- Validation không dùng popup native của trình duyệt. Trường lỗi có viền đỏ, icon cảnh báo tròn ở mép phải và dòng lỗi đỏ có icon bên dưới; kiểm tra cả client lẫn server và trả lỗi đúng trường.
- Không để exception rơi ra trang Error; lỗi nghiệp vụ phải được bẫy và thông báo chuyên nghiệp bằng tiếng Việt.
- Trạng thái phải là tag dễ nhận biết, dùng tiếng Việt; không để mã nội bộ hoặc trạng thái tiếng Anh lộ ra UI.
- Danh sách nhiều dữ liệu dùng AJAX/jQuery DataTables theo khuôn mẫu: tìm kiếm, lọc, sắp xếp, phân trang; mặc định tối thiểu 10 dòng; pagination đúng màu từng khu vực.
- Tabs là các nhiệm vụ riêng, không biến thành một trang cuộn dài. Ưu tiên thao tác ngay trong workspace/modal thay vì điều hướng qua nhiều trang.
- Modal phải đúng lớp phủ, khóa cuộn đúng và không làm bung/mất dữ liệu nền.
- Nút có icon cân giữa; câu chữ ngắn, chuyên nghiệp và nhất quán.
- CSS/JavaScript của KOC, Brand, Admin và Public phải tách riêng; không để CSS khu vực này ghi đè khu vực khác.
- Mọi thay đổi quan trọng phải lưu audit/lịch sử và dùng thông báo chuẩn của hệ thống.

## Chuẩn public cần giữ

- Không phủ tím toàn trang; dùng nền sáng/trắng làm chủ đạo, tím là màu thương hiệu, phối thêm gradient tươi nhưng hài hòa.
- Header/footer gọn, logo nền trong suốt, đủ tương phản; menu active đơn giản; mọi link đi đúng route.
- Chỉ có một form đăng ký, chia tab KOC/Brand; form ban đầu tối giản, hồ sơ chi tiết hoàn thiện sau đăng nhập.
- `/hoc-vien-koc`: nội dung đào tạo, khóa học seed và quản trị Admin; tiêu đề không quá to/đậm, bố cục màu sắc đa dạng.
- `/danh-cho-brand`: hero, lợi ích, quy trình tuyến tính có progress animation, số liệu, minh bạch, CTA; dùng container chuẩn và hiệu ứng vừa phải.
- Nội dung động như lợi ích, bước triển khai, KOC/KOL nổi bật, đối tác phải cho Admin thêm/xóa/sắp xếp/bật tắt; hình ảnh dùng upload đẹp, không lộ ô URL thô.

## Database và seed

- Lỗi CSS hiện tại không cần SQL.
- Không chạy lại tùy tiện các SQL campaign funding cũ.
- Nếu cần thay schema, tạo file SQL idempotent riêng kèm verify và ghi rõ chính xác file cần chạy.
- Seed phải phục vụ kiểm thử nhưng không làm sai số đếm nghiệp vụ (KOC bị từ chối không được tính vào danh sách hiệu lực, số KOC phải khớp yêu cầu chiến dịch).

## Quy trình tiếp tục bắt buộc

1. Đọc toàn bộ `document`, đặc biệt `document/notes` và tài liệu mô hình trong `document/flow`.
2. Kiểm tra `git status`; không ghi đè/xóa thay đổi hiện hữu.
3. Sửa dứt điểm lỗi CSS `/danh-cho-brand` trước khi mở rộng nội dung khác.
4. Build solution.
5. Chạy website tại `http://localhost:5201`.
6. Kiểm tra trực tiếp asset, Console, Network và các trang public liên quan.
7. Cập nhật `document/notes` sau mỗi phần.

## Điều kiện được báo hoàn thành

- Build thành công.
- Website chạy đúng cổng 5201.
- Không còn lỗi console/network/exception ở luồng vừa sửa.
- Giao diện responsive, không tràn ngang hoặc mất CSS.
- Validation, modal, DataTables/AJAX, thông báo và audit đúng chuẩn.
- Đã cập nhật note và nêu rõ SQL cần chạy nếu có.
