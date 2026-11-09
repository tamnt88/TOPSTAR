# Số dư Brand

Chạy `database/sql/161_add_brand_balance.sql`, sau đó `database/sql/162_add_campaign_pause_status.sql` trước khi triển khai binaries. Các script 29, 36, 147, 159, 160 là các phụ thuộc đã có của hệ thống. Không tự chạy SQL từ ứng dụng. Script không chuyển số tiền ký quỹ cũ thành số dư và không tự xác nhận giao dịch.

## Luồng

- Admin vào `/Admin/Finance/BrandBalances`, đối soát khoản hoàn đã duyệt. Chỉ hoàn phần nghĩa vụ đã được duyệt hoàn; tiền chưa phân bổ chỉ được hoàn khi chiến dịch đã hủy. Tranh chấp chưa giải quyết chặn thao tác.
- Khoản hoàn vào số dư và khoản hoàn ngân hàng dùng chung tổng `RefundedAmount` của quỹ, cùng khóa giao dịch, tránh hoàn hai lần.
- Brand vào `/Brand/Balance` xem khả dụng, tiền đang giữ, lịch sử; có thể yêu cầu nhận về ngân hàng đã lưu trong hồ sơ.
- Tạo yêu cầu rút giữ ngay toàn bộ số tiền yêu cầu. Phí trừ từ tiền nhận, không trừ thêm ngoài số đã giữ. Hủy/từ chối trả lại tiền khả dụng; xác nhận đã chuyển yêu cầu chứng từ và mã giao dịch.
- Chính sách Admin cho phép bật/tắt nhận yêu cầu rút, mức tối thiểu, phí, thời gian dự kiến, ngưỡng hai quản trị viên duyệt. Yêu cầu cũ giữ nguyên phí và yêu cầu duyệt.
- Với ký quỹ đang chờ thanh toán, Brand có thể dùng một phần hoặc toàn bộ số dư trước khi mở thanh toán online/gửi chứng từ. Phần thiếu thanh toán theo phương thức đã chọn. Bắt đầu thanh toán online khóa số tiền cần trả để giữ nguyên chữ ký và số tiền đối soát.
- Hủy yêu cầu ký quỹ chưa bắt đầu thanh toán trả lại đúng phần số dư đã sử dụng. Nếu đã mở thanh toán online/gửi chứng từ hoặc có đối soát thì chặn tự hủy, cần hỗ trợ xác minh tiền bên ngoài trước.
- Việc thanh toán đủ không tự mở công việc cho KOC. Brand vẫn chủ động mở theo luồng chiến dịch.

Sổ số dư không cho sửa/xóa; OperationKey/RequestKey duy nhất; số dư bị khóa trong transaction khi chi, giữ hoặc trả tiền. Các biểu mẫu không quyết định số dư thật hay số tiền tối đa; máy chủ đọc và kiểm tra lại.

## Giới hạn và kiểm tra triển khai

Brand vào trung tâm chiến dịch, chọn “Tạm ngừng / hủy chiến dịch”. Chỉ tự tạm ngừng/hủy trước khi mở công việc và chưa phát sinh lịch/giao nhận/nội dung/thu nhập/tranh chấp. Tạm ngừng giữ nguyên xác nhận KOC và tiền; tiếp tục khôi phục trạng thái trước đó, không tự mở việc. Lời mời không thể chấp nhận khi chiến dịch tạm ngừng/hủy. Hủy đóng lời mời và hồ sơ ứng tuyển, giữ tiền đã nhận để Admin chốt nghĩa vụ KOC ở phần nghiệm thu. Phần chưa phân bổ được đối soát hoàn; phần đã phân bổ cần Admin quyết định mức KOC được hưởng và phần Brand được hoàn. Tiền gói dịch vụ, phí nền tảng và phí thanh toán không tự hoàn. Chưa có số dư khuyến mại.

Thông báo trong hệ thống được lưu cùng transaction thay đổi trạng thái, gửi Brand/KOC/Admin. Email Brand/KOC sử dụng template chung sau khi transaction thành công; lỗi SMTP được ghi log, không đảo trạng thái và không báo giao dịch tài chính thất bại. Cần kiểm tra cấu hình SMTP thực tế khi triển khai.

Đã qua 356 unit tests và 55 kiểm tra tích hợp trên LocalDB riêng: thực thi/chạy lại SQL161–162, rút đồng thời, gửi lặp, duyệt hai người, rút cạnh tranh với thanh toán, hoàn ngân hàng cạnh tranh hoàn số dư, chặn số tiền callback sai/chữ ký sai, Return/IPN lặp, thanh toán kết hợp số dư với phản hồi có chữ ký thử nghiệm, bảo toàn quyền lợi KOC và hoàn phần còn lại sau hủy. Harness tại `devops/finance-integration`; chạy từ repo bằng `dotnet run --project devops/finance-integration/FinanceIntegration.csproj`. Chỉ tạo database `TopstarFinanceTest_<guid>` trên LocalDB rồi xóa nó trong finally; không đọc connection string hay khóa thanh toán của ứng dụng. Fixture dùng model EF với cascade conventions được bỏ riêng cho test và các script lifecycle/escrow/callback liên quan.

Các kiểm tra chữ ký dùng secret và payload nội bộ; chưa gọi cổng thanh toán hoặc SMTP thật. Sau deploy cần thử một lượt sandbox qua trình duyệt với URL Return/IPN và email của môi trường triển khai.
