# Brand - hoàn thiện trang xem hồ sơ

Ngày cập nhật: 31/07/2026

## Vấn đề đã rà soát

- Trang `/Brand/Profile` chỉ hiện tiêu đề và cảnh báo, không hiển thị dữ liệu hồ sơ.
- Nội dung tiếng Việt trong Razor Page và PageModel bị lỗi mã hóa.
- Khi dữ liệu hồ sơ không tồn tại, trang chưa có trạng thái rỗng rõ ràng.
- Trình duyệt có thể tiếp tục dùng bản CSS cũ do URL asset chưa đổi phiên bản.

## Phần đã hoàn thiện

- Viết lại trang xem hồ sơ Brand bằng dữ liệu thật từ `Users`, `BrandProfiles`, tỉnh/thành, phường/xã và thống kê chiến dịch.
- Bổ sung phần nhận diện Brand, trạng thái hồ sơ, ngày tham gia và nút chỉnh sửa.
- Bổ sung ba chỉ số tổng quan: tổng chiến dịch, chiến dịch đã hoàn tất và trạng thái hồ sơ.
- Chia thông tin thành các khối dễ đọc:
  - Thông tin doanh nghiệp và người đại diện.
  - Địa chỉ và thông tin liên hệ.
  - Thuế, xuất hóa đơn và giấy phép kinh doanh.
- Chuẩn hóa toàn bộ nhãn và trạng thái sang tiếng Việt.
- Bổ sung empty state chuyên nghiệp khi chưa có dữ liệu.
- Tăng phiên bản cache của CSS Brand để trình duyệt tải đúng giao diện mới.

## Kiểm tra kỹ thuật

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- CSS Brand trả HTTP 200.
- `/Brand/Profile` trả HTTP 302 về trang đăng nhập khi chưa xác thực, đúng cơ chế phân quyền.
- Website đang chạy tại `http://localhost:5201`.

## SQL cần chạy

Không cần chạy SQL cho thay đổi này. Trang sử dụng các cột và bảng hiện có.

## Cách kiểm tra

1. Đăng nhập bằng tài khoản Brand.
2. Chọn menu `Hồ sơ Brand`.
3. Kiểm tra các nhóm doanh nghiệp, địa chỉ, thuế và pháp lý đã hiển thị.
4. Chọn `Chỉnh sửa hồ sơ` để đối chiếu dữ liệu và hoàn thiện các mục còn thiếu.

