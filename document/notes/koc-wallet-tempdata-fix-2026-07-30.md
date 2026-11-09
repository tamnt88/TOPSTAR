# Khắc phục lỗi TempData ví KOC ngày 30/07/2026

## Nguyên nhân

Cookie TempData cũ chứa ID kiểu `Guid`, trong khi PageModel trước đây nhận
giá trị bằng `string`. Bộ lọc TempData của Razor Pages cố ép `Guid` sang
`string` trước khi handler chạy và gây `InvalidCastException`.

## Quy chuẩn xử lý

- `PayoutChallengeId` và `PayoutAccountId` phải dùng đúng kiểu `Guid?`.
- Dùng khóa TempData phiên bản mới `WalletPayoutChallengeIdV2` và
  `WalletPayoutAccountIdV2` để không đọc cookie cũ không tương thích.
- Không sửa lỗi bằng cách xóa lịch sử nghiệp vụ hoặc ép ID về chuỗi tùy tiện.
- Khi thay kiểu dữ liệu của thuộc tính TempData, phải đổi khóa hoặc có chiến
  lược chuyển đổi tương thích ngược.

## Xác minh

- Solution build sạch: 0 lỗi, 0 cảnh báo.
- Test: 237/237 đạt.
- Website và `/css/koc/koc.css` phản hồi HTTP 200 trên cổng 5201.
