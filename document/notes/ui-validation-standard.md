# Quy chuẩn bẫy lỗi giao diện toàn hệ thống

Áp dụng bắt buộc cho mọi form và modal của KOC, Brand, Admin.

## Mẫu hiển thị chuẩn

- Form phải có `novalidate`; tuyệt đối không để trình duyệt hiện validation bubble như `Please fill out this field`.
- Trường lỗi có viền đỏ.
- Có icon cảnh báo màu đỏ nằm **bên trong góc phải** của `input`, `textarea` hoặc vùng chọn.
- Dòng lỗi tiếng Việt màu đỏ nằm ngay bên dưới trường.
- Dòng lỗi **không lặp lại icon** vì icon đã nằm trong trường.
- Nội dung lỗi phải nêu rõ điều kiện, ví dụ: `Vui lòng nhập ghi chú từ 10 đến 500 ký tự.`
- Focus trường lỗi đầu tiên; modal không đóng khi validation thất bại.
- Khi người dùng nhập/chọn lại, trạng thái lỗi được cập nhật hoặc xóa ngay.
- Giữ nội dung người dùng đã nhập khi lỗi client hoặc lỗi nghiệp vụ từ server.
- Dùng `aria-invalid` và `aria-describedby`.

## Luồng kiểm tra

1. JavaScript trim dữ liệu và kiểm tra required/min/max/format trước submit hoặc AJAX.
2. Không gửi request nếu client validation chưa đạt.
3. Backend luôn kiểm tra lại cùng business rule; không tin dữ liệu trình duyệt.
4. Lỗi backend trả tiếng Việt và gắn đúng trường hoặc validation summary.
5. Submit hợp lệ mới bật loading và khóa nút chống gửi lặp.

## Modal

- Modal thao tác dùng backdrop tĩnh và không đóng bằng phím Escape nếu có dữ liệu nhập.
- Không dùng `alert()`, `confirm()` hoặc validation popup mặc định.
- Hành động nhạy cảm phải có icon, mô tả hậu quả, lý do bắt buộc khi nghiệp vụ yêu cầu, anti-forgery, history và audit.

## Ví dụ trực quan

```text
[ Nội dung textarea                              (!) ]
  Vui lòng nhập ghi chú từ 10 đến 500 ký tự.
```

Tài liệu này là nguồn quy chuẩn chung. Notes theo từng chức năng chỉ ghi rule nghiệp vụ cụ thể, không được định nghĩa một kiểu validation khác.
## Lưu ý khi trường nhập có wrapper chứa icon

- Nếu `input`/`textarea` được bọc trong một phần tử để đặt icon cảnh báo bên trong, `.invalid-feedback` không còn là sibling trực tiếp của trường nhập.
- CSS của từng portal phải chủ động hiển thị phản hồi theo trạng thái của wrapper, ví dụ: `.validation-field:has(.is-invalid) + .invalid-feedback { display: block; }`.
- Khi cập nhật CSS/JavaScript validation, phải đổi cache version của asset để trình duyệt nhận đúng bản mới.

## Nhãn nút lưu

- Nút lưu dữ liệu ghi ngắn gọn **Lưu**, kèm icon đĩa mềm. Không lặp lại tên mục đang mở.
- Dùng màu và nút Hủy/Lưu nhất quán giữa các tab cùng form.
- Chỉ giữ nhãn riêng khi thao tác còn gửi duyệt, thanh toán hoặc hành động nghiệp vụ khác.
- MST và số CCCD: chỉ bắt buộc có nội dung; không ép độ dài hoặc số ký tự. Giữ nguyên số 0 đầu.


## Quy chuẩn hiển thị chung (31/10/2026)

- `ui-standard.css` được tải sau CSS từng trang trong layout Admin, Brand, KOC và public. Tab dùng `ts-tabs` theo mẫu tab chỉnh sửa hồ sơ KOC, không tạo kiểu pill riêng cho mỗi trang. Không gạch chân link thường; liên kết cấu hình có thể trình bày như nút có icon.
- Form controls bỏ số 0 thập phân thừa của ô số; giữ nguyên giá trị phần lẻ có ý nghĩa. Tiền VND dùng dấu chấm phân cách và ký hiệu đ trong ô. `data-money-input` đánh dấu trường số tiền khi tên không đủ rõ. Form có mask riêng (`js-money`, `data-money`, tên Display) giữ nguyên xử lý cũ, chỉ bổ sung cách trình bày nếu thiếu ký hiệu tiền.
- Bộ chuyển đổi tiền dùng ô số gốc có tên để gửi giá trị thuần; ô hiển thị không mang name. Không gửi dấu phân cách và không diễn giải phần lẻ thành số tiền lớn hơn. Giá trị nguyên đồng vẫn được kiểm tra tại server.
- Ngày và ngày giờ dùng jQuery UI datepicker tiếng Việt; icon lịch trong ô mở bộ chọn. Ô gốc giữ định dạng ISO để binding không phụ thuộc định dạng hiển thị. Ngày giờ giữ riêng ô giờ. Đầu vào disabled/readonly và reset được đồng bộ.
