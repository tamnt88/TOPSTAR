# Seed số dư ví kiểm thử payout KOC

Ngày cập nhật: 30/07/2026.

## Dữ liệu kiểm thử

- Tài khoản: `seed.koc.0001@seed.topstar.local`.
- Script `96_seed_koc_wallet_payout_test_balance.sql` bổ sung 12.000.000 đ khả dụng, 1.500.000 đ đang đối soát và 750.000 đ tạm giữ.
- Script chạy trong transaction, dùng marker riêng và chạy lặp không nhân đôi.
- Không sửa hoặc xóa giao dịch, payout và lịch sử hiện hữu.

## Quy chuẩn lịch sử và bảo mật

- Mỗi khoản seed có snapshot `Pending`. Khoản khả dụng và tạm giữ có thêm sự kiện chuyển trạng thái trong `WalletTransactionStatusHistories`.
- Không seed `PayoutRequests` đang chờ vì yêu cầu đang chờ/đã chi được trừ khỏi số dư khả dụng, gây cản trở kiểm thử tạo yêu cầu rút và OTP.
- Không ghi trực tiếp tài khoản ngân hàng giả vào database. KOC phải nhập trên giao diện để số tài khoản được mã hóa bằng đúng Data Protection key của ứng dụng.

## Kiểm tra

- Chạy `97_verify_koc_wallet_payout_test_balance.sql` để đối chiếu sổ cái, khoản payout giữ chỗ, số dư thực rút được, lịch sử trạng thái, tài khoản nhận tiền và chính sách payout đang hiệu lực.
- Sau khi seed, nên kiểm thử lần lượt: mở Ví thu nhập, thêm/cập nhật tài khoản nhận tiền, tạo yêu cầu rút hợp lệ, xác nhận OTP, và kiểm tra lịch sử giao dịch/payout.

## Encoding tiếng Việt

- File SQL được lưu UTF-8. Khi chạy bằng `sqlcmd`, luôn truyền `-f 65001`; nếu không, chuỗi Unicode trong file có thể bị chuyển thành mojibake trước khi SQL Server nhận dữ liệu.
- Script `98_repair_koc_wallet_seed_vietnamese.sql` sửa phần chữ của đúng batch seed ví đã bị sai encoding. Script giữ nguyên ID, số tiền, trạng thái, actor và thời gian lịch sử.
- Trạng thái kỹ thuật không được hiển thị trực tiếp cho KOC. Giao diện ánh xạ `PaidOut` thành `Đã chi trả`, `Failed` thành `Thất bại`, cùng các trạng thái `Khả dụng`, `Đối soát`, `Tạm giữ`.

## Chi tiết lịch sử giao dịch

- Bảng lịch sử chỉ hiển thị dữ liệu phục vụ quét nhanh. Nút mắt ở cột `Chi tiết` mở modal riêng; không dùng `details`, popup nổi hay child row làm tràn/che bảng.
- Modal sử dụng `data-bs-backdrop="static"` và `data-bs-keyboard="false"`, có phần tổng quan chiến dịch, số tiền, trạng thái hiện tại, nội dung giao dịch, lý do tạm giữ và timeline đầy đủ.
- Dữ liệu modal được render sẵn trong `template` theo ID giao dịch rồi clone khi mở. Không ghép HTML từ chuỗi JSON và không đưa nội dung nghiệp vụ chưa escape vào thuộc tính HTML.
- Timeline hiển thị mới nhất trước, giữ nguyên toàn bộ lịch sử; modal có vùng cuộn riêng và chuyển ba thẻ tổng quan thành một cột trên màn hình nhỏ.

## Modal rút tiền

- Không đặt ô rút tiền trực tiếp trên thẻ số dư. Thẻ chỉ hiển thị tài khoản nhận và nút `Rút tiền`; toàn bộ thao tác được thực hiện trong modal riêng.
- Modal hiển thị số dư khả dụng, tài khoản nhận, số tiền yêu cầu, phí giao dịch và số tiền thực nhận dự kiến. Có lựa chọn nhanh 25%, 50%, 75% và toàn bộ số dư.
- Số tiền được định dạng phân cách hàng nghìn khi nhập nhưng gửi lên server dưới dạng số thuần để tránh sai lệch locale.
- Validation là validation nội tuyến của hệ thống: viền đỏ, icon lỗi và thông báo tiếng Việt ngay dưới trường; không dùng validation popup mặc định của trình duyệt.
- Lỗi nghiệp vụ từ server sẽ mở lại đúng modal và giữ số tiền đã nhập. Sau khi gửi OTP thành công, modal xác nhận OTP tự mở; OTP chỉ nhận đúng 6 chữ số.
- Hai modal dùng backdrop tĩnh và tắt đóng bằng phím Escape. Yêu cầu rút chỉ được tạo sau khi OTP hợp lệ; lịch sử payout tiếp tục được lưu theo luồng dịch vụ hiện có.
# Cập nhật 30/07/2026 — nhiều tài khoản nhận tiền

- Một KOC được lưu tối đa **5 tài khoản nhận tiền đang hoạt động**.
- Mỗi KOC luôn có đúng một tài khoản mặc định khi còn tài khoản hoạt động.
- Modal rút tiền bắt buộc chọn tài khoản nhận; lựa chọn được ghi cùng OTP và yêu cầu payout.
- Xóa tài khoản là **xóa mềm** (`DeletedAtUtc`, `DeletedReason`), không xóa lịch sử giao dịch/payout.
- Các thao tác thêm, đặt mặc định và xóa đều được ghi `AuditLogs`.
- Khối rút tiền trên nền gradient luôn dùng chữ trắng để bảo đảm tương phản.
- SQL cần chạy: `database/sql/99_enable_multiple_koc_payout_accounts.sql`.
- Quy ước validation vẫn là inline: viền đỏ, icon lỗi trong trường và thông báo tiếng Việt ngay bên dưới; không dùng validation bubble của trình duyệt.
