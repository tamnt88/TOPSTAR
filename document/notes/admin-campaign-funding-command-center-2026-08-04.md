# Trung tâm điều hành tài chính chiến dịch Admin

Ngày chốt thiết kế: 04/08/2026.

## Nguyên tắc trải nghiệm

- Một chức năng nghiệp vụ là một dashboard điều hành; Admin không phải đi qua nhiều trang để hoàn tất một hồ sơ.
- Card thống kê, tab hàng đợi, bộ lọc và DataTable phải đồng bộ.
- Mỗi dòng mở một modal hồ sơ lớn; đóng modal giữ nguyên tab, từ khóa, bộ lọc và trang hiện tại.
- Chỉ hiển thị hành động hợp lệ với trạng thái. Không tạo nút giả khi backend chưa có nghiệp vụ.
- Mọi hành động tài chính phải ghi ledger, audit và lịch sử trạng thái; thông báo lỗi dùng validation chuẩn, không lộ exception.

## Hàng đợi chuẩn

1. Chờ đối soát: yêu cầu chuyển khoản/OnePAY ở trạng thái Pending.
2. Đã ký quỹ: quỹ đã Paid, chiến dịch được phép bước sang thực hiện.
3. Nghiệm thu & giải ngân: hồ sơ Paid/PartiallyReleased cần xử lý kết quả hoặc đang giữ tiền.
4. Ngoại lệ: Cancelled/Refunded/PartiallyReleased và các hồ sơ cần hoàn tiền.
5. Tất cả: tra cứu toàn bộ phiên bản quỹ, gồm cả Draft.

## Modal hồ sơ

- Tổng quan: chiến dịch, Brand, ngân sách, phí, tổng phải trả và trạng thái.
- Phân bổ KOC: snapshot thù lao từng KOC, số nghiệm thu và số hoàn.
- Thanh toán/chứng từ: phương thức, mã yêu cầu, thời hạn, chứng từ Brand gửi.
- Xử lý: xác nhận chuyển khoản; OnePAY chỉ đọc và chờ IPN hợp lệ.
- Nghiệm thu/hoàn tiền: chỉ bật khi trạng thái và kết luận tranh chấp cho phép.
- Lịch sử: ledger và audit của hồ sơ.

## Quy tắc vòng đời

`Draft → Pending → Paid → PartiallyReleased/Released → Refunded hoặc đóng hồ sơ`.

- Pending quá hạn được tác vụ nền chuyển Cancelled.
- Chỉ `Paid`/`PartiallyReleased` cho phép booking, giao hàng và nộp nội dung.
- OnePAY không được Admin xác nhận thủ công.
- Nghiệm thu một phần chỉ dùng với chiến dịch hủy theo kết luận tranh chấp.
- Tổng giải ngân cộng tổng hoàn không được vượt snapshot quỹ.

## Phần cần schema/workflow bổ sung sau

### Hoàn thiện ngày 04/08/2026

- Đã bổ sung journal `CampaignFundingEvidences` lưu chứng từ do Topstar phát hành, tách biệt hoàn toàn với chứng từ Brand trên `CampaignFundings`.
- Journal chỉ cho phép thêm mới; trigger cơ sở dữ liệu chặn sửa và xóa lịch sử.
- Tab `Chứng từ` trong modal hồ sơ hiển thị riêng chứng từ Brand và chứng từ Topstar, đồng thời cho Admin tải chứng từ đối soát hoặc hoàn tiền ngay tại dashboard.
- Chứng từ gồm loại nghiệp vụ, ảnh, mã giao dịch, số tiền, ghi chú, người thao tác và thời gian; thao tác được ghi thêm vào fund ledger và audit log.
- Chuẩn hóa nhãn trạng thái `Refunded` và các sự kiện ledger, không hiển thị mã tiếng Anh ra giao diện.
- DataTables mặc định tối thiểu 10 dòng; chỉ cho chọn 10/20/50.

SQL bắt buộc chạy trước khi mở trang Admin ký quỹ sau bản cập nhật này:

`database/sql/124_create_campaign_funding_evidence_journal.sql`

### Bổ sung maker-checker

- Xác nhận ký quỹ chuyển khoản từ 50.000.000 ₫ bắt buộc hai quản trị viên khác nhau.
- Lần thao tác đầu tạo yêu cầu `Pending`; người tạo không thể tự duyệt. Quản trị viên thứ hai phải xác nhận đúng mã giao dịch và số tiền.
- Hai bước đều ghi `ApprovalRequested`/`ApprovalGranted` vào ledger và audit.
- Chạy tiếp file `database/sql/125_create_campaign_funding_maker_checker.sql` sau file 124. Script chạy lặp an toàn.

