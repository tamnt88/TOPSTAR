# Bàn giao conversation TOPSTAR - 15/08/2026

## Mục đích

Tài liệu này là điểm tiếp tục cho conversation mới. Không được coi các hạng mục dưới đây là hoàn thành 100% nếu chưa build và kiểm thử lại trực tiếp tại `http://localhost:5201`.

## Phạm vi hệ thống đã phát triển

- KOC, Brand và Admin đã có các luồng nghiệp vụ chính.
- Brand: hồ sơ, mua gói, tài chính gói, tạo/quản lý chiến dịch, tuyển chọn KOC, workspace chiến dịch, ký quỹ/thanh toán chiến dịch, booking, giao nhận, duyệt nội dung, hỗ trợ, lịch sử và báo cáo.
- Admin: quản lý tài khoản Brand/KOC, hồ sơ chờ duyệt, vi phạm, chiến dịch, chính sách phí, ký quỹ/đối soát/giải ngân, cấu hình thanh toán và quản trị nội dung public.
- Thanh toán hỗ trợ chuyển khoản ngân hàng và thanh toán trực tuyến. Giao diện người dùng chỉ dùng cụm từ “Thanh toán trực tuyến”; OnePAY là tên cổng tích hợp ở cấu hình/quản trị kỹ thuật.
- Mô hình quỹ chiến dịch cho phép chia đều hoặc nhập thù lao riêng từng KOC; phí nền tảng và các ngưỡng nghiệp vụ do Admin cấu hình, snapshot sau khi chốt.

Các chức năng trên vẫn phải được regression test vì worktree có nhiều thay đổi liên tục trong cùng phiên.

## Trạng thái mới nhất cần xử lý trước

### Lỗi ưu tiên cao

Trang public `/danh-cho-brand` đang có lúc chỉ tải được header nhưng nội dung trang mất CSS, hiển thị như HTML thô. Ảnh gần nhất cho thấy hero và dashboard mô phỏng bị mất toàn bộ layout.

Cần kiểm tra theo thứ tự:

1. Razor layout thực tế mà `/danh-cho-brand` sử dụng.
2. Các `@section Styles`, `@section Scripts` và vị trí render section trong layout.
3. Đường dẫn stylesheet phải là đường dẫn tuyệt đối từ web root, không phụ thuộc URL hiện tại.
4. Chỉ giữ một bundle CSS public có thẩm quyền; loại bỏ import trùng, override tạm và style khẩn cấp chồng chéo.
5. Kiểm tra Network/console để xác nhận CSS trả HTTP 200 và đúng MIME type.
6. Build, dừng đúng tiến trình cũ, chạy lại website ở cổng 5201 rồi hard refresh.
7. Kiểm tra đồng thời `/`, `/hoc-vien-koc`, `/danh-cho-brand`, đăng nhập và đăng ký để tránh sửa trang này nhưng làm hỏng trang khác.

### Public website cần regression test

- Header/footer: màu sắc hài hòa, logo nền trong suốt và đủ tương phản, icon đồng nhất, link đúng route.
- Đăng ký: một form duy nhất chia tab KOC/KOL và Brand; form ban đầu chỉ hỏi dữ liệu tối thiểu, hồ sơ chi tiết hoàn thiện sau đăng nhập.
- `/hoc-vien-koc`: hero, khóa học, lộ trình, năng lực, tài nguyên, kết quả và CTA; dữ liệu seed và quản trị Admin phải hoạt động.
- `/danh-cho-brand`: hero, lợi ích, quy trình tuyến tính có progress animation, số liệu, minh bạch, CTA; cần đa dạng gradient tươi nhưng không phủ tím toàn trang.
- Trang chủ: nội dung, ảnh, danh sách động và quản trị Admin; ảnh dùng upload, không hiển thị ô URL kỹ thuật cho người quản trị.
- Các nhóm nội dung động như lợi ích, bước triển khai, KOC/KOL nổi bật và đối tác phải cho thêm, xóa, sắp xếp, bật/tắt và lưu audit.

## Quy chuẩn bắt buộc

### Validation

- Không dùng popup validation mặc định của trình duyệt.
- Trường lỗi có viền đỏ, icon cảnh báo hình tròn có dấu chấm than nằm cân giữa bên phải và dòng thông báo đỏ có icon phía dưới.
- Thông báo phải cụ thể, tiếng Việt chuyên nghiệp và nêu đúng giới hạn, ví dụ “Vui lòng nhập lý do từ 10 đến 500 ký tự.”
- Client và server cùng kiểm tra; lỗi server phải trả về đúng trường trong modal/form, không chuyển sang trang Error.

### Giao diện và thao tác

- Mỗi tab là một nhiệm vụ, không biến tab thành trang cuộn chứa tất cả chức năng.
- Ưu tiên dashboard/workspace một màn hình; thao tác mở modal hoặc cập nhật trong tab hiện tại, hạn chế điều hướng qua nhiều trang.
- Modal phải đặt đúng lớp, khóa scroll nền nhưng không làm bung/mất layout; icon và nút phải cân giữa.
- Trạng thái hiển thị dạng badge/tag tiếng Việt; không lẫn mã trạng thái hoặc tiếng Anh.
- Câu từ hướng người dùng, không dùng thuật ngữ nội bộ như code gói, tên enum hoặc tên cổng kỹ thuật khi không cần thiết.
- Danh sách chuẩn dùng jQuery DataTables/AJAX khi phù hợp: tìm kiếm, lọc, sắp xếp, phân trang; mặc định tối thiểu 10 dòng; pagination đúng tông từng khu vực.
- CSS và JavaScript của KOC, Brand, Admin phải tách riêng. Public cũng phải có bundle riêng, không dựa vào CSS Admin/Brand.
- Mọi thao tác quan trọng cần lưu lịch sử/audit và hiển thị thông báo thành công/thất bại theo chuẩn chung.

## Dữ liệu và SQL

- Không cần chạy SQL mới chỉ để sửa lỗi CSS/public hiện tại.
- Không tự động chạy lại các file SQL quỹ chiến dịch cũ. Chỉ tạo file SQL mới khi schema thực sự thiếu; file phải an toàn, chạy lặp lại được và có file verify riêng.
- Dữ liệu seed chỉ phục vụ kiểm thử, không được làm sai số nghiệp vụ thực tế (ví dụ KOC bị từ chối không được tính vào số đã chốt).

## Quy trình tiếp tục bắt buộc

1. Đọc toàn bộ `document`, đặc biệt `document/notes` và file bàn giao này.
2. Chạy `git status --short`; bảo toàn mọi thay đổi hiện có, không reset hoặc ghi đè thay đổi của người dùng.
3. Xử lý triệt để lỗi mất CSS `/danh-cho-brand` trước khi làm thêm UI.
4. Build dự án và xử lý cả warning/error có liên quan.
5. Chạy website tại `http://localhost:5201`.
6. Kiểm tra request CSS/JS, console và các trang public nêu trên.
7. Sau mỗi phần, cập nhật `document/notes` với file đã đổi, kết quả build/test và SQL cần chạy (nếu có).

## Tiêu chí chỉ được báo hoàn thành

- Không còn trang Error/exception ở luồng đã kiểm tra.
- CSS/JS tải ổn định sau restart và hard refresh.
- Giao diện desktop/responsive không tràn ngang, không dính section, không lệch icon/nút.
- Validation, modal, DataTables/AJAX và audit đúng quy chuẩn.
- Build thành công và website thực sự chạy ở cổng 5201.
- Các chức năng vừa sửa đã được thao tác thử bằng dữ liệu seed, không chỉ kiểm tra bằng mắt.

