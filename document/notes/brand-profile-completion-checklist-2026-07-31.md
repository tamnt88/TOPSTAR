# Checklist hoàn thiện hồ sơ Brand

Ngày cập nhật: 31/07/2026

## Quy chuẩn hiển thị

- Cảnh báo ở layout phải cho biết hồ sơ còn thiếu bao nhiêu mục, không dùng thông báo chung chung.
- Trang chỉnh sửa hiển thị phần trăm hoàn thiện và checklist các trường còn thiếu.
- Mỗi mục trong checklist phải bấm được để mở đúng tab, cuộn tới và focus đúng ô cần cập nhật.
- Nếu đã đủ dữ liệu nhưng chưa được Admin duyệt, hiển thị trạng thái `Đang chờ duyệt` thay vì tiếp tục báo thiếu dữ liệu.
- Không hiển thị ký tự Razor/CSS thừa trong giao diện.

## Tiêu chí hoàn thiện

Checklist lấy trực tiếp từ `ProfileAccessService.CheckBrandAsync`, gồm 12 tiêu chí:

1. Email liên hệ.
2. Số điện thoại tài khoản.
3. Tên công ty/Brand.
4. Tên người đại diện.
5. Số điện thoại liên hệ.
6. Số nhà và tên đường.
7. Tỉnh/Thành phố.
8. Phường/Xã.
9. Mã số thuế.
10. Tên pháp lý theo mã số thuế.
11. Địa chỉ xuất hóa đơn.
12. Giấy phép kinh doanh.

## Trạng thái

- Thiếu ít nhất một tiêu chí: `Cần bổ sung`.
- Đủ 12 tiêu chí nhưng chưa được duyệt: `Đang chờ duyệt`.
- Được Admin duyệt: `Đã duyệt`.
- Các trạng thái từ chối hoặc khóa vẫn phải hiển thị đúng tiếng Việt và giữ lịch sử xử lý.

Không cần chạy SQL cho thay đổi giao diện và checklist này.

## Select địa chỉ có tìm kiếm

- Hai ô `Tỉnh/Thành phố` và `Phường/Xã` trên hồ sơ Brand dùng bộ chọn có ô tìm kiếm.
- Tìm kiếm không phân biệt chữ hoa, chữ thường hoặc dấu tiếng Việt.
- Danh sách Phường/Xã vẫn tải theo Tỉnh/Thành phố đã chọn; khi đổi tỉnh phải xóa lựa chọn phường cũ.
- Trạng thái đang tải và trạng thái vô hiệu hóa của Phường/Xã phải được phản ánh trên bộ chọn mới.
- CSS và JavaScript chỉ đặt trong namespace/file của Brand, không dùng chung với KOC hoặc Admin.

## Bẫy lỗi form Brand

- Form hồ sơ dùng `novalidate` kết hợp jQuery unobtrusive và kiểm tra lại toàn bộ ở server; không phụ thuộc validation mặc định của trình duyệt.
- Khi có lỗi:
  - Hiển thị banner và toast tiếng Việt thống nhất.
  - Đánh dấu viền ô lỗi.
  - Hiển thị icon cảnh báo trong input/textarea/select lỗi và trước từng thông báo lỗi.
  - Đánh dấu tab đang chứa lỗi.
  - Tự mở đúng tab, cuộn và focus vào lỗi đầu tiên.
- Khi submit hợp lệ, nút lưu bị khóa và hiển thị trạng thái `Đang lưu...` để tránh gửi lặp.
- Server chuẩn hóa và kiểm tra thêm:
  - Email đúng định dạng và không trùng tài khoản khác.
  - Số điện thoại tài khoản/liên hệ đúng định dạng Việt Nam.
  - Mã số thuế gồm 10 chữ số hoặc dạng `10 chữ số-3 chữ số`.
  - Giới hạn độ dài tên doanh nghiệp, người đại diện và địa chỉ.
  - Tỉnh/Thành phố và Phường/Xã phải tồn tại và đúng quan hệ.
  - Giấy phép kinh doanh chỉ nhận PDF/JPG/PNG, tối đa 10 MB.
- Khi backend gặp lỗi ngoài dự kiến, giao diện chỉ hiển thị thông báo an toàn; chi tiết exception chỉ ghi vào application log.
- Trạng thái completion/checklist được nạp lại khi POST lỗi, không bị trở về dữ liệu mặc định.
- Đã QA POST với nhiều dữ liệu sai đồng thời: HTTP 200, giữ nguyên form và trả đầy đủ thông báo lỗi theo từng trường.
- Build thành công, 0 warning, 0 error; 238/238 test đạt.
- Không thay đổi database và không có SQL cần chạy.
