# 2026-08-06 - Hoàn thiện trang Dành cho Brand

## Phạm vi

- Làm lại toàn bộ trang công khai `/danh-cho-brand` theo hành trình ra quyết định của Brand.
- Tách CSS riêng tại `wwwroot/css/public/brand-landing.css`; không dùng CSS Brand dashboard hoặc Admin.
- Sửa CTA đúng nghiệp vụ:
  - `Xem gói dịch vụ` dẫn đến `/goi-koc`.
  - `Tạo tài khoản Brand` và `Đăng ký Brand` dẫn đến form đăng ký chung với `tab=brand`.

## Nội dung và bố cục

- Hero mô tả rõ giá trị của trung tâm điều hành, kèm mô phỏng dashboard thay cho hình trang trí không liên quan.
- Bổ sung bằng chứng tin cậy, bốn nhóm lợi ích, quy trình bốn giai đoạn, quản trị/audit và CTA cuối trang.
- Chuẩn hóa toàn bộ câu chữ tiếng Việt theo ngữ cảnh hệ thống chạy thật; bỏ các từ pha tiếng Anh không cần thiết như `launching`, `tracking`, `realtime`, `targeting`.
- Giao diện responsive, không tạo thanh cuộn ngang ở thiết bị nhỏ.

## Kiểm tra

- `dotnet build backend/Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test backend/tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build`: 262/262 test thành công.
- Không thay đổi database, không có SQL cần chạy.

## Bổ sung full-bleed và chuyển động

- Gỡ khoảng đệm 16px của layout công khai riêng trên `/danh-cho-brand`; các section nay phủ đủ chiều ngang, không ảnh hưởng trang chủ hoặc Học viện KOC.
- Bổ sung hiệu ứng xuất hiện theo vùng nhìn, stagger nhẹ theo nhóm nội dung, trạng thái vận hành có nhịp báo và dashboard nổi mềm.
- Card lợi ích, quy trình, quản trị, logo đối tác và danh sách KOC có phản hồi hover rõ ràng.
- Hiệu ứng tự tắt khi thiết bị bật `prefers-reduced-motion`; nội dung vẫn hiển thị đầy đủ khi JavaScript không hoạt động.
- CSS và JavaScript được tách riêng tại `wwwroot/css/public/brand-landing.css` và `wwwroot/js/public/brand-landing.js`.
- Khối quy trình được chuyển từ danh sách dọc sang timeline ngang bốn bước có đường nối và mũi tên, giúp thể hiện một hành trình liên tục; màn hình nhỏ tự chuyển thành timeline dọc.
- Tinh chỉnh timeline thành một dải hành trình chung: bỏ bốn card rời, dùng một khung bao duy nhất, đường tiến trình xuyên qua bốn mốc và vạch phân cách nội dung nhẹ bên dưới.
- Bổ sung luồng sáng và điểm dẫn tiến độ chạy liên tục từ mốc 01 đến 04; trên thiết bị nhỏ hiệu ứng chạy theo trục dọc và tự tắt khi người dùng bật giảm chuyển động.
- Loại bỏ nền trắng của các chevron trên thanh tiến trình, chỉ giữ nét tím để đường chạy liền mạch về thị giác.

## Việc quản trị nội dung

Trang hiện đã được chuẩn hóa về giao diện và nội dung mặc định. Việc đưa toàn bộ khối Brand landing vào CMS cần một phiên bản schema/audit riêng; không ghép vào JSON trang chủ hoặc Học viện để tránh ghi đè chéo dữ liệu.

## Điều chỉnh khối minh bạch

- Không hiển thị tên cổng thanh toán kỹ thuật trên landing page; nội dung công khai chỉ dùng cách gọi `thanh toán trực tuyến`.
- Gom ba nội dung kiểm soát thành một cụm liền mạch hai cột để người dùng nhận biết đây là một hệ thống quản trị xuyên suốt.
- Viết lại câu từ theo lợi ích của Brand: điều kiện hồ sơ, phương thức thanh toán và khả năng đối soát lịch sử.
- Chuẩn hóa CTA cuối trang: chữ trắng đủ tương phản trên nền tím, nội dung và nhóm hành động cân đối, hai nút đồng nhất kích thước và responsive trên thiết bị nhỏ.

## Thử nghiệm cân bằng bảng màu công khai

- Riêng trang `/danh-cho-brand`, chuyển CTA cuối trang từ mảng tím toàn khối sang nền trắng/lavender rất nhạt; tím chỉ giữ ở đường nhấn, nút chính và trạng thái tương tác.
- Footer trên trang Brand chuyển sang xanh than trung tính, giảm quầng tím; màu tím tiếp tục làm accent cho nút, icon và tiêu đề nhóm.
- Thay đổi được scope theo `.brand-landing`, không làm đổi bảng màu của trang chủ, Học viện KOC hoặc các dashboard KOC/Brand/Admin.
- Mục tiêu tỷ lệ thị giác: phần lớn diện tích là nền trung tính, màu tím dùng có chủ đích để dẫn hướng hành động và giữ nhận diện TopStar.
## Khắc phục mất stylesheet ngày 06/08/2026

- Nguyên nhân: stylesheet riêng của `/danh-cho-brand` được khai báo trực tiếp trong phần thân Razor nên không bảo đảm được đưa vào `<head>` theo layout công khai.
- Đã chuyển `brand-landing.css` vào `@section Styles`; đồng thời chuẩn hóa cùng lỗi cấu trúc tại trang Học viện KOC và trang Đăng ký.
- `_Layout.cshtml` phải render `Styles` trong `<head>`; stylesheet riêng của từng trang phải khai báo qua section này, không đặt thẻ `<link>` trong body.
- Xác minh runtime: `/danh-cho-brand` HTTP 200; stylesheet fingerprint của Brand landing HTTP 200; CSS 23.500 byte, nằm trong `<head>` và có selector cho hero/quy trình.
- Build `Topstar.Web`: thành công, 0 warning, 0 error. Không phát sinh thay đổi database hoặc file SQL.
