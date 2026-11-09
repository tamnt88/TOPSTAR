# Xác minh CSS trang Dành cho Brand - 14/08/2026

## Sự cố

- Trang `/danh-cho-brand` vẫn hiển thị header nhưng toàn bộ nội dung bên dưới mất bố cục và hiệu ứng.
- Nguyên nhân: `BrandPublic.cshtml` chỉ nạp stylesheet hiệu chỉnh, thiếu stylesheet nền `brand-public.css`.

## Điều chỉnh

- Nạp `~/css/public/brand-public.css` trước.
- Giữ `~/css/public/brand-landing-journey-fix.css` ở sau để chỉ ghi đè các chi tiết cần hiệu chỉnh.
- Không gộp CSS vào Razor; CSS public tiếp tục được tách riêng.

## Kiểm tra

- Build dự án thành công.
- Website được khởi động lại tại `http://localhost:5201`.
- `/danh-cho-brand` và hai stylesheet riêng của trang đều phản hồi HTTP 200.
- Không có thay đổi cơ sở dữ liệu và không cần chạy SQL.

