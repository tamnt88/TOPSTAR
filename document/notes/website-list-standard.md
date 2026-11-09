# Quy chuẩn danh sách website Admin / Brand / KOC

Được người dùng đồng ý triển khai ngày 08/09/2026, theo bản đánh giá `2026-09-08-website-consistency-review.md`. Phạm vi là website.

## Quy ước chung

- Bảng mặc định 20 dòng, lựa chọn 10/20/50/100; server kiểm tra lại qua `PaginationDefaults.Normalize`.
- Một lựa chọn số dòng cho mỗi bảng; không lặp trên cả toolbar và footer.
- Từ khóa áp dụng bằng Enter hoặc nút Lọc; dropdown áp dụng ngay; có Xóa lọc.
- Bộ lọc, trang, số dòng và sắp xếp được lưu trên URL. Đổi bộ lọc về trang 1; sau thao tác giữ trang nếu còn dữ liệu.
- Danh sách phát sinh dài hạn lọc/đếm/phân trang tại DB. Danh mục nhỏ có thể dùng client-side với giới hạn rõ ràng.
- Phân biệt tổng trong phạm vi truy cập với tổng sau lọc. Không đếm dữ liệu ngoài quyền người dùng.
- Loading, lỗi tải, không có dữ liệu và không khớp bộ lọc là trạng thái khác nhau. Lỗi có Thử lại, không xóa bộ lọc hay giả làm dữ liệu rỗng.
- Sau thao tác cập nhật cả bảng và thống kê liên quan. Không tải lại toàn trang chỉ để cập nhật một bảng.
- Pagination thống nhất icon, nhãn tiếng Việt, hình dạng điều khiển và thông tin số lượng; màu có thể theo portal.

## Thành phần triển khai

- `wwwroot/js/shared/lists.js`: `TopstarLists.configure(element, options, fields, sizeSelector)` bao bọc cấu hình DataTables của trang; khôi phục URL, chuẩn hóa số dòng, lưu trạng thái, lỗi/Thử lại, trang hết dữ liệu, icon và nhãn phân trang.
- `fields` ánh xạ tên query trên URL sang selector của input; dữ liệu nghiệp vụ vẫn được gửi qua `ajax.data` của từng trang.
- Nút Xóa lọc được khai báo bằng `data-clear-filters` trên table. Trường ẩn chứa phạm vi như nhóm thông báo được giữ khi xóa từ khóa.
- `wwwroot/css/shared/lists.css`: kích thước và hành vi pagination chung, feedback tải dữ liệu và responsive.
- Ba layout tải asset chung với `asp-append-version`; bảng DataTables cũ nhận chuẩn icon/geometry qua sự kiện init/draw, không bị tự đổi truy vấn.
- `_Pager.cshtml` giữ toàn bộ RouteValues và bỏ bộ chọn số dòng trùng với toolbar Menu.

## Phạm vi đã áp dụng

