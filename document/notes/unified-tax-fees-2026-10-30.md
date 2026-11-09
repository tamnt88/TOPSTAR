# Thuế và phí Brand / KOC

Admin > Tài chính > Thuế và phí (`/Admin/Finance/TaxFees`).

- Brand: GTGT, phí nền tảng, phí thanh toán và quy tắc vận hành; hiển thị lịch sử phiên bản.
- KOC: phí nền tảng, thuế TNCN và phí/điều kiện rút tiền. Đối soát và chứng từ nộp thuế ở trang riêng.
- Cấu hình tạo chiến dịch chỉ còn ngưỡng ngân sách, số KOC và quyền tự chọn. Không ghi đè phí KOC khi lưu nhu cầu.
- Bỏ các mục cấu hình thuế/phí trùng trong menu tài chính. GET các đường dẫn cấu hình cũ chuyển về màn hình chung; POST giữ dịch vụ kiểm tra/audit hiện có.

## Tương thích thuế Brand đã lưu

Luồng hiện tại đọc chính sách Funding đã có hiệu lực. Nếu chưa có phiên bản Funding hiệu lực, dùng chính sách Package hiệu lực đã được Admin cấu hình, giữ đúng thuế suất, chế độ và cơ sở tính thuế. Chính sách Funding None có hiệu lực luôn ưu tiên, không bị chính sách cũ ghi đè. Cấu hình mới lưu ở màn hình chung tạo phiên bản Funding. Không tự ghi hay chuyển dữ liệu trong database ứng dụng.

GTGT chỉ trên phí dịch vụ: phí × thuế suất; trên tổng: (ngân sách + phí) × thuế suất. Giá đã gồm thuế tách GTGT, không cộng thêm. Dự kiến, checkout và chốt yêu cầu dùng cùng BrandTaxPricing. Các snapshot đã chốt không đổi. Ví dụ cấu hình 8% trên tổng: 5.000.000 + 500.000 + 440.000 = 5.940.000 đồng; nếu chỉ trên phí dịch vụ, thuế là 40.000 đồng.

Không cần SQL mới; dùng schema 166–169 hiện có. Kiểm thử trên database LocalDB riêng, không thay đổi cấu hình hoặc giao dịch thật.

## Sửa lỗi lưu ngày 31/10/2026

Các thuộc tính chuỗi chỉ dùng trong form Brand được để nullable ở PageModel màn hình chung để MVC không tự áp Required cho form KOC không gửi những trường đó. Handler GTGT vẫn kiểm tra bắt buộc bằng quy tắc riêng. Tab KOC gửi rõ ngữ cảnh; lỗi trường hiện ngay trong form, không chặn vì trường thuộc form khác.

Bỏ card lồng trong card cho các trình chỉnh sửa và bỏ xác nhận trung gian khi lưu phí rút tiền. Dùng thông báo chung sau lưu. Đồng bộ font hộp xác nhận còn dùng ở các thao tác khác theo font portal.

182 kiểm tra tích hợp trên database LocalDB riêng đạt, gồm POST HTTP thực với antiforgery/cookie, đọc lại phí và thuế đã lưu, thông báo thành công sau redirect, trường sai giữ giá trị và không ghi DB. Kiểm tra Chrome cho Razor thực xác nhận không còn card lồng hoặc form data-confirm trong màn hình này.
