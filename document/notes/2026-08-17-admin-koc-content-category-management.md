# Quản trị lĩnh vực nội dung KOC - 17/08/2026

## Vấn đề

- `KocCategories` là nguồn dùng chung cho hồ sơ KOC và targeting chiến dịch nhưng trước đây chỉ có `Code`, `Name`.
- Form hồ sơ KOC đọc toàn bộ bảng và tự sắp theo tên.
- Admin không có chức năng thêm, sửa, sắp xếp hoặc tạm ẩn danh mục.
- Không có lịch sử riêng cho thay đổi danh mục.

## Triển khai

- Mở rộng `KocCategories` với `SortOrder` và `IsActive`.
- Thêm `KocCategoryHistories` lưu append-only: hành động, snapshot trước/sau, lý do, người thao tác và thời gian.
- Mọi thao tác đồng thời ghi `AuditLogs` với entity `KocCategory`.
- Thêm dịch vụ Application/Infrastructure cho danh sách, thêm, sửa, tạm ẩn và hiển thị lại.
- Không xóa cứng danh mục đã được hồ sơ hoặc chiến dịch tham chiếu.
- Form KOC chỉ lấy danh mục đang hoạt động, theo `SortOrder` rồi `Name`.
- Danh sách targeting mới của Brand cũng chỉ lấy danh mục đang hoạt động. Dữ liệu targeting lịch sử vẫn giữ tên danh mục đã dùng.

## Màn Admin

- Route: `/Admin/Settings/KocCategories`.
- Menu: `Cài đặt hệ thống > Lĩnh vực nội dung KOC`.
- Danh sách dùng jQuery DataTables/AJAX, mặc định 10 dòng, tìm kiếm, sắp xếp, lọc trạng thái và phân trang.
- Có số liệu tổng danh mục, đang hiển thị và tạm ẩn.
- Thêm/sửa và đổi trạng thái dùng modal backdrop tĩnh.
- Form có `novalidate`, kiểm tra từng trường, icon lỗi trong trường, thông báo tiếng Việt bên dưới và lý do bắt buộc 10–500 ký tự.
- Tắt danh mục chỉ ngăn lựa chọn mới; dữ liệu lịch sử không bị xóa.

## Database đã chạy

Đã chạy trên `TopstarDb` theo thứ tự:

1. `database/sql/132_create_koc_category_management.sql`
2. `database/sql/133_verify_koc_category_management.sql`

Hai file an toàn khi chạy lặp lại. Migration 132 ban đầu gặp lỗi biên dịch sớm cột `SortOrder` và đã rollback toàn bộ transaction. File được sửa dùng dynamic SQL, sau đó chạy thành công. Verify xác nhận:

- Có cột `SortOrder`, `IsActive`.
- Có bảng và index lịch sử.
- 12 danh mục cũ được giữ nguyên, gán thứ tự 10–120 và đang hoạt động.
- Menu mới đã gán cho role `ADMIN`.

## Xác minh

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build --no-restore`: 268/268 đạt.
- `node --check wwwroot/js/admin/koc-categories.js`: đạt.
- Đăng nhập Admin thật và mở trang mới: HTTP 200.
- Endpoint DataTables `?handler=Data`: HTTP 200, trả 12 dòng.
- CSS/JS Admin và DataTables: HTTP 200, đúng MIME.
- Đã thao tác tạm ẩn qua handler có anti-forgery: DB chuyển `IsActive=0`, thông báo tiếng Việt đúng và có history/audit.
- Đăng nhập KOC thật, mở `/Koc/EditProfile`: danh mục tạm ẩn không còn xuất hiện.
- Đã hiển thị lại danh mục sau kiểm tra; DB trở về `IsActive=1`, không để lại trạng thái test sai.
- Website chạy tại `http://localhost:5201`, PID 14720.

## Chuẩn hóa trình bày bảng Admin

- Gộp tìm kiếm, trạng thái, số dòng và đặt lại bộ lọc vào một toolbar thống nhất phía trên bảng.
- Loại bỏ cụm tìm kiếm/số dòng mặc định của DataTables để không còn hai hàng điều khiển rời rạc.
- Cố định độ rộng và căn lề theo loại dữ liệu; cột thứ tự, lượt sử dụng, trạng thái và thao tác được căn giữa.
- Đưa biểu tượng sắp xếp vào đúng tiêu đề cột, không còn các mũi tên trông như cột giả.
- Chuẩn hóa nền tiêu đề, khoảng cách dòng, footer thông tin và phân trang; bổ sung responsive cho màn hình nhỏ.
- Kiểm tra lại bằng đăng nhập Admin thật: trang HTTP 200, endpoint DataTables HTTP 200 và trả đủ 12 dòng.
- CSS/JavaScript riêng của trang trả HTTP 200 với MIME lần lượt `text/css` và `text/javascript`.
- Thay đổi này chỉ ở Razor/CSS/JavaScript, không cần chạy thêm SQL.

## Hoàn thiện CRUD bằng trang riêng

