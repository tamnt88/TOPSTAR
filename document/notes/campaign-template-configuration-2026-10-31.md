# Cấu hình gói theo luồng tạo chiến dịch

- Gói chỉ là mẫu nhu cầu: ReferenceBudget, SelectionQuota, BrandSelectionEnabled; public và Brand đọc chung GetPublicAsync.
- Bỏ ngân sách suy đoán cho dữ liệu cũ. Admin nhập ngân sách cụ thể để gói hiển thị; không sao chép giá bán cũ thành ngân sách KOC.
- Form Admin bỏ thời hạn sử dụng, khoảng ngân sách cũ và lý do nhập tay. Sửa gói giữ nguyên các thuộc tính cũ phía server để bảo toàn lịch sử; vẫn ghi audit tự động.
- Quyền Brand tự chọn và số KOC được kiểm tra theo CampaignCreationPolicies; phí và thuế tính tại bước nhu cầu/thanh toán, không nằm trong ngân sách trả KOC.
- Không cần SQL mới ngoài SQL 169. Không tự cập nhật dữ liệu gói hiện có. Admin cần cấu hình ReferenceBudget cho từng gói.
- Menu tài chính chia xử lý/cấu hình; đơn mua gói cũ giữ ở Lịch sử.
- Kiểm tra: Release build và 184 kiểm thử tích hợp trên LocalDB riêng.

## Rà soát nội dung và điều hướng
- Menu Brand đổi Ký quỹ thành Thanh toán. Số dư hướng dẫn dùng cho chiến dịch, không còn hướng dẫn mua gói trước.
- Billing và Packages ghi rõ gói/đơn trước đây. Giữ tra cứu, hủy, hoàn và quyền sử dụng đã mua; không xóa dữ liệu.
- Tiến trình workspace dùng Thanh toán; không nói phải chờ KOC xác nhận mới thanh toán.
- Đường GET/POST PackageCheckout không có PurchaseId đã chuyển đến CampaignNeeds; đường có PurchaseId giữ xử lý đơn cũ.
- Luồng mới sử dụng CampaignNeeds → CampaignEdit → CampaignCheckout, phí/thuế và số tiền được chốt theo giao dịch. Phân bổ thù lao vẫn cần để KOC xác nhận nhận việc.
- Build Release sau thay đổi nội dung: thành công, 0 cảnh báo và 0 lỗi. Không thay đổi schema hay dữ liệu tài chính.

## Form tạo chiến dịch gọn hơn
- Mở sẵn ngày bắt đầu/kết thúc dự kiến, tên, brief và nội dung bàn giao. Mặc định một nội dung, số lượng 1; giữ nút thêm.
- Ảnh, tiêu chí KOC và chi tiết thực hiện nằm trong details Thông tin bổ sung. Lỗi trong phần thu gọn tự mở phần tương ứng.
- Luồng mới giữ AllocationMode ở hidden input, thay đổi bảng phân bổ ở tài chính trước lời mời KOC như hiện tại.
- Thông tin địa điểm/lịch không bắt buộc lúc tạo theo luồng mới; OpenWork kiểm tra đủ trước khi mở.
- Brand/CampaignPreparation bổ sung chi tiết sau duyệt, trước triển khai. Kiểm tra quyền sở hữu, trạng thái, chưa mở công việc trong transaction Serializable; không cập nhật ngân sách, tuyển chọn hay chế độ thực hiện. Ghi audit.
- Kiểm thử 194 checks ở database riêng; Chrome kiểm tra Razor thực tế, thu gọn/mở rộng, hidden needs, không tràn mobile. Không cần SQL mới.
