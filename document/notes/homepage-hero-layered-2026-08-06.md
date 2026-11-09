# Banner chính nhiều lớp

- Giữ nền chủ đạo trắng/kem và navy; tím, vàng chỉ dùng làm ánh sáng và điểm nhấn để banner sang nhưng không bị tím toàn trang.
- Bổ sung vòng quỹ đạo, quầng sáng và bóng đổ phía sau ảnh nhân vật chính để tạo một “sân khấu” riêng, tăng chiều sâu thị giác.
- Dải KOC được xếp nhịp cao thấp, bo góc và đổ bóng nhiều lớp; hover nâng nhẹ card để bớt cảm giác catalogue phẳng.
- Nút hành động chính dùng chuyển sắc navy, viền sáng nhẹ và hiệu ứng nâng vừa phải; nội dung và thứ tự CTA không thay đổi.
- Chuyển động của nhân vật, quỹ đạo và quầng sáng đều chậm, tinh tế; tự tắt khi thiết bị bật `prefers-reduced-motion`.
- Có cấu hình riêng cho desktop, tablet và mobile; không thay đổi dữ liệu quản trị banner và không phát sinh SQL.

## Kiểm tra

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `http://localhost:5201/`: HTTP 200.
- `/css/public/public-theme.css`: HTTP 200 và đã phục vụ lớp CSS hero mới.
