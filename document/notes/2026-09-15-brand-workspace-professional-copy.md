# Chuẩn hóa câu chữ trung tâm chiến dịch Brand

## Hoàn tất

- “Chốt KOC” → “Xác nhận danh sách KOC”; “KOC chính thức” → “KOC đã xác nhận”.
- Ngân sách dùng “Ngân sách thù lao KOC”; lịch dùng “Lịch làm việc cần xác nhận”.
- Nút hành động tiếp theo sử dụng tên hành động thực tế thay cho “Thực hiện ngay”.
- Chuẩn hóa tài chính: “Đã giải ngân cho KOC”, “Số dư ký quỹ”, “Đã hoàn trả cho doanh nghiệp”.
- Thông báo chưa có KOC hướng dẫn rõ việc tuyển chọn và xác nhận danh sách.
- Dùng “Lịch làm việc”, “hạn hoàn thành” thay cho Booking/deadline trong phần tiến độ. Chuẩn hóa cách viết TopStar trong nội dung tĩnh liên quan.
- Đồng bộ nhãn xác nhận ở tab tuyển chọn và tài chính; không khẳng định chắc chắn sẽ có hồ sơ thay thế khi vẫn cần TopStar xem xét.

## Trạng thái thực hiện

Trạng thái kỹ thuật InProgress không đủ để hiển thị “Đang triển khai”. Cần danh sách KOC đã được xác nhận đầy đủ, trạng thái ký quỹ hợp lệ và không ở giai đoạn chờ ngày bắt đầu. Nếu thiếu danh sách hoặc ký quỹ, hiển thị “Chưa đủ điều kiện triển khai”. Chiến dịch hoàn tất/đã hủy vẫn hiển thị kết quả tương ứng, không bị đổi thành trạng thái chờ.

Đây là sửa quy tắc hiển thị, không cập nhật trạng thái chiến dịch hoặc dữ liệu đăng ký trong database.

## Kiểm chứng

Bổ sung tab KOC: đổi “Cơ chế tuyển chọn” thành “Quy trình tuyển chọn”, nêu rõ TopStar đề xuất và doanh nghiệp xem xét/xác nhận. Trạng thái phân biệt chờ TopStar đề xuất, chờ doanh nghiệp xác nhận và đã xác nhận danh sách. Thông báo trống chỉ giữ tiêu đề và hướng dẫn ngắn, không lặp giải thích hoặc khẳng định đang xử lý khi chưa có dữ liệu. Bổ sung khoảng cách giữa tiêu đề tab và thống kê. Tổng quan không còn mặc định quy trách nhiệm chờ doanh nghiệp khi chưa có hồ sơ đề xuất.

- Build thành công, không cảnh báo/lỗi.
- 287 unit tests đạt, gồm 7 kiểm tra mới về điều kiện triển khai và chiến dịch đã hoàn tất.
- Browser audit được bổ sung kiểm tra nút hành động cụ thể và trạng thái thực hiện không vượt trước bước xác nhận KOC/ký quỹ, trên desktop/mobile.
