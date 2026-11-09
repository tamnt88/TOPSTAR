# KOC Wallet - UI, email OTP và hủy yêu cầu rút tiền (2026-07-30)

## Đã hoàn thành

- Sửa nút `Rút tiền`: icon và chữ cùng màu, căn giữa, có khoảng cách ổn định.
- Sửa khu vực tài khoản nhận tiền mặc định:
  - badge `Mặc định` không còn bị ép thành hình tròn hoặc xuống dòng;
  - badge và nút `Xóa` không còn chồng lên nhau;
  - chỉ khối thông tin tài khoản được phép co giãn.
- Email OTP rút tiền dùng `IEmailTemplateRenderer` chung của TopStar:
  - có logo/header;
  - tiêu đề và nội dung giao dịch;
  - OTP nổi bật, thời hạn 5 phút và cảnh báo bảo mật;
  - có footer chứa thông tin liên hệ/website.
- KOC có thể hủy yêu cầu rút tiền đang chờ xử lý:
  - chỉ trạng thái `WithdrawRequested` được hủy;
  - bắt buộc lý do từ 10 đến 500 ký tự, hiển thị lỗi inline theo chuẩn;
  - cập nhật có điều kiện để tránh xung đột với Admin đang xử lý;
  - trạng thái mới là `Cancelled` / `Đã hủy`;
  - lý do, lịch sử trạng thái và audit được lưu;
  - số tiền của lệnh đã hủy được trả lại số dư khả dụng.
- Có xử lý phòng vệ khi database chưa cho phép trạng thái `Cancelled`: trả thông báo thân thiện, không bung trang lỗi kỹ thuật.

## Quy tắc nghiệp vụ

- Không cho hủy lệnh đã chi trả, thất bại hoặc đã hủy.
- Nếu Admin xử lý cùng lúc, chỉ thao tác cập nhật trạng thái đầu tiên thành công.
- Hủy không xóa bản ghi payout; lịch sử tài chính phải được giữ nguyên để đối soát.

## SQL bắt buộc

Chạy file sau trên đúng database của website:

`database/sql/100_allow_cancelled_payout_request_status.sql`

Script idempotent theo constraint có tên chuẩn: xóa constraint cũ và tạo lại với bốn trạng thái
`WithdrawRequested`, `PaidOut`, `Failed`, `Cancelled` cho cả:

- `dbo.PayoutRequests.Status`;
- `dbo.PayoutRequestStatusHistories.FromStatus`;
- `dbo.PayoutRequestStatusHistories.ToStatus`.

Script trả về định nghĩa của cả ba constraint để kiểm tra. Nếu đã chạy phiên bản đầu của
migration 100 thì phải chạy lại file này vì phiên bản đầu chưa cập nhật hai constraint lịch sử.

## Kiểm tra

- `dotnet build backend/Topstar.sln --no-restore`
- `dotnet test backend/Topstar.sln --no-build --no-restore`
