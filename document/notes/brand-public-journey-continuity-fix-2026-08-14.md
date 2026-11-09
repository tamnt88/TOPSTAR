# Brand Public - hành trình liền mạch (2026-08-14)

- Sửa khối quy trình tại `/danh-cho-brand` thành một tuyến liên tục qua bốn giai đoạn.
- Loại bỏ nền màu rời rạc của từng bước để tránh cảm giác bốn khối tách biệt.
- Dùng một thanh tiến trình gradient chạy phía sau các mốc và một điểm sáng chuyển động dọc tuyến.
- Giữ hỗ trợ `prefers-reduced-motion` cho người dùng tắt hiệu ứng chuyển động.
- CSS hiệu chỉnh được tách riêng tại `wwwroot/css/public/brand-landing-journey-fix.css`.
- Không thay đổi cơ sở dữ liệu, không có SQL cần chạy.
