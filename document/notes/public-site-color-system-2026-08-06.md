# 2026-08-06 - Chuẩn hóa bảng màu toàn bộ website công khai

## Phạm vi

- Áp dụng cho Trang chủ, Học viện KOC, Dành cho Brand, Gói dịch vụ, đăng ký, đăng nhập và footer.
- Không nạp vào layout KOC, Brand dashboard hoặc Admin để giữ nguyên nhận diện nghiệp vụ riêng của ba cổng.

## Quy chuẩn

- Nền trắng và xám rất nhạt là bề mặt chính.
- Tím chỉ dùng cho nhận diện TopStar, trạng thái active và hành động chính.
- Xanh ngọc dùng làm màu hỗ trợ tích cực.
- Xanh navy dùng làm điểm neo thị giác cho footer.
- CTA cuối trang dùng nền trung tính có điểm nhấn nhẹ, không tạo thêm mảng tím toàn chiều ngang.

## Triển khai

- Layout công khai gắn class `topstar-public-site`.
- Theme dùng chung nằm tại `wwwroot/css/public/public-theme.css` và được nạp sau CSS riêng của từng trang.
- Header công khai dùng nền trắng kính mờ; menu trung tính, chỉ mục đang chọn và nút chính dùng tím.
- Các CTA cuối trang của Trang chủ, Học viện KOC và Dành cho Brand được chuyển sang bề mặt sáng, giảm độ phủ tím.
- Footer dùng nền navy thống nhất và giữ tím/xanh ngọc ở vai trò điểm nhấn.
- Đã gỡ override thử nghiệm chỉ dành cho Brand để mọi trang công khai dùng cùng một hệ màu.

## Kiểm tra

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build`: 262/262 test đạt.
- HTTP 200: `/`, `/hoc-vien-koc`, `/danh-cho-brand`, `/goi-koc`, `/dang-ky`, `/dang-nhap`.
- CSS theme dùng chung `/css/public/public-theme.css`: HTTP 200.
- Website đang chạy tại `http://localhost:5201`.

## Database

- Không thay đổi database.
- Không có file SQL cần chạy.
# Điều chỉnh hệ màu theo tông quản trị (06/08/2026)

- Navy `#17384a` là màu neo cho điều hướng, quy trình và footer.
- Tím `#7448d8` chỉ dùng cho nhận diện, CTA chính và điểm nhấn; không phủ nền toàn trang.
- Vàng `#d7a937` cùng nền kem `#fff5dc` dành cho trạng thái đang chọn/active.
- Nền nội dung ưu tiên trắng và xám rất nhạt `#f7f8fa`; chữ dùng navy/ink để tăng độ chuyên nghiệp và khả năng đọc.
- Hero dùng gradient rất nhẹ; CTA cuối trang chuyển sang nền sáng. Chỉ quy trình và footer là các mảng tối để tạo nhịp thị giác.
- Header public giữ nền trắng nhằm bảo toàn logo, nhưng trạng thái active dùng cùng ngôn ngữ kem + viền vàng của Admin.

## Tinh giản Header/Footer

- Header giữ nền trắng phẳng, bóng đổ rất nhẹ; menu active chỉ đổi màu chữ/icon và có gạch tím 2px.
- Bỏ nền kem, viền khối và bóng nổi khỏi menu active để điều hướng không cạnh tranh với nội dung.
- Footer dùng một nền navy phẳng; bỏ radial gradient và đường viền tím dày.
- Giảm chiều cao, khoảng cách và cỡ tiêu đề footer; CTA tím phẳng, tiêu đề cột dùng gạch vàng nhỏ.
- Header và footer dùng cùng font Arimo với phần nội dung public.

## Hero trang chủ

- Áp bảng màu mới trực tiếp cho markup `landing-hero`, không chỉ selector cũ `home-hero`.
- Nền hero dùng trắng ấm, trắng và xám sáng; tím chỉ còn là quầng sáng rất nhẹ quanh ảnh nhân vật.
- Nhãn tin cậy dùng vàng trầm trên nền kem; tiêu đề dùng navy; mô tả dùng xám trung tính.
- CTA chính dùng navy, CTA phụ nền trắng viền trung tính; tránh đặt hai nút tím cạnh nhau.
- Card KOC có lớp phủ navy từ dưới lên, chữ trắng và tag nền trắng để bảo đảm tương phản trên mọi ảnh.
- Hiệu ứng hover ảnh chỉ phóng nhẹ, không làm dịch chuyển bố cục.