- Nút thêm chuyển sang `/Admin/Settings/KocCategoryCreate`; không còn mở modal.
- Nút sửa chuyển sang `/Admin/Settings/KocCategoryEdit?id=...`; không còn mở modal.
- Bổ sung nút thùng rác và trang xác nhận `/Admin/Settings/KocCategoryDelete?id=...`.
- Danh mục chưa được sử dụng được xóa cứng sau khi nhập lý do 10–500 ký tự.
- Danh mục đã được hồ sơ KOC, tài khoản mạng xã hội hoặc targeting chiến dịch sử dụng bị chặn xóa và được hướng dẫn tạm ẩn.
- Lượt sử dụng trên bảng hiện tính đủ cả ba nguồn tham chiếu trên.
- Tắt sắp xếp DataTables ở danh sách này để giữ thứ tự quản trị `SortOrder` và loại bỏ hoàn toàn indicator sort lệch khỏi header.
- Thao tác xóa ghi cả `KocCategoryHistories` và `AuditLogs`; lịch sử cũ vẫn được bảo toàn sau khi bản ghi danh mục bị xóa.

### Database đã chạy

- `database/sql/134_allow_koc_category_delete_history.sql`
- File cho phép `KocCategoryId` trong lịch sử nhận `NULL`, đổi foreign key sang `ON DELETE SET NULL` và bổ sung action `Delete`.
- Đã chạy lặp lại lần hai thành công để xác nhận idempotent.

### Xác minh CRUD thực tế

- Đăng nhập Admin thật và chạy đủ tạo → mở trang sửa → cập nhật → mở trang xóa → xóa thành công một danh mục test chưa sử dụng.
- Sau kiểm tra danh sách trở lại đúng 12 danh mục, không còn dữ liệu test.
- Trang xóa của danh mục đang sử dụng trả HTTP 200 và hiển thị đúng thông báo chặn xóa.
- History `Delete` và audit `Delete` của bản ghi test đều tồn tại.
- Build thành công: 0 warning, 0 error; JavaScript syntax đạt; test 271/271 đạt.

## Thu gọn header theo chuẩn trang quản lý Brand

- Bỏ eyebrow và khung hero gradient của phần tiêu đề.
- Tiêu đề, mô tả và nút `Thêm lĩnh vực` nằm trên một hàng gọn ở desktop.
- Thu nhỏ nút thao tác, khoảng cách dọc, icon và typography của ba thẻ thống kê.
- Thẻ thống kê dùng chiều cao 84px, bo góc 12px và không dùng shadow nặng.
- Kiểm tra trực tiếp bằng Chrome ở viewport 1407px: header cao 58px, ba thẻ nằm cùng hàng và bảng bắt đầu tại khoảng 260px.
- Responsive vẫn xếp nút và các thẻ theo chiều dọc trên màn hình nhỏ.
- Build solution thành công, 0 warning, 0 error. Không thay đổi database.

## Chuẩn hóa bảng lịch sử

- Thay toolbar mặc định của DataTables bằng toolbar Admin gồm tìm kiếm, lọc hành động, số dòng và đặt lại.
- DataTables chỉ quản lý thân bảng và footer; bỏ icon sort lệch, giữ thứ tự mới nhất từ server.
- Khóa độ rộng các cột, tách rõ nội dung trước/sau và bổ sung horizontal scroll ở màn hình hẹp.
- Tách JavaScript của trang sang `wwwroot/js/admin/koc-category-history.js`, có cache-busting.
- Đã xóa 3 dòng `KocCategoryHistories` của danh mục test `crud-runtime-check` do quá trình xác minh tạo ra; còn 0 dòng test trên màn hình lịch sử.
- Không xóa Audit Log test vì `AuditLogs` là append-only và trigger database chặn sửa/xóa đúng thiết kế.
- Đăng nhập Admin thật: trang HTTP 200, render 6 lịch sử nghiệp vụ, toolbar và bộ lọc hành động tồn tại, không còn mã test.
- Build solution thành công, 0 warning, 0 error; JavaScript syntax đạt.
- Các trang con tạo, sửa, xóa và lịch sử đặt `AdminActivePath=/Admin/Settings/KocCategories`; header active đúng `Cài đặt hệ thống` và mục con `Lĩnh vực nội dung KOC`.
- Kiểm tra đăng nhập thật trên trang lịch sử và trang thêm: HTTP 200, đúng 1 menu cấp cao active và đúng mục con active.
- Chuẩn hóa vị trí hành động cuối toolbar thành nút tím `Lọc` có icon funnel trên cả danh sách danh mục và lịch sử; bỏ nút `Đặt lại` khỏi vị trí này.
- Bộ lọc được áp dụng khi bấm `Lọc`; ô tìm kiếm hỗ trợ phím Enter. Hai trang đăng nhập thật đều HTTP 200, có nút lọc và không còn markup/nút đặt lại.
- Sửa khoảng cách icon–nhãn của nút xác nhận trong modal trạng thái; Chrome xác nhận nút dùng flex và khoảng cách thực tế 7px.

## Màn hình lịch sử thay đổi

- Bổ sung nút `Lịch sử thay đổi` cạnh `Thêm lĩnh vực` trên trang danh sách.
- Route mới: `/Admin/Settings/KocCategoryHistory`.
- Hiển thị thời gian Việt Nam, lĩnh vực/mã, hành động tiếng Việt, trạng thái trước–sau, lý do và email người thao tác.
- Bao gồm đủ các action `Thêm mới`, `Cập nhật`, `Tạm ẩn`, `Hiển thị lại` và `Xóa`; lịch sử danh mục đã xóa vẫn đọc được từ snapshot.
- Danh sách dùng DataTables, tìm kiếm, sắp theo thời gian mới nhất, phân trang và style pagination Admin dùng chung.
- Đăng nhập Admin thật: trang danh sách và trang lịch sử đều HTTP 200; 9 bản ghi lịch sử hiện có được render, có đủ action tạm ẩn và xóa.
- Build solution thành công, 0 warning, 0 error. Không thay đổi database.
