# Admin - bảng ký quỹ chiến dịch (04/08/2026)

## Phạm vi sửa

- Trang `/Admin/Finance/CampaignFunding` sử dụng đúng jQuery DataTables 2.3.8.
- Hai tab `Đối soát ký quỹ` và `Nghiệm thu & hoàn tiền` hoạt động độc lập, không cuộn/gộp nội dung.
- Bộ lọc gồm tìm kiếm tức thời, trạng thái và số dòng; tất cả cùng điều khiển một DataTable.
- Chuẩn vận hành hiện tại: mặc định tối thiểu 10 dòng/trang; cho phép đổi 10/20/50 dòng. Cấu hình 5 dòng trước đây chỉ phục vụ QA và đã bị loại bỏ.
- Giữ riêng JavaScript Admin tại `wwwroot/js/admin/campaign-funding.js`.
- Static assets DataTables và JavaScript trang có version fingerprint để tránh cache bản cũ.

## Kiểm tra

- `dotnet build backend/Topstar.sln --no-restore`: đạt, 0 warning, 0 error.
- `dotnet test backend/tests/Topstar.Application.Tests/Topstar.Application.Tests.csproj --no-build --no-restore`: 248/248 đạt.
- `node --check backend/src/Topstar.Web/wwwroot/js/admin/campaign-funding.js`: đạt.
- Website chạy tại `http://localhost:5201`.

Không có thay đổi database và không cần chạy SQL.
