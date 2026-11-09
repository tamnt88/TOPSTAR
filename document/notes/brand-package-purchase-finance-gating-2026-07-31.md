# Brand package purchase, finance and campaign gating — 2026-07-31

## Vấn đề

Catalog gói trước đây chỉ truyền package sang form chiến dịch. Hệ thống chưa có đơn mua, trạng thái thanh toán, lịch sử sử dụng và backend vẫn cho tạo draft trực tiếp. Trang `/Brand/Billing` chỉ phản ánh phí dịch vụ 15% sau khi campaign hoàn tất, không phải tiền mua gói.

## Luồng đã chuẩn hóa

1. Brand chọn gói tại `/Brand/Packages` để tạo đơn `Pending`.
2. Admin xác nhận tiền và mã giao dịch tại `/Admin/Finance/Billing`; đơn chuyển sang `Paid`, tính ngày hết hạn từ snapshot thời hạn.
3. Brand xem tiền mua gói, trạng thái, tham chiếu và chiến dịch đã dùng tại `/Brand/Billing`.
4. Chỉ đơn `Paid`, chưa hết hạn và chưa liên kết campaign mới hiện nút tạo chiến dịch.
5. Khi backend tạo draft, giao dịch serializable kiểm tra lại quyền sở hữu/trạng thái/thời hạn/phạm vi ngân sách, liên kết campaign và chuyển gói sang `Consumed`.
6. Truy cập trực tiếp `/Brand/CampaignEdit` không có purchase hợp lệ bị chuyển về catalog. Nút thêm chiến dịch trong danh sách cũng đi qua catalog.

Mọi lần tạo đơn, xác nhận thanh toán và tiêu thụ gói đều ghi lịch sử; tạo đơn/xác nhận tiền đồng thời ghi `AuditLogs`. Snapshot mã, tên, giá và thời hạn bảo toàn dữ liệu tài chính nếu Admin sửa catalog sau này.

## Database

Chạy theo thứ tự, UTF-8:

```powershell
sqlcmd -S "<SQL_SERVER>" -d TopstarDb -E -b -f 65001 -i database/sql/106_create_brand_package_purchase_ledger.sql
sqlcmd -S "<SQL_SERVER>" -d TopstarDb -E -b -f 65001 -i database/sql/107_seed_brand_package_purchase_test_dataset.sql
```

- `106_create_brand_package_purchase_ledger.sql`: schema nghiệp vụ, index, constraint, append-only history trigger và đổi tên menu Brand thành `Tài chính Brand`.
- `107_seed_brand_package_purchase_test_dataset.sql`: chỉ dành cho dev/test; liên kết các campaign seed với purchase `Consumed` và tạo một gói `SELECT_20M` trạng thái `Paid` còn hiệu lực.
- Cả hai file chạy lặp lại an toàn.

## QA

- Brand Packages: HTTP 200, hiện gói khả dụng `SEED-BRAND-AVAILABLE-001`.
- Brand Billing: HTTP 200, hiện lịch sử mua gói, tiền, trạng thái, mã tham chiếu và campaign đã dùng.
- `/Brand/CampaignEdit` không có purchase: HTTP 302 về `/brand/packages?required=True`.
- `/Brand/CampaignEdit?purchaseId=<paid-id>`: HTTP 200.
- Build: 0 warning, 0 error.
- Test: 238/238 đạt.
- Website chạy tại `http://localhost:5201`.

## Điều chỉnh trình bày sau QA

- Trang mua gói không còn trộn toàn bộ lịch sử `Consumed` vào catalog. Khu vực `Gói của Brand` chỉ hiển thị đơn đang `Pending` hoặc gói `Paid` còn có thể hành động.
- Nếu không có gói khả dụng, hiển thị empty state ngắn gọn và dẫn sang lịch sử tài chính.
- `/Brand/Billing` được tổ chức lại thành: bốn KPI tài chính, bảng lịch sử mua gói, và bảng phí dịch vụ sau chiến dịch.
- Tất cả trạng thái kỹ thuật được ánh xạ tiếng Việt có icon và màu: `Chờ thanh toán`, `Đã thanh toán`, `Đã sử dụng`, `Đã hết hạn`, `Đã hủy`.
- QA HTML: catalog không còn lịch sử campaign seed; cả catalog và tài chính không còn nhãn trạng thái tiếng Anh.

## Điều chỉnh tabs và ngôn ngữ sản phẩm

- Catalog chia thành hai tab: `Gói đang có` và `Khám phá gói`, mỗi tab có số lượng và một nhiệm vụ rõ ràng.
- Liên kết `Chiến dịch` và `Tài chính` được gom thành nhóm hành động ở header thay vì nằm rời rạc hai bên nội dung.
- Không hiển thị package code (`ASSIGNED_*`, `SELECT_*`) hoặc order seed trên catalog. Trang tài chính chỉ giữ mã đơn vì đây là chứng từ tra soát, không hiển thị package code.
- Thay ngôn ngữ hệ thống `Admin phân công KOC` bằng ngôn ngữ dịch vụ:
  - `Topstar tuyển chọn KOC phù hợp`.
  - `Brand chủ động tuyển chọn tối đa ... KOC`.
