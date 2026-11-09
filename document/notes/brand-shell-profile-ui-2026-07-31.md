# Brand shell và hồ sơ doanh nghiệp — 2026-07-31

## Phạm vi đã hoàn thiện

- Tách riêng giao diện Brand khỏi KOC và Admin:
  - CSS: `wwwroot/css/brand/brand.css`
  - JavaScript: `wwwroot/js/brand/brand.js`
- Chuẩn hóa font Roboto và bộ màu Brand xanh ngọc, xanh navy, vàng nhấn.
- Thay menu phẳng bằng menu hai cấp:
  - Tổng quan
  - Chiến dịch
  - Vận hành
  - Hiệu quả
  - Hỗ trợ
- Bổ sung menu tài khoản gồm hồ sơ, cập nhật hồ sơ, đổi mật khẩu và đăng xuất.
- Đổi mật khẩu bằng modal cố định, không đóng khi bấm ra ngoài, kiểm tra lỗi nội tuyến và gửi AJAX.
- Trang cập nhật hồ sơ Brand được chia thành hai tab:
  - Thông tin doanh nghiệp
  - Thuế và pháp lý
- Chuẩn hóa khu vực cảnh báo hồ sơ chưa hoàn thiện, icon, nút thao tác và bố cục responsive.

## Quy ước tiếp tục áp dụng cho Brand

- Mọi CSS/JS đặc thù Brand phải đặt trong thư mục `css/brand` và `js/brand`.
- Không ghi style/script đặc thù Brand vào asset KOC hoặc Admin.
- Form dùng kiểm tra lỗi nội tuyến tiếng Việt; không dùng thông báo validation mặc định của trình duyệt.
- Modal thao tác quan trọng dùng `data-bs-backdrop="static"` và `data-bs-keyboard="false"`.
- Chỉ dùng `.container` của Bootstrap để quản lý chiều rộng, không thêm `max-width` cho khung nội dung chính.
- Menu cấp một chỉ thể hiện nhóm nghiệp vụ; chức năng chi tiết đặt ở menu cấp hai.

## Database

Thay đổi giao diện này không yêu cầu chạy SQL.
## Cập nhật menu Hồ sơ Brand và giao diện chỉnh sửa

- `Hồ sơ Brand` là menu cấp 1, truy cập trực tiếp trang chỉnh sửa hồ sơ và giữ trạng thái active ở cả `/Brand/Profile` lẫn `/Brand/EditProfile`.
- Dropdown tài khoản chỉ giữ các thao tác tài khoản như đổi mật khẩu và đăng xuất; không lặp liên kết hồ sơ.
- Trang chỉnh sửa không hiển thị lại banner cảnh báo chung vì đã có checklist tiến độ chi tiết ngay trong nội dung trang.
- Checklist chỉ rõ từng trường còn thiếu và điều hướng đúng tab/ô cần cập nhật.
- Form dùng toàn bộ chiều rộng của `container`, không bổ sung `max-width` riêng.
- Tab thông tin được giữ ở đầu vùng nhìn khi cuộn; Tỉnh/Thành phố và Phường/Xã dùng select có tìm kiếm.
- CSS/JS Brand tiếp tục tách riêng tại `/css/brand/brand.css` và `/js/brand/brand.js`; không dùng chung quy tắc riêng của KOC/Admin.
- Mỗi lần thay đổi giao diện Brand phải đổi cache key của asset để trình duyệt nhận bản mới.

## Trang xem hồ sơ Brand

- `/Brand/Profile` là trang xem hồ sơ và phải tải dữ liệu thật từ database; không dùng nội dung placeholder.
- Trang hiển thị tổng quan chiến dịch, trạng thái hồ sơ, thông tin doanh nghiệp, liên hệ, địa chỉ, thuế và hồ sơ pháp lý.
- Nếu Brand chưa có bản ghi hồ sơ thì mới điều hướng sang `/Brand/EditProfile`.
- Menu `Hồ sơ Brand` mở trang xem hồ sơ; nút `Chỉnh sửa hồ sơ` mới mở form cập nhật.
- Trạng thái hồ sơ phải hiển thị bằng tiếng Việt và tài liệu pháp lý có liên kết xem khi đã được tải lên.
- Thay đổi này chỉ bổ sung truy vấn và giao diện, không thay đổi schema nên không cần chạy SQL.
