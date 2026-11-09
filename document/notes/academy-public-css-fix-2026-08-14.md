# Khắc phục CSS trang Học viện KOC - 14/08/2026

## Hiện tượng

Trang `/hoc-vien-koc` hiển thị HTML thô, logo và nội dung mất bố cục vì bundle CSS cô lập `Topstar.Web.styles.css` trả về HTTP 500.

## Điều chỉnh

- Loại bundle CSS bị lỗi khỏi layout public.
- Nạp trực tiếp CSS dùng chung `site.css`, `footer.css`, `public-theme.css` bằng URL tĩnh có phiên bản cache.
- Nạp riêng `academy.css` cho trang Học viện bằng URL tĩnh có phiên bản cache.
- Giữ CSS public tách biệt với Brand, KOC và Admin.

## Kiểm tra

- Build `Topstar.Web.csproj`: thành công, 0 lỗi, 0 cảnh báo.
- Website đang chạy tại `http://localhost:5201`.
- `/hoc-vien-koc`: HTTP 200.
- Toàn bộ 8 stylesheet của trang trả về HTTP 200 với `Content-Type: text/css`.
- HTML không còn tham chiếu bundle `Topstar.Web.styles.css` bị lỗi.
- Không có thay đổi database và không cần chạy thêm SQL.
