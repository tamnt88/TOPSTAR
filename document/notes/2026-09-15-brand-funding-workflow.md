# Hoàn thiện chức năng phân bổ thù lao và ký quỹ Brand

## Điều kiện nghiệp vụ

- Chỉ mở phân bổ khi chiến dịch đã được phê duyệt, danh sách KOC đã xác nhận đủ số lượng và ngân sách là số tiền nguyên đồng hợp lệ.
- Trạng thái chưa đủ điều kiện ẩn toàn bộ lựa chọn phân bổ/nút lưu và dẫn về tab KOC.
- Server kiểm tra lại quyền sở hữu, trạng thái chiến dịch, shortlist đã hoàn tất, số KOC, ngân sách và từng khoản thù lao trong transaction Serializable.
- Mỗi KOC phải có đúng một khoản thù lao lớn hơn 0; tổng phải bằng ngân sách. Chặn thiếu/thừa KOC, ID trùng, số âm, số lẻ, ký tự lạ, bảng cũ và ngân sách vừa thay đổi.
- Bảng phân bổ bản nháp luôn đồng bộ với danh sách KOC hiện tại; yêu cầu đã hủy có thể tạo phiên bản phân bổ mới, nhưng không thể thanh toán lại phiên bản đã hủy.
- Chỉ mở bước ký quỹ khi bảng đã lưu khớp hoàn toàn với danh sách KOC và ngân sách hiện tại.
- Trước khi tạo yêu cầu, hiển thị thù lao KOC, phí nền tảng, phí thanh toán và tổng tiền. Brand phải xác nhận lần cuối trong modal.
- Server so sánh tổng tiền Brand vừa xác nhận với phí hiệu lực tại thời điểm gửi; nếu khác thì yêu cầu Brand kiểm tra lại.
- Tạo yêu cầu ký quỹ không được gọi là đã thanh toán; bảng phân bổ bị khóa và Brand tiếp tục thanh toán/đối soát theo phương thức đã chọn.

## Câu chữ và thao tác

- Dùng “Ngân sách thù lao KOC”, “Trạng thái ký quỹ”, “Phân bổ số dư”, “Kiểm tra và xác nhận ký quỹ”.
- Nút lưu bị vô hiệu hóa cho đến khi mọi khoản hợp lệ và tổng phân bổ khớp ngân sách.
- “Chia đều nhanh” giữ đúng chế độ chia đều; “Xóa số tiền” chuyển sang nhập riêng và khóa lưu; phân bổ số dư chỉ hoạt động khi còn số dư hợp lệ.
- Thông báo lỗi sau POST hiển thị ngay trong tab và được cuộn vào vùng nhìn thấy.

## Kiểm chứng

- Build thành công, 313 unit tests đạt.
- Browser audit desktop 1440 và mobile 390 đạt: 2 lượt native workspace, không runtime error.
- Xác nhận server từ chối chiến dịch chưa đủ điều kiện và không hiển thị form chỉnh sửa.
- Kiểm tra giao diện bằng form cô lập, không ghi database: chia đều, nhập riêng, vượt ngân sách, bằng 0, số âm, xóa, phân bổ số dư đều cho kết quả đúng.
- Không tạo yêu cầu thanh toán, ký quỹ hoặc thay đổi dữ liệu đăng ký thật.
