# Brand workspace: câu chữ và tích hợp tab

Đã hoàn tất:
- Thay các tiêu đề mang tính kỹ thuật bằng “Chi phí và dòng tiền”, “Tiến độ theo KOC”, “Hỗ trợ chiến dịch”.
- Bỏ thống kê KOC trùng trong workspace; giữ thống kê hồ sơ đề xuất của trang tuyển chọn.
- Ẩn tiêu đề trang độc lập khi nhúng trong tab; giữ nguyên thao tác tạo yêu cầu hỗ trợ. Không ảnh hưởng trang truy cập trực tiếp.
- Bỏ câu tuyển chọn lặp và thông báo khẳng định đang xử lý hồ sơ khi chưa có dữ liệu chứng minh. Phân biệt hồ sơ ứng tuyển và hồ sơ được đề xuất.
- Tiền giữ trong ký quỹ chỉ hiển thị khi có thời điểm thanh toán; không coi ngân sách dự kiến là tiền đã nộp. Hoàn trả hiển thị không có dấu âm trước số 0.
- Build thành công, 280 unit tests đạt. Không chỉnh sửa dữ liệu đăng ký hoặc thực hiện giao dịch thử trên dữ liệu thật.

Giới hạn: các tab hiện vẫn dùng iframe cùng hệ thống, chưa chuyển sang partial/AJAX native. Cần triển khai tiếp nếu yêu cầu trải nghiệm tab hoàn toàn native, đồng bộ trạng thái sau thao tác và điều hướng sâu trong tab. Đây không phải nghiệm thu production toàn bộ.
