# Rà soát cấu hình Admin và hardcode - 17/08/2026

## Nguyên tắc phân loại

- Phải cấu hình trong Admin/database: nội dung, danh mục, mức phí, hạn mức, thời hạn nghiệp vụ, nhà cung cấp tích hợp, phương thức thanh toán và template thông báo.
- Được giữ trong code: mã trạng thái workflow, quyền/role, tên action audit và giới hạn kỹ thuật bảo mật không cho Admin tự nới lỏng.
- Khi thiếu cấu hình bắt buộc, hệ thống phải fail closed với thông báo rõ ràng; không tự dùng một con số nghiệp vụ viết chết trong code.

## Đã lấy từ cấu hình/database

- Website, branding và SEO.
- Tài khoản email, vận chuyển và phương thức thanh toán.
- Chính sách phí chiến dịch, payout và ngưỡng duyệt kép.
- Gói Brand/chính sách matching.
- Menu và phân quyền menu.
- Lĩnh vực nội dung KOC dùng chung cho đăng ký/hồ sơ KOC và targeting Brand.
- Homepage, Academy landing/course và notification template.

## Fallback nghiệp vụ đã loại bỏ

- Payout không còn tự dùng `100.000 ₫`, `3 lần/ngày` khi thiếu bản ghi `PayoutPolicies`; cấu hình Admin là bắt buộc.
- Trang Payout Policy không còn khởi tạo model bằng các con số viết chết.
- Billing không còn rơi về `BillingRules.PlatformFeeRate` hoặc phí cố định bằng 0 khi thiếu Campaign Fee Policy đang hiệu lực.
- Background finance không còn tự dùng 7 ngày giữ tiền khi funding không có snapshot policy; bản ghi thiếu snapshot sẽ không được tự động giải phóng.

## Tình trạng cấu hình trên database kiểm tra

- Website: 1 active.
- Campaign Fee Policy: 1 đang hiệu lực.
- Payout Policy: 1.
- Payment Method: 1.
- Email: 1 active.
- Lĩnh vực KOC: 12 active.
- Package Policy: 6 active.
- Shipping: 0 active; tính năng tích hợp vận chuyển phải được coi là chưa cấu hình.
- Notification Template: 0 active; hiện notification vẫn dùng nội dung gốc từ nghiệp vụ vì chưa có template override.

## Các policy cần tiếp tục đưa ra Admin

1. SLA vận hành còn rải trong code: thời gian sửa review 24 giờ, lịch phải trước 15 phút, booking tối đa 24 giờ.
2. Hạn mức bảo mật/nghiệp vụ payout: tối đa 5 tài khoản nhận tiền, OTP 3 lần/10 phút. Cần tách phần được phép cấu hình khỏi giới hạn an toàn tối đa.
3. Danh mục vi phạm và điểm phạt mặc định: hiện loại vi phạm là ô nhập tự do; cần danh mục Admin có trạng thái, thứ tự và mức phạt gợi ý.
4. Loại nội dung bàn giao và checkpoint vận chuyển: hiện là workflow option trong Razor; nếu nghiệp vụ muốn mở rộng không triển khai code thì cần catalog riêng.
5. Notification Template cần seed/cấu hình đầy đủ theo các `Type + AudienceRole` thực tế; không nên dựa lâu dài vào câu chữ tạo tại từng service.

Các trạng thái như Draft/Published/Paid/Cancelled và role Admin/Brand/KOC không được chuyển thành dữ liệu tùy ý vì chúng là contract của state machine và phân quyền.
