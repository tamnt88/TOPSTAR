# VNPay: mua gói và ký quỹ

## Nhật ký đầy đủ Return/IPN

Chạy thêm database/sql/160_add_vnpay_callback_logs.sql trước khi triển khai bản có nhật ký. Mỗi lần nhận Return/IPN đều lưu nguyên query string và toàn bộ tham số JSON, giữ cả tham số chưa biết và giá trị lặp; không chỉ lưu giao dịch thành công. Nhật ký ghi thời điểm nhận/xử lý, nguồn, IP, User-Agent, kết quả chữ ký, mã kết quả và lỗi xử lý.

Dữ liệu gốc không được sửa/xóa; kết quả xử lý được cập nhật sau khi kiểm tra. Nhật ký dùng transaction riêng để không mất phản hồi khi giao dịch tiền thất bại. Nếu hạ tầng ghi nhật ký lỗi, ứng dụng ghi lỗi vận hành và vẫn cho phép xử lý thanh toán an toàn; cần giám sát log vận hành này.

Admin xem trong modal đơn mua gói hoặc tab Thanh toán của hồ sơ ký quỹ: Dữ liệu giao dịch VNPay → mở từng lần Return/IPN → Xem toàn bộ tham số nhận được. Phản hồi có chữ ký sai được đánh dấu rõ và không được coi là bằng chứng thanh toán.

Không thể backfill thông tin phản hồi đầy đủ cho giao dịch cũ chỉ từ mã giao dịch đã lưu. Nếu còn URL kết quả có chữ ký, mở lại để hệ thống lưu nhật ký bổ sung và xử lý theo cơ chế chống trùng.

## Thử nghiệm trên localhost

- Chọn môi trường Thử nghiệm và dùng TmnCode/Hash Secret sandbox.
- URL thanh toán: https://sandbox.vnpayment.vn/paymentv2/vpcpay.html.
- Return URL: http://localhost:7161/Brand/VnPayReturn (chỉ cho phép HTTP loopback trong chế độ thử nghiệm).
- IPN URL: phải là HTTPS công khai, trỏ tới /api/payments/vnpay/ipn trên cùng ứng dụng và cơ sở dữ liệu, ví dụ thông qua tunnel HTTPS. Đăng ký URL này với VNPay.
- Nếu Return trỏ tới topstar.vn mà máy chủ chưa triển khai bản mới thì sẽ gặp 404, dù localhost đã có trang này. Có thể dùng tên miền staging đã triển khai bản mới cho cả Return/IPN.
- Return và IPN đều kiểm tra chữ ký, merchant, mã đơn, số tiền và trạng thái giao dịch trước khi ghi nhận. Phản hồi thành công hợp lệ ở Return có thể hoàn tất thanh toán khi IPN chưa tới; IPN đến sau không ghi nhận trùng.

1. Chạy database/sql/159_add_vnpay_payment_configuration.sql trước khi triển khai binaries mới. Script giữ nguyên cấu hình OnePAY, tài khoản ngân hàng và dữ liệu tài chính; VNPay mặc định tắt.
2. Build/deploy ứng dụng rồi mở /Admin/Settings/PaymentMethods. Nhập TmnCode, Hash Secret, URL thanh toán theo môi trường, Return URL và IPN URL. Dùng tài khoản merchant của Topstar; không sao chép thông tin đăng nhập BeautyStory.
3. Đăng ký IPN URL với VNPay: https://topstar.vn/api/payments/vnpay/ipn. Return URL: https://topstar.vn/Brand/VnPayReturn. Localhost không nhận IPN từ máy chủ VNPay.
4. Bật VNPay. Đơn mới hiển thị “Thanh toán online” qua VNPay; đơn OnePAY cũ giữ nguyên phương thức để theo dõi.

Secret hiện rõ cho Admin được phân quyền, không gửi xuống trang Brand, không ghi vào audit. Giá trị trong DB vẫn dùng Data Protection. Cần giữ key ring Data Protection khi chuyển máy chủ.

Mua gói lấy số tiền từ AmountSnapshot; ký quỹ lấy TotalPayableSnapshot gồm các phí đã chốt. URL không nhận số tiền từ trình duyệt. Return và IPN dùng chung quy trình ghi nhận thanh toán, chỉ chấp nhận chữ ký hợp lệ và cả ResponseCode/TransactionStatus thành công. Xác thực HMAC-SHA512, TmnCode, mã đơn, phương thức, số tiền và trạng thái pending; receipt có khóa duy nhất cho giao dịch và đơn. Receipt, trạng thái, lịch sử, sổ quỹ, audit, thông báo cùng transaction; callback gửi lại không cộng tiền lần hai. Ký quỹ thành công không tự mở công việc KOC.

Trước khi bật thực tế, kiểm tra sandbox cho cả mua gói và ký quỹ: thành công; hủy trên cổng; callback sai chữ ký/số tiền; callback lặp; IPN đến trước/sau Return; đơn đã hủy; thanh toán lại khi lần trước chưa thành công; tài khoản Brand khác không tạo được URL của đơn. Đối chiếu đúng một receipt/lần ghi có/sự kiện lịch sử.

Đã có kiểm thử tự động cho mã hóa tham số, chữ ký và số tiền. Chưa xác nhận giao dịch sandbox hoặc thực tế khi chưa cấu hình merchant và endpoint IPN công khai.

Tham chiếu: [Tài liệu kết nối VNPay](https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html); mã tham khảo E:/HOAXINH/beautystory.vn/App_Code/VnPayService.cs.
