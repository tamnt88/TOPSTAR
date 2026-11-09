# KOC Dashboard visual refresh — 2026-07-30

## Đã hoàn thiện

- Giữ phạm vi CSS riêng cho KOC, không ảnh hưởng Brand và Admin.
- Đồng bộ dashboard theo bộ màu hồng, coral và peach của header KOC.
- Chuyển chữ trên hero sang trắng/kem để bảo đảm tương phản; bỏ cảm giác chữ đen nặng trên nền gradient.
- Chuẩn hóa màu chữ nội dung sang xanh than mềm và màu phụ sang xanh xám dễ đọc.
- Bổ sung chart **Nhịp công việc** từ dữ liệu thật:
  - chiến dịch đang thực hiện;
  - hồ sơ chờ phản hồi;
  - nội dung cần chỉnh sửa.
- Bổ sung chart **Sức khỏe hồ sơ**:
  - độ hoàn thiện hồ sơ;
  - điểm đánh giá đối tác;
  - tổng follower đã đồng bộ.
- Tăng độ rõ của card, tiêu đề, mô tả và trạng thái; hỗ trợ responsive cho tablet/mobile.
- Tăng version CSS KOC để trình duyệt tải bản giao diện mới.
- Chuyển khối truy cập nhanh từ nền xanh tối cũ sang card sáng; mỗi hành động có icon màu phân nhóm và trạng thái hover rõ ràng.

## Quy chuẩn ghi nhận

- Dashboard không dùng màu đen tuyệt đối trên các vùng màu; ưu tiên trắng trên gradient và xanh than mềm trên nền sáng.
- Chart phải lấy dữ liệu thật từ dashboard contract, không dựng số minh họa.
- Không dùng chung CSS dashboard giữa KOC, Brand và Admin.
- Các chỉ số phải có nhãn và chú thích ngắn để người dùng hiểu ý nghĩa ngay.

## Database / SQL

- Thay đổi này không yêu cầu chạy SQL.
