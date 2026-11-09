# Sửa tải CSS trang Dành cho Brand

- Trang: `/danh-cho-brand`.
- Hiện tượng: layout/header vẫn có giao diện nhưng toàn bộ nội dung landing page hiển thị như HTML thô.
- Nguyên nhân: CSS riêng của landing page phụ thuộc vào section của Razor layout nên có trường hợp không được render/tải đúng.
- Điều chỉnh: thêm `brand-public-entry.css` và liên kết trực tiếp tại page; entry này nạp CSS chính cùng CSS hiệu chỉnh hành trình với phiên bản cache mới.
- Không thay đổi database, không có file SQL cần chạy.
