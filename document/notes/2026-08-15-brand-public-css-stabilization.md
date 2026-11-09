# Ổn định CSS trang Dành cho Brand - 15/08/2026

## Hiện tượng và nguyên nhân

- `/danh-cho-brand` có lúc chỉ còn giao diện header, phần landing hiển thị như HTML thô.
- `_Layout.cshtml` đang render `Styles` ba lần và nạp lặp CSS Brand trên mọi trang public.
- `BrandPublic.cshtml` đồng thời nạp nhiều bản trực tiếp, qua Razor section và qua hai entry dùng `@import`.
- Các entry đều tham chiếu `brand-public.css`, nhưng file này không tồn tại nên request trả 404. Bundle đầy đủ thực tế là `brand-landing.css`.
- Thứ tự cascade, cache và Razor section vì vậy không có một nguồn có thẩm quyền ổn định.

## Điều chỉnh

- `_Layout.cshtml` chỉ render `@section Styles` đúng một lần, sau bundle public dùng chung.
- Gỡ toàn bộ CSS Brand khỏi layout dùng chung để không ảnh hưởng Trang chủ, Học viện, đăng nhập và đăng ký.
- `BrandPublic.cshtml` chỉ nạp hai asset hiện hữu qua `@section Styles`, theo đúng thứ tự:
  1. `brand-landing.css`;
  2. `brand-landing-journey-fix.css`.
- Không còn link stylesheet trong body và không còn phụ thuộc entry `@import`.
- Sửa hai CTA đăng ký Brand về route thật `/dang-ky?tab=brand`.
- Bổ sung bảo vệ responsive cho hero ở màn hình hẹp: phần tử grid được phép co, tiêu đề được ngắt an toàn và hai CTA xếp một cột.
- Giữ nguyên `brand-public-entry.css` và `brand-public-runtime.css` vì đây là file chưa rõ nguồn gốc trong worktree; chúng không còn được Razor tham chiếu và không bị xóa.

## Xác minh

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build --no-restore`: 262/262 test đạt.
- Website được chạy lại tại `http://localhost:5201` bằng bản build cuối.
- HTTP 200: `/`, `/hoc-vien-koc`, `/danh-cho-brand`, `/dang-nhap`, `/dang-ky?tab=koc`, `/dang-ky?tab=brand`.
- HTML Brand chỉ sinh một URL fingerprint cho mỗi CSS riêng của trang.
- `brand-landing.css` và `brand-landing-journey-fix.css`: HTTP 200, MIME `text/css`.
- Toàn bộ CSS, JavaScript, ảnh và route CTA nội bộ xuất hiện trong HTML `/danh-cho-brand`: HTTP 200.
- `node --check wwwroot/js/public/brand-landing.js`: đạt.
- Đã kiểm tra ảnh chụp trình duyệt headless ở desktop 1440x1000 và mobile 500x900; hero có style, CTA không tràn và dashboard mô phỏng giữ đúng bố cục responsive.

Lần build trung gian đầu tiên bị khóa bởi tiến trình `Topstar.Web` cũ PID 11792. Sau khi xác minh đúng executable trong workspace và dừng riêng tiến trình đó, build cuối đạt sạch như trên.

## Database

- Không thay đổi schema hoặc dữ liệu.
- Không có file SQL cần chạy.

## Ghi nhận ngoài phạm vi sửa CSS Brand

- Regression asset/link phát hiện nội dung mặc định của Trang chủ và Học viện còn một số URL kỹ thuật cũ như `/Public/BrandPublic`, `/Public/RegisterKocStep1` và `/Public/Register?type=koc` trả 404.
- Các URL này nằm trong contract nội dung mặc định/CMS, không phát sinh từ bản sửa Brand và chưa được thay đổi trong đợt này để tránh mở rộng phạm vi khi chưa rà nguồn dữ liệu đang xuất bản.