### Ngưỡng duyệt hai lớp có cấu hình

- Ngưỡng không còn cố định trong mã nguồn; Admin cấu hình tại `Tài chính > Chính sách phí chiến dịch`.

## Hoàn thiện màn hình chính sách phí chiến dịch

- Màn `/Admin/Finance/CampaignFeePolicy` được tổ chức theo dashboard thống nhất với Admin: card tổng quan, tag trạng thái và hai tab `Cấu hình chính sách`/`Lịch sử thay đổi`.
- Tiền tệ nhập có phân cách hàng nghìn nhưng vẫn gửi giá trị số nguyên về backend; kiểm tra chéo phí tối đa không được nhỏ hơn phí tối thiểu.
- Validation dùng quy chuẩn chung: viền/icon lỗi tại trường và thông báo cụ thể ngay bên dưới sau khi submit.
- Lịch sử dùng jQuery DataTables, mặc định 10 dòng, có tìm kiếm, sắp xếp và phân trang; lịch sử và snapshot cũ không bị ghi đè.
- Ngưỡng được lưu theo từng phiên bản chính sách và trong lịch sử thay đổi.
- Cơ chế maker-checker áp dụng đồng nhất cho xác nhận đủ tiền và xác nhận hoàn tiền; người tạo yêu cầu không được tự duyệt.
- Chạy tiếp file `database/sql/126_configure_campaign_funding_dual_approval.sql` sau file 125. Script chạy lặp an toàn.
- Script 126 dùng dynamic SQL khi tạo CHECK constraint để tránh lỗi SQL Server biên dịch constraint trước khi nhận cột vừa thêm; cuối transaction có bước tự xác minh đủ cột và constraint.

### Xuất dữ liệu

- Dashboard ký quỹ Admin có nút `Xuất dữ liệu`, xuất CSV UTF-8 đầy đủ các trường số tiền, trạng thái, phương thức, mã tham chiếu và thời gian để mở trực tiếp bằng Excel.

### Kiểm soát chênh lệch đối soát

- Chứng từ đối soát bắt buộc nhập số tiền thực nhận; cho phép ghi nhận cả số tiền thiếu hoặc thừa để phản ánh đúng sao kê.
- Chỉ được xác nhận đủ tiền khi chứng từ Topstar cùng mã giao dịch có số tiền bằng chính xác tổng phải thanh toán.
- Thiếu hoặc thừa tiền đều bị chặn và thông báo rõ số tiền chênh lệch; không tự động mở chiến dịch.

- Sai lệch thiếu tiền, thừa tiền, sai nội dung chuyển khoản.
- Yêu cầu Brand bổ sung/từ chối từng phiên bản chứng từ.
- Hai cấp kiểm tra/duyệt đối với giao dịch vượt hạn mức.
- Tài khoản nhận hoàn và chứng từ hoàn tiền độc lập.
- Đối soát ngân hàng tự động và xuất Excel theo phiên đối soát.

Các phần trên không được mô phỏng bằng nút giao diện khi chưa có dữ liệu, ledger và audit tương ứng.
# Rà soát chứng từ và hoàn tiền độc lập (04/08/2026)

- Chứng từ chuyển khoản của Brand được lưu theo phiên bản và Admin phải ghi nhận một trong ba kết quả: chấp nhận, yêu cầu bổ sung hoặc từ chối. Mỗi kết quả có lý do 10–500 ký tự, lưu append-only, ledger và audit.
- Admin chỉ được xác nhận đủ tiền khi phiên bản chứng từ Brand mới nhất đã được chấp nhận và có chứng từ đối soát Topstar khớp chính xác mã giao dịch, số tiền phải thu.
- Tài khoản nhận hoàn được nhập độc lập khi Brand tạo yêu cầu thanh toán, không suy diễn từ tài khoản thanh toán hay hồ sơ doanh nghiệp. Snapshot gồm ngân hàng, số tài khoản và chủ tài khoản; Admin xem được trong hồ sơ và dữ liệu xuất CSV.
- SQL cần chạy theo thứ tự: `127_create_campaign_funding_evidence_reviews.sql`, sau đó `128_add_campaign_funding_refund_account.sql`. Cả hai file an toàn khi chạy lặp lại.
- Kiểm tra tĩnh: build 0 lỗi/0 cảnh báo; 248/248 test đạt; JavaScript Brand/Admin qua `node --check`.
- Chưa được đánh dấu hoàn tất môi trường cho đến khi hai SQL trên được chạy và smoke test lại Brand/Admin trên database thật.

