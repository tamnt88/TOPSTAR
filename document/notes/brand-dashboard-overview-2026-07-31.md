# Brand Dashboard - 31/07/2026

## Mục tiêu

Hoàn thiện trang tổng quan dành riêng cho Brand theo cùng quy chuẩn đã thống nhất:

- CSS/JS của Brand tách biệt với KOC và Admin.
- Dữ liệu hiển thị từ service/database, không dùng số liệu giả trong view.
- Nội dung tiếng Việt rõ ràng, trạng thái có màu và icon phù hợp.
- Ưu tiên tác vụ Brand cần xử lý thay vì trình bày các khối dữ liệu rời rạc.
- Không đặt thêm `max-width` riêng bên trong Bootstrap container.

## Đã hoàn thành

- Thiết kế lại hero thành **Trung tâm điều hành Brand**.
- Bổ sung hành động nhanh:
  - Tạo chiến dịch.
  - Quản lý chiến dịch.
  - Duyệt nội dung.
  - Quản lý shortlist KOC.
  - Lịch booking.
  - Báo cáo hiệu quả.
- Bổ sung cảnh báo hồ sơ chưa hoàn thiện và điều hướng thẳng đến trang cập nhật.
- Bổ sung 4 chỉ số tổng quan bằng dữ liệu thật:
  - Tổng chiến dịch.
  - KOC đang hợp tác.
  - Tổng lượt tiếp cận.
  - Tổng ngân sách.
- Trình bày danh sách chiến dịch gần đây với:
  - Tên chiến dịch.
  - Trạng thái tiếng Việt.
  - Số KOC.
  - Ngân sách.
  - Lượt tiếp cận.
  - Điều hướng đến chi tiết.
- Bổ sung empty state khi Brand chưa có chiến dịch.
- Hoàn thiện responsive cho tablet và mobile.
- Tăng cache version CSS Brand để trình duyệt nhận đúng giao diện mới.

## Kiểm tra

- `dotnet build backend\Topstar.sln --no-restore -v:q`
- Kết quả: build thành công, `0 warning`, `0 error`.

## Database

Không phát sinh migration hoặc SQL mới trong hạng mục Dashboard Brand này.

## Hạng mục Brand tiếp theo

Rà soát và chuẩn hóa trang quản lý chiến dịch theo quy chuẩn CRUD/DataTables, sau đó hoàn thiện luồng tạo/sửa chiến dịch và phần duyệt nội dung.