1. Admin / Brands (bao gồm chế độ danh sách KOC qua Type): URL, số dòng, lỗi/Thử lại và pagination.
2. Brand / Campaigns: URL, Xóa lọc, số dòng, lỗi/Thử lại, pagination và cache version script.
3. KOC / Tasks: các mục trên; bỏ truy vấn danh sách trong OnGet; Data handler trả bảng cùng summary; tổng trước/sau lọc tách riêng; nộp bài cập nhật tại chỗ; thứ tự có khóa phụ Submission.Id.
4. Brand / Notifications: server-side tìm kiếm tiêu đề/nội dung, phân trang DB, lịch sử theo trang, hành động AJAX có anti-forgery, cập nhật số chưa đọc và giữ bộ lọc.
5. KOC / Notifications: server-side như Brand, giữ modal chi tiết, cập nhật bảng/thống kê sau thao tác; bỏ các lần tải toàn bộ inbox trong OnGet.
6. Booking Brand/KOC, giao nhận Brand/KOC, lịch sử hợp tác Admin/Brand, tranh chấp Admin/Brand/KOC: lọc, đếm và phân trang tại DB; lịch sử con chỉ tải cho các dòng của trang hiện tại.
7. Billing Brand/Admin, mua gói Brand/Admin, ví KOC, đối soát và yêu cầu chi trả Admin: mỗi danh sách có namespace URL riêng; tổng tiền và KPI được tính trên toàn phạm vi truy cập, không phụ thuộc trang.
8. Các bảng DataTables còn lại của Admin/Brand/KOC đi qua `TopstarLists.adapt`: mặc định 20 dòng, lựa chọn 10/20/50/100, nhãn/trạng thái tải thống nhất và trạng thái URL tách theo id bảng.
9. Định dạng phía server dùng `vi-VN`, ngày giờ nghiệp vụ hiển thị theo UTC+7 qua `PortalFormat`; các formatter AJAX chính dùng `TopstarFormat` với múi giờ `Asia/Ho_Chi_Minh`.
10. POST quay lại cùng trang giữ các tham số `list.*`, nên thao tác nghiệp vụ không làm mất bộ lọc/trang đang xem.

`GetInboxAsync` cũ vẫn dùng cho các nơi cần bản xem trước; danh sách thông báo đầy đủ dùng `GetPageAsync`, không còn tìm kiếm trên riêng 100 bản ghi đầu.

## Lưu ý phạm vi

- Ký quỹ chiến dịch đã bỏ giới hạn 200: danh sách chiến dịch phân trang tại DB, còn ledger/chứng từ/phân bổ chỉ tải cho các funding thuộc trang hiện tại. Các KPI và tổng tiền chờ đối soát lấy trên toàn phạm vi.
- Lịch sử cấu hình, lịch sử tài khoản và các bảng xem trước giữ giới hạn 50/100 và được hiểu là “gần nhất”, không phải danh sách đầy đủ.
- Nhật ký audit đã phân trang tại DB nhưng vẫn dùng 50 dòng/trang theo yêu cầu tra soát hiện hữu; hình học và nhãn phân trang dùng chuẩn chung.
- Validation nghiệp vụ đặc thù vẫn nằm trong module; quy tắc required/email/min/max và cách hiển thị lỗi đã thống nhất ở layout tương ứng.
- Không cần thay đổi schema cho đợt này.

## Xác minh

- Build Web cùng các project phụ thuộc: 0 warning / 0 error, output riêng `.build/website-consistency` để tránh khóa DLL của website đang chạy.
- Bộ test Application: 271/271 đạt.
- `node backend/tests/website-lists.browser.cjs`: dùng Chrome headless, DataTables thật trong repo, HTTP server và dữ liệu giả lập, không truy cập DB hay tài khoản.
- Browser test xác minh khôi phục URL, pageSize, icon/nhãn, giữ query khác, Xóa lọc, lỗi 500/Thử lại, phục hồi trang rỗng, pagination viewport 390px và khởi tạo script thông báo Brand/KOC.
- Kiểm tra cú pháp JS cho các script chỉnh sửa.
- Nghiệm thu end-to-end bằng Chrome headless và tài khoản Admin/Brand/KOC trên bản sao local `TopstarWebsiteReview_20260908`: booking, shipment, collaboration, dispute, billing, wallet, settlement; cả URL lọc không có kết quả và nhóm shipment đều HTTP 200, không có `pageerror`.
- Database gốc không bị thay đổi; background jobs bị tắt riêng trên host nghiệm thu.

## Gợi ý tiếp theo

Thêm test tích hợp SQL tự động cho bất biến “tổng tiền không đổi khi chuyển trang”, rồi đo query time với dữ liệu 10.000+ dòng. Sau đó tách phần chi tiết ký quỹ thành endpoint theo funding id để giảm thêm dữ liệu HTML ban đầu.