- CTA được đổi từ `Tạo đơn mua gói` sang `Chọn gói này`, và từ `Dùng gói tạo chiến dịch` sang `Bắt đầu chiến dịch`.
- Tab gói đang có dùng card riêng, chỉ tập trung trạng thái, giá trị, hạn dùng và hành động kế tiếp.
- CSS responsive nằm riêng tại `wwwroot/css/brand/brand.css`.

## Sửa lỗi tạo đơn mua gói

- Tái hiện POST thật với anti-forgery token cho thấy SQL Server từ chối `BrandPackagePurchaseHistories` vì EF chưa biết quan hệ phụ thuộc và phát lệnh insert lịch sử trước đơn mua.
- Bổ sung đầy đủ relationship metadata cho purchase với Brand, package policy, campaign; history với purchase và actor. EF hiện tự sắp insert đơn trước lịch sử.
- Bổ sung bắt `DbUpdateException` và concurrency để trả thông báo tiếng Việt về trang Brand/Admin, không làm người dùng rơi ra trang Error chung.
- QA POST sau sửa: HTTP 302 về `/brand/packages`; số đơn tăng đúng 1; đơn mới có trạng thái `Pending` và đúng 1 bản ghi lịch sử.

## Bổ sung bước checkout trước khi tạo đơn

- Nút `Chọn gói này` không còn POST tạo đơn ngay; chuyển tới `/Brand/PackageCheckout?packageId=...`.
- Checkout hiển thị tên gói theo ngôn ngữ sản phẩm, giá, thời hạn, quyền tuyển chọn, phương thức thanh toán, tổng tiền và checkbox xác nhận điều khoản.
- Chỉ khi Brand nhấn `Xác nhận đặt mua` hợp lệ mới tạo purchase `Pending`.
- Sau khi tạo đơn, trang kết quả nhấn mạnh `Đây chưa phải trạng thái đã thanh toán`, hiển thị nội dung chuyển khoản và ba bước đối soát/kích hoạt.
- Xóa handler POST mua trực tiếp trên catalog để không thể bỏ qua bước xác nhận.
- QA: checkout GET HTTP 200; thiếu xác nhận điều khoản trả lại trang HTTP 200 và hiển thị validation tiếng Việt.

## Brand hủy đơn gói

- Mỗi đơn `Chờ thanh toán` có nút `Hủy đơn` và bước xác nhận trước khi gửi.
- Backend chỉ cho đúng Brand sở hữu đơn hủy khi trạng thái còn `Pending`.
- Đơn đã thanh toán, đã sử dụng, hết hạn hoặc đã hủy không thể đi qua handler này.
- Hủy chuyển trạng thái sang `Cancelled`, ghi `BrandPackagePurchaseHistories` và `AuditLogs`; không xóa chứng từ.
- Có xử lý concurrency nếu Admin vừa xác nhận tiền ở phiên khác.
- Modal hủy tuân thủ nguồn quy chuẩn chung `document/notes/ui-validation-standard.md`: `novalidate`, icon nằm trong trường, viền đỏ, dòng lỗi tiếng Việt không lặp icon và focus lỗi đầu tiên.
- Lỗi validation/backend của hủy đơn không hiển thị alert tổng ở đầu trang. Hệ thống giữ ID, tên gói và lý do, tự mở lại đúng modal, gắn lỗi vào textarea và focus trường lỗi.
## Điều chỉnh bố cục bảng Tài chính Brand

- Cột `Trạng thái` chỉ hiển thị trạng thái của đơn.
- Các nút `Hủy đơn` và `Tạo chiến dịch` nằm trong cột `Thao tác`.
- Nhãn `Tạo chiến dịch` luôn hiển thị trên một dòng ở màn hình desktop.
- Hàng tiêu đề và nút `Mua thêm gói` có khoảng cách riêng với khu vực thống kê bên dưới.
## Cập nhật DataTables 01/08/2026

- Trang Tài chính Brand đã load jQuery DataTables cho cả lịch sử mua gói và phí dịch vụ sau chiến dịch.
- Mỗi bảng có tìm kiếm, chọn 5/10/20/50 dòng, sắp xếp, thông tin tổng số và phân trang tiếng Việt.
- Phân trang dùng màu teal đồng bộ header Brand; JavaScript nằm riêng tại `wwwroot/js/brand/brand-billing-datatables.js`.
- Đơn mua gói ở trạng thái `Pending` phải có nút **Thanh toán ngay** tại cột Thao tác, mở lại chính đơn bằng `purchaseId`; không tạo đơn mua trùng.
- Trang hướng dẫn thanh toán của đơn chờ hiển thị trực tiếp ngân hàng, số tài khoản, chủ tài khoản, số tiền và nội dung chuyển khoản. Đơn chỉ chuyển sang `Paid` sau khi Topstar đối soát/xác nhận.