## Kết quả xác nhận môi trường

- Database thật đã có bảng duyệt chứng từ, trigger append-only và đủ ba cột tài khoản nhận hoàn.
- Build cuối: 0 lỗi, 0 cảnh báo; test: 248/248 đạt; JavaScript Brand/Admin hợp lệ.
- Smoke test đăng nhập thật: Brand Campaign Funding, Brand Campaign Workspace, Admin Campaign Funding và CSV export đều HTTP 200, không xuất hiện trang lỗi runtime.
- Website đã được khởi động lại tại `http://localhost:5201` bằng bản build cuối.
# Cập nhật giao diện Admin (04/08/2026)

- Đồng bộ trang **Ký quỹ chiến dịch** với nhận diện Admin tím/vàng; loại bỏ màu teal vốn thuộc giao diện Brand.
- Chuẩn hóa màu tab đang chọn, bộ lọc, tiêu đề bảng và phân trang theo header Admin.
- Chuẩn hóa nút xem hồ sơ thành nút vuông 36px, bỏ padding kế thừa và căn icon chính giữa theo cả hai trục.
## Rà soát trung tâm điều hành chiến dịch - 05/08/2026

- Màn hình `/Admin/CampaignReview?id={CampaignId}` là điểm điều hành duy nhất của một chiến dịch; mỗi tab chỉ hiển thị một nhóm nhiệm vụ và không cuộn ghép nội dung tab khác.
- Mọi truy vấn tuyển chọn, quỹ, lịch, giao nhận và cảnh báo vận hành phải được giới hạn bằng đúng `CampaignId`; handler POST phải kiểm tra lại quyền sở hữu đối tượng trước khi thay đổi.
- Tab Ký quỹ chuyển khoản phải thực hiện đủ chuỗi: Brand gửi chứng từ → Admin xét duyệt chứng từ → Admin lưu ảnh đối soát, mã giao dịch và số tiền thực nhận → xác nhận đủ tiền. Không hiển thị hành động xác nhận cuối khi thiếu một bước tiền điều kiện.
- Khi xác nhận đủ tiền thành công, service chuyển chiến dịch từ `Published` sang `InProgress`, ghi lịch sử trạng thái, ledger và audit. OnePAY chỉ được xác nhận bằng callback/IPN hợp lệ.
- Validation dùng `novalidate`, viền đỏ, icon tròn cảnh báo trong trường và thông báo có icon bên dưới; server tiếp tục kiểm tra lại độ dài, số tiền, loại tệp và quan hệ với chiến dịch.
- DataTables trong tab tuyển chọn mặc định 10 dòng, cho phép 10/20/50, có tìm kiếm, sắp xếp và phân trang; khi đổi tab phải gọi điều chỉnh cột.
- CSS/JavaScript của màn hình nằm riêng tại `wwwroot/css/admin/campaign-review.css` và `wwwroot/js/admin/campaign-review.js`.
- Kiểm tra ngày 05/08/2026: build 0 lỗi/0 cảnh báo; 262/262 tests đạt. Không phát sinh thay đổi database hoặc file SQL mới.

## Sửa tràn ngang DataTable tuyển chọn - 05/08/2026

- DataTable trong tab Tuyển chọn không được sinh thanh cuộn ngang giả trên màn hình desktop; bảng phải co đúng chiều rộng panel.
- Tắt `autoWidth` của DataTables, giữ `columns.adjust()` khi mở tab ẩn và ép các lớp bọc DataTables có `min-width: 0`, `max-width: 100%`.
- Cụm Hiển thị/Tìm kiếm có khoảng đệm an toàn, không bị cắt mép. Chỉ cho phép cuộn ngang ở màn hình nhỏ hơn 901px.
- Kiểm tra: build 0 lỗi/0 cảnh báo; 262/262 tests đạt; JavaScript hợp lệ. Không thay đổi database, không có SQL cần chạy.

- Phân trang trong DataTable dùng adapter Bootstrap 5 nên phải style trực tiếp `.dt-paging-button .page-link`: nút 38px, khoảng cách 6px, trang hiện tại màu tím Admin, nút vô hiệu hóa nền xám nhạt; không dùng màu xanh mặc định Bootstrap.
