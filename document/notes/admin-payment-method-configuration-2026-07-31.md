# Admin payment method configuration — 2026-07-31

- Bổ sung menu `Cài đặt hệ thống → Phương thức thanh toán`.
- Trang `/Admin/Settings/PaymentMethods` quản lý chuyển khoản và OnePAY.
- Chuyển khoản gồm ngân hàng, số tài khoản, chủ tài khoản và trạng thái hoạt động.
- OnePAY gồm môi trường Test/Production, Merchant ID, Access Code, Base URL, Secure Hash Secret, Return URL và Callback/IPN URL.
- Ba kênh OnePAY được bật/tắt độc lập: thẻ nội địa/Internet Banking, thẻ quốc tế và ví điện tử.
- Secure Hash Secret được mã hóa bằng ASP.NET Data Protection; khi sửa cấu hình không trả secret về HTML.
- Return URL mặc định: `https://topstar.vn/Brand/OnePayReturn`.
- Callback/IPN mặc định: `https://topstar.vn/api/payments/onepay/ipn`.
- Thay đổi cấu hình được ghi `AuditLogs` nhưng không ghi secret.
- SQL bắt buộc: `database/sql/108_create_payment_method_configuration.sql`, UTF-8, idempotent.
- Build 0 warning/0 error; test 238/238.

## Nối cấu hình vào checkout

- Checkout không còn hardcode chuyển khoản; đọc trực tiếp `PaymentMethodConfigurations`.
- Nếu Admin bật cả hai, giao diện hiển thị `Chuyển khoản ngân hàng` và `Thanh toán online qua OnePAY`.
- OnePAY hiển thị các kênh con theo đúng cờ cấu hình: nội địa/Internet Banking, quốc tế và ví điện tử.
- Backend kiểm tra lại phương thức và kênh còn hoạt động khi submit, không chỉ tin dữ liệu từ trình duyệt.
- Nút quay lại được cố định trong vùng header checkout và tách khoảng cách khỏi grid nội dung.
- QA local: cấu hình `1|1|1|1|1`; checkout render đủ hai phương thức và ba kênh OnePAY.

## Nâng cấp payment card

- Hai phương thức được trình bày thành hai card cân đối trên desktop và xếp dọc trên mobile.
- Card chuyển khoản hiển thị trực tiếp ngân hàng, số tài khoản, chủ tài khoản và ghi chú nội dung chuyển khoản sinh theo đơn.
- Card OnePAY nhóm ba kênh thành lựa chọn có tiêu đề/phụ đề và bổ sung thông điệp không lưu thông tin thẻ.
- Card được chọn có border, nền và shadow nhận diện rõ; mặc định chọn chuyển khoản nếu đang hoạt động.
- Admin không thể bật chuyển khoản khi thiếu một trong ba thông tin tài khoản; checkout cũng kiểm tra lại ở backend.
- Theo QA bố cục, payment card chuyển về dạng ngang toàn chiều rộng: chuyển khoản là một hàng với ba thông tin tài khoản; OnePAY là một hàng với ba kênh đặt ngang. Mobile tự xếp một cột.
