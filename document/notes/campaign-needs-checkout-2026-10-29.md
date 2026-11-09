# Nhu cầu → thông tin → thanh toán chiến dịch

## Triển khai

1. Sao lưu cơ sở dữ liệu theo quy trình triển khai hiện hành.
2. Chạy `database/sql/169_campaign_creation_checkout.sql` sau các SQL 165–168 và các script thanh toán/ví trước đó (159–163). Script chạy lại được; đã kiểm tra trên LocalDB dùng riêng cho kiểm thử.
3. Deploy binaries sau SQL. Không chạy lại 168 sau 169 vì 169 mở rộng bảo vệ snapshot phí KOC.
4. Admin → Tài chính → Tạo chiến dịch và phí KOC (`/Admin/Finance/CampaignCreation`). Mặc định: ngân sách tối thiểu 5 triệu, ngưỡng tự chọn 10 triệu, tối thiểu bình quân 1 triệu/KOC, tối đa 100 KOC. Đây là mặc định vận hành có thể thay đổi, không phải định giá bắt buộc.
5. Phí nền tảng KOC mặc định 0% để không tự đặt mức phí kinh doanh. Admin phải nhập mức đã thống nhất. Phí Brand dùng cấu hình Chính sách phí hiện tại; thuế Brand dùng phạm vi Funding; thuế KOC dùng chính sách thuế hiện tại.
6. Admin → Cấu hình gói KOC: nhập **Ngân sách chiến dịch tham khảo** (`ReferenceBudget`), số KOC, quyền tự chọn, nội dung và ảnh. Chỉ mẫu có ngân sách tham khảo phù hợp quy tắc mới hiện public. Không lấy giá dịch vụ cũ làm ngân sách. SQL không tự đổi giá/đơn cũ.

## Brand

- `/Brand/CampaignNeeds`: chọn mẫu hoặc tùy chỉnh ngân sách/số KOC/cách tuyển. Bên dưới ngưỡng tự chọn chỉ được TopStar phân bổ. Kiểm tra cả ở server, không tin hidden inputs. Có báo giá dự kiến ngân sách + phí Brand + VAT, chưa gồm phí phương thức thanh toán.
- `/Brand/CampaignEdit`: mẫu nội dung hiện tại, nhu cầu tóm tắt phía trên. Ngân sách và số KOC không được lén thay đổi khi lưu nội dung; có nút quay lại nhu cầu trước khi tạo hồ sơ.
- `/Brand/CampaignCheckout`: kiểm tra tổng, phương thức online/chuyển khoản/số dư, checkbox xác nhận bắt buộc. Tổng do server tính, so sánh với tổng đã hiển thị. Có thể dùng số dư một phần cho chuyển khoản hoặc VNPay. OnePay dự phòng chưa hỗ trợ kết hợp ví.
- Hồ sơ và thanh toán tách trạng thái: Draft + Pending chưa được tuyển KOC. Đủ tiền bằng ví, đối soát ngân hàng hoặc callback có chữ ký hợp lệ tự chuyển **Submitted**, chưa công khai. Admin chỉ duyệt nội dung để Published.
- Phải thanh toán toàn bộ ngân sách tạo mới; không thể nộp một khoản nhỏ để vượt bước kiểm tra. Không cần KOC xác nhận trước khi thanh toán.
- Khi yêu cầu sửa nội dung, tiền đã thanh toán giữ nguyên; lưu lại nội dung để gửi duyệt lại, không thanh toán lần nữa. Điều chỉnh ngân sách sau khi hồ sơ được tạo chưa hỗ trợ trong đợt này.
- Mọi tiền tiếp tục qua sổ ví và quỹ chiến dịch cũ; không cộng ngân sách vào số dư khả dụng đồng thời. Hủy/hoàn dùng đối soát hiện tại, không tự hoàn toàn bộ phí/VAT.

## KOC

- Phí nền tảng % chốt trên Campaign lúc tạo; cập nhật cấu hình sau không thay đổi chiến dịch cũ.
- Brand được phân bổ từ quỹ đã trả cho KOC đang Invited. KOC chỉ nhận việc sau khi có khoản phân bổ Active được bảo đảm bằng quỹ đã trả. Không tự mở công việc.
- KOC xem gross, phí nền tảng, thuế dự kiến và net trước khi nhận. Email lời mời mới chỉ gửi khi có thù lao và có bảng phân tích. Phân bổ lưu thành công tạo thông báo và gửi email qua SMTP hiện tại.
- Khi ghi nhận earning: phí = round(gross × rate / 100); thuế theo chính sách hiện tại tính trên gross; net = gross − phí − thuế. Không khấu trừ lại khi khả dụng/rút tiền. Nếu tổng khấu trừ vượt gross, giao dịch bị từ chối để xử lý cấu hình.
- Chờ xác minh thuế vẫn OnHold, không rút được. Snapshot phí và thuế đã chốt được trigger bảo vệ.

## Tương thích

`CheckoutAtCreation=false` cho tất cả chiến dịch đã có. Đơn mua gói và chiến dịch cũ vẫn tra cứu, dùng quyền lợi và đối soát như trước. Không tự đổi đơn Paid thành số dư, không tự hoàn hay thu thêm tiền cũ. Các CTA public và Brand mới đi vào xác định nhu cầu.

## Kiểm tra

Build Razor/.NET; bộ tích hợp LocalDB kiểm tra khả năng chạy lại SQL, tương thích các luồng cũ, ngưỡng ngân sách/quyền tuyển, thanh toán ví/chống replay, callback ký số, đối soát ngân hàng, chặn duyệt khi chưa trả, phân bổ Invited trước nhận việc, khấu trừ phí và thuế đúng một lần.
Không gửi giao dịch thanh toán thật hoặc email SMTP thật trong kiểm thử. Không chạy SQL trên database ứng dụng từ agent.

Kết quả kiểm tra ngày 30/10/2026: 156 kiểm tra tích hợp đạt trên database LocalDB riêng đã được xóa sau kiểm thử. Build Release/Razor thành công. Agent không chạy script 169 trên database ứng dụng. Đã kiểm tra read-only thấy schema 169 có trong database cấu hình và chạy lại bản localhost sau khi build.
