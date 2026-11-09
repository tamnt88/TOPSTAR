# Trung tâm chiến dịch Brand: chuyển toàn bộ tab sang native

## Phạm vi triển khai

- Không còn iframe trong Brand Portal, JavaScript quản lý iframe hoặc CSS cuộn/modal dành cho iframe.
- KOC, tài chính, booking, giao nhận, nội dung, báo cáo, tranh chấp, đánh giá, lịch sử và hồ sơ chiến dịch tải Razor fragment qua AJAX vào DOM trang chính.
- Trang truy cập trực tiếp vẫn dùng layout Brand đầy đủ. Yêu cầu fragment dùng header `X-Topstar-Workspace: native`, không thay đổi kiểm tra đăng nhập, phân quyền hay dịch vụ nghiệp vụ.
- Các fragment chỉ chạy initializer từ section Scripts của trang; JSON dữ liệu được giữ nguyên. Thư viện dùng chung chỉ nạp một lần.
- Form GET/POST và phân trang giữ mã chiến dịch; phản hồi sau POST hiển thị tại tab đang sử dụng. Kiểm tra frontend chạy trước khi gửi request; giữ token chống CSRF và dữ liệu upload.
- Redirect OnePay vẫn dùng điều hướng top-level theo yêu cầu của cổng thanh toán, không nhúng/không gọi AJAX ra ngoài hệ thống.
- Tổng quan, giai đoạn, số KOC và yêu cầu hỗ trợ được làm mới sau POST. Deep link tab/subtab được giữ; hỗ trợ bàn phím và trạng thái loading/error/retry.
- Dọn DataTables, modal, editor và listener khi rời nội dung; không lặp footer, tiêu đề trang hoặc lớp nền modal.
- Sửa ID radio bị trùng trong hồ sơ chiến dịch: mỗi lựa chọn phân bổ và hình thức thực hiện có ID riêng, giữ nguyên tên trường gửi server.
- Bổ sung lọc chiến dịch ở truy vấn tranh chấp, bao gồm danh sách ứng viên và số liệu phân trang.

## Kiểm chứng và an toàn dữ liệu

- Build không lỗi/cảnh báo; 280 unit tests đạt.
- Audit browser đăng nhập Brand thật trên viewport desktop 1440 và mobile 390, bao gồm mọi tab/subtab và quay lại tab KOC.
- Audit tổng thể 38 lượt trang đạt; audit bổ sung trên chiến dịch QA có hồ sơ chỉnh sửa kiểm tra mọi tab, icon và ID bị trùng.
- Kiểm tra GET bộ lọc giữ chiến dịch; POST từ chối với `Guid.Empty` trả lỗi hồ sơ không tồn tại tại tab, không thể khớp hồ sơ thật và không ghi thay đổi nghiệp vụ.
- Kiểm tra modal native: nằm trong màn hình, đúng một backdrop. Không thử thanh toán, xác nhận KOC, nghiệm thu hay tranh chấp trên dữ liệu thật.
- Không xóa hoặc chỉnh sửa dữ liệu đăng ký. Việc render native chỉ thay đổi giao diện/vận chuyển HTML, không thay schema database.

## Còn phải nghiệm thu production

Luồng thành công có ghi dữ liệu (phân bổ, ký quỹ, giao nhận, duyệt, nghiệm thu, hoàn tiền và tranh chấp) cần kiểm chứng bằng bộ dữ liệu QA riêng và sandbox thanh toán. Cần kiểm tra thiết bị iOS/Android thật, cấu hình bảo mật/HTTPS, callback thanh toán, backup và giám sát trước khi public. Chuyển native không đồng nghĩa toàn bộ sản phẩm đã được nghiệm thu production.
