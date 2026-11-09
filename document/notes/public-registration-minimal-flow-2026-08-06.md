# Tinh gọn bước đăng ký tài khoản - 06/08/2026

## Quyết định nghiệp vụ

Đăng ký ban đầu chỉ có nhiệm vụ tạo tài khoản. Thông tin hồ sơ phục vụ xét duyệt, matching và vận hành được hoàn thiện sau khi người dùng đăng nhập.

### KOC/KOL

- Bước đăng ký: họ tên, email, mật khẩu.
- Chuyển sang hồ sơ: số điện thoại, giới tính, ngày sinh, địa chỉ, hình thể, mạng xã hội và nhóm nội dung.

### Brand/Doanh nghiệp

- Bước đăng ký: người đại diện, tên Brand/doanh nghiệp, email, mật khẩu.
- Chuyển sang hồ sơ: số điện thoại, địa chỉ, tỉnh/thành, phường/xã, thuế và pháp lý.

## Luồng sau đăng ký

- Hệ thống tạo tài khoản và hồ sơ ở trạng thái `Draft`.
- KOC được chuyển đến `/Koc/Profile?forceComplete=1`.
- Brand được chuyển đến `/Brand/EditProfile?forceComplete=1`.
- Các điều kiện đầy đủ hồ sơ, kiểm duyệt và mở quyền tính năng vẫn giữ nguyên.

## Điều chỉnh kỹ thuật

- Loại bỏ validation và xử lý vị trí khỏi form đăng ký nhanh.
- Loại bỏ trường ngày sinh cũ vì trường này chỉ được kiểm tra nhưng không được lưu ở bước tạo tài khoản.
- Không dùng cách ẩn trường bằng CSS; binding và xử lý máy chủ đã được tinh gọn tương ứng.

## Kiểm tra

- Build solution: thành công, 0 lỗi, 0 cảnh báo.
- Application tests: 262/262 đạt.
- Tab KOC và Brand: HTTP 200.
- Kiểm tra HTML xác nhận các trường hồ sơ không còn xuất hiện ở form đăng ký.
- Website chạy tại `http://localhost:5201`.

## Database

- Không thay đổi database, không có SQL cần chạy.
