# Ký quỹ và thanh toán chiến dịch — 01/08/2026

## Phạm vi đã hoàn thiện

- Tách độc lập tiền mua gói KOC và tiền ký quỹ thực hiện chiến dịch.
- Brand phân bổ toàn bộ ngân sách cho danh sách KOC chính thức theo hai cách: chia đều hoặc nhập riêng từng KOC.
- Tổng phân bổ bắt buộc bằng đúng ngân sách chiến dịch; mỗi KOC phải có thù lao lớn hơn 0.
- Khi Brand gửi yêu cầu thanh toán, hệ thống chụp bất biến ngân sách, phí nền tảng, phí cổng thanh toán, bên chịu phí, thời hạn thanh toán, thời gian giữ đối soát và chính sách hoàn tiền.
- Admin cấu hình chính sách phí, xem lịch sử thay đổi và xác nhận khoản ký quỹ đã thanh toán bằng mã tham chiếu.
- Chặn tạo vận đơn, đề xuất lịch, nộp nội dung và nghiệm thu khi chiến dịch chưa ký quỹ đầy đủ.
- Khi Brand nghiệm thu hoàn tất, hệ thống tự tạo thu nhập chờ đối soát cho từng KOC theo bảng phân bổ; Admin không nhập lại số tiền.
- Chặn tạo thu nhập thủ công đối với chiến dịch đã dùng luồng ký quỹ mới.
- Sổ quỹ, lịch sử chính sách và audit được lưu; SQL có trigger bảo vệ dữ liệu append-only và bảng phân bổ sau khi chốt.

## Quy chuẩn giao diện và validation

- Brand: `/Brand/CampaignFunding` — chọn chiến dịch, chọn cách chia, nhập thù lao, xem tổng tiền và gửi yêu cầu thanh toán.
- Admin: `/Admin/Finance/CampaignFeePolicy` — cấu hình phí và xem lịch sử.
- Admin: `/Admin/Finance/CampaignFunding` — theo dõi yêu cầu và xác nhận thanh toán.
- 04/08/2026: Chuẩn hóa màn hình Admin **Ký quỹ chiến dịch** thành hai tab nhiệm vụ độc lập: **Đối soát ký quỹ** và **Nghiệm thu & hoàn tiền**. Danh sách đối soát dùng jQuery DataTables, có tìm kiếm/phân trang; trạng thái được Việt hóa hoàn toàn. Chứng từ mở trong modal xem ảnh; xác nhận chuyển khoản dùng modal và validation nội tuyến chuẩn, không dùng form chen trực tiếp trong danh sách.
- Validation hiển thị ngay tại trường theo chuẩn chung: viền đỏ, icon cảnh báo và thông báo tiếng Việt; backend kiểm tra lại toàn bộ quy tắc.
- CSS/JavaScript Brand vẫn nằm riêng trong `wwwroot/css/brand` và `wwwroot/js/brand`.

## Database

Chạy đúng thứ tự, mỗi file chỉ cần chạy một lần và đều an toàn khi chạy lại:

1. `database/sql/110_create_campaign_fee_policy.sql`
2. `database/sql/112_add_campaign_funding_payment_method.sql`
3. `database/sql/111_verify_campaign_funding.sql`

File 111 chỉ kiểm tra, không sửa dữ liệu. Kết quả hợp lệ trả về `Result = OK`; nếu thiếu thành phần sẽ dừng và liệt kê `VerificationError`.

Nếu đã gặp lỗi `Invalid column name 'PaymentMethod'` khi chạy bản script trước, chạy `database/sql/113_repair_campaign_funding_payment_method.sql`, sau đó chạy lại file 111. File 113 dùng dynamic DDL để tránh lỗi SQL Server biên dịch cột mới trong cùng batch và không xóa dữ liệu hiện hữu.

## Kiểm tra kỹ thuật

- `dotnet build backend/Topstar.sln --no-restore`: đạt, 0 warning, 0 error.
- `dotnet test backend/Topstar.sln --no-restore`: đạt 248/248 test.
- Hậu kiểm database sau repair: file 111 trả `Result = OK`; có 1 chính sách phí hoạt động, cột `PaymentMethod`, unique filtered index mã yêu cầu và đủ 4 trigger đang bật.
- Cấu hình thanh toán thực tế: chuyển khoản và OnePAY Production đều bật; Merchant ID, Access Code và protected hash đều hiện hữu; Return/IPN dùng `https://topstar.vn`.
- Kiểm tra runtime: `/` trả HTTP 200; `/Brand/CampaignFunding` khi chưa đăng nhập trả 302 về trang đăng nhập; IPN thiếu chữ ký trả `responsecode=0&desc=confirm-fail` và không cập nhật giao dịch.

## Thanh toán OnePAY

- Brand chọn `Chuyển khoản` hoặc `OnePAY` trước khi khóa yêu cầu. Phí cổng chỉ được cộng với OnePAY; phương thức thanh toán là một phần của snapshot bất biến.
- Request OnePAY dùng HMAC-SHA256, chuỗi tham số `vpc_`/`user_` sắp theo alphabet, số tiền nhân 100 và mã giao dịch duy nhất.
- Return URL và IPN đều kiểm tra chữ ký theo thời gian cố định, Merchant ID, mã đơn, số tiền và `vpc_TxnResponseCode=0`.
- IPN xử lý trong transaction Serializable và idempotent; callback lặp không tạo thêm ledger.
- Admin không được xác nhận tay đơn OnePAY. Chỉ chuyển khoản mới có nút xác nhận đối soát.
- Secret được bảo vệ bằng ASP.NET Data Protection; màn hình Admin kiểm tra Hash Code HEX 64 ký tự và yêu cầu Base URL HTTPS.
# Bổ sung vòng đời tài chính hoàn chỉnh (01/08/2026)

Đã đối chiếu lại `document/flow/Mô hình đề xuất.docx` và hoàn thiện các điểm còn thiếu ở vòng đời chính:

- Chốt shortlist chỉ xác lập KOC chính thức; chiến dịch vẫn ở trạng thái tuyển chọn cho đến khi ký quỹ đủ.
- Xác nhận chuyển khoản hoặc IPN OnePAY hợp lệ mới chuyển chiến dịch sang `InProgress`.
- Mỗi yêu cầu quỹ có `VersionNo`; yêu cầu đã hủy/hoàn được giữ bất biến và Brand lập phiên bản mới.
- Brand được hủy yêu cầu đang chờ thanh toán bằng modal, bắt buộc lý do 10–500 ký tự; yêu cầu quá hạn tự chuyển hủy khi truy cập màn hình quỹ.
- Chính sách phí có ngày giờ hiệu lực và trạng thái hoạt động; mỗi lần thay đổi tạo một bản chính sách mới, lịch sử không bị ghi đè.
- Admin nghiệm thu riêng từng KOC, nhập số tiền được hưởng và lý do; phần chênh lệch trở thành khoản được duyệt hoàn.
- Admin ghi nhận giao dịch hoàn tiền bằng số tiền, mã tham chiếu và lý do. Sổ quỹ lưu các bút toán `FundingRequested`, `Paid`, `Cancelled`, `Expired`, `EarningCreated`, `RefundApproved`, `RefundPaid`.
- DB chống trùng phiên bản, chống đồng thời nhiều quỹ hoạt động trên cùng chiến dịch, chống tạo thu nhập trùng và giữ snapshot/phân bổ bất biến.

SQL cần chạy theo đúng thứ tự sau (đều an toàn và chạy lặp lại được):

1. `database/sql/114_complete_campaign_funding_lifecycle.sql`
2. `database/sql/115_verify_campaign_funding_lifecycle.sql`

Không chạy `115` trước `114`. Kết quả cuối của `115` phải trả về `Result = OK`.

Lưu ý bản sửa sau kiểm thử SSMS: các index/check constraint phụ thuộc cột vừa thêm được tạo bằng dynamic SQL để tránh lỗi biên dịch trước của SQL Server; tên constraint được ghép vào biến SQL trước khi gọi `sp_executesql`; phần tổng hợp của file verify cũng dùng dynamic SQL nên không còn báo `Invalid column name` khi migration chưa đạt.

## Kết quả rà soát sau khi chạy migration

- `115_verify_campaign_funding_lifecycle.sql`: `Result = OK`.
- Dữ liệu tại thời điểm kiểm tra: `FundingVersions = 0`, `PendingPayments = 0`, `SettledAllocations = 0`, `RefundedAmount = 0`.
- Build solution: thành công, `0 warning`, `0 error`.
- Automated tests: `248/248` đạt.
- Website chạy tại `http://localhost:5201`.
- Kiểm tra chưa đăng nhập: trang chủ `200`; các route Brand/Admin tài chính chuyển `302` về đăng nhập; OnePAY IPN trả response có kiểm soát.
- Kiểm tra bằng tài khoản seed Admin: `/Admin/Finance/CampaignFunding` và `/Admin/Finance/CampaignFeePolicy` trả `200`, không có unhandled exception; form chính sách có thời điểm hiệu lực và trạng thái hoạt động.
- Kiểm tra bằng tài khoản seed Brand: `/Brand/CampaignFunding`, `/Brand/Campaigns`, `/Brand/Packages`, `/Brand/Billing` trả `200`, không có unhandled exception. Chiến dịch `Launch Serum Q2` hiển thị cả hai cách phân bổ “Chia đều ngân sách” và “Nhập riêng từng KOC”.
- Bổ sung precision `decimal(18,2)` cho `RefundedAmount`, `AcceptedAmount` ở EF model để tránh làm tròn/ngắt dữ liệu tiền.
# Bổ sung rà soát và dữ liệu QA (01/08/2026)

- Tranh chấp chỉ giữ khoản thu nhập của đúng `ParticipationId` liên quan, không giữ toàn bộ KOC trong cùng chiến dịch.
- Thêm tác vụ nền `CampaignFinanceBackgroundService` chạy định kỳ: tự hết hạn đơn chưa thanh toán và tự chuyển earning `Pending` sang `Available` sau số ngày chờ đã snapshot, trừ khi participation có tranh chấp đang mở.
- Nghiệm thu một phần/hoàn một phần chỉ được thao tác với chiến dịch bị hủy theo kết luận tranh chấp. Luồng hoàn tất bình thường giải ngân đúng snapshot phân bổ, Admin không được nhập lại tùy ý.
- `116_harden_campaign_funding_invariants.sql` chặn gửi thanh toán nếu tổng phân bổ khác ngân sách chiến dịch.
- `117_seed_campaign_funding_test_dataset.sql` tạo 6 chiến dịch QA riêng (`[QA Quỹ]`, mã đơn `SEED-CF-*`), mỗi chiến dịch 2 KOC; có đủ Draft, Pending qua chuyển khoản, Pending qua OnePay, Paid chia đều, Paid tùy chỉnh và Released.
- `118_verify_campaign_funding_test_dataset.sql` kiểm tra số lượng seed và tổng phân bổ. Các file đều an toàn khi chạy lặp lại và không ghi đè đơn seed sau khi người kiểm thử đã thao tác.
- DB website local lấy từ `backend/src/Topstar.Web/appsettings.json` là instance `DESKTOP-L6JINQQ`, không phải instance của API `SHENLUNG\\SQLSERVER2022`.
- Brand chọn `Chia đều ngân sách` hoặc `Thỏa thuận riêng từng KOC` ngay khi tạo/sửa chiến dịch; lựa chọn được lưu ở `Campaigns.AllocationMode` bởi `119_add_campaign_allocation_preference.sql`.
- Brand có thể thay KOC đã xác nhận khi chiến dịch vẫn `Published` và quỹ chưa chốt. Modal bắt buộc lý do 10–500 ký tự; participation cũ chuyển `Removed`, application trở lại shortlist, danh sách được mở lại và audit/history được lưu.
- Nếu quỹ đang `Pending`, Brand phải hủy đơn trước khi thay KOC. Nếu quỹ đã `Paid`, việc loại/thay và hoàn tiền phải đi qua tranh chấp/Admin.
- `120_allow_draft_allocation_rebuild.sql` chỉ cho phép xóa/lập lại allocation khi funding còn `Draft`; snapshot đã chốt tiếp tục bất biến.
- Dropdown **Chiến dịch** của Brand hiển thị trực tiếp **Thanh toán chiến dịch** (`/Brand/CampaignFunding`) để Brand truy cập bảng phân bổ và ký quỹ, thay vì chỉ tồn tại quyền/menu trong database.
- Nếu seed được nạp bằng `sqlcmd`, dùng `-f 65001`; file `121_repair_campaign_funding_seed_unicode.sql` sửa riêng tên 6 chiến dịch `CF0000...` đã bị nạp sai encoding, không chạm dữ liệu thật.
# Bổ sung số lượng KOC mong muốn (01/08/2026)

- Form tạo/sửa chiến dịch có trường `Số lượng KOC mong muốn`, bẫy lỗi chuẩn từ 1 đến 100.
- Dữ liệu được lưu tại `Campaigns.DesiredKocCount` để dùng xuyên suốt bước shortlist, phân bổ thù lao và quỹ chiến dịch.
- Bố cục hai lựa chọn phân bổ thù lao được chia đều toàn chiều ngang; tăng khoảng cách tiêu đề với phần form/tab.
- SQL cần chạy: `database/sql/122_add_campaign_desired_koc_count.sql` (an toàn, chạy lặp lại được).
- Rà soát bổ sung: quyền ngân sách, quyền Brand tự chọn và quota KOC được snapshot trên từng đơn mua gói; Admin đổi cấu hình sau này không làm thay đổi quyền của gói Brand đã thanh toán. Chạy tiếp `database/sql/123_snapshot_package_campaign_rights.sql`.
- Admin shortlist hiển thị số KOC chiến dịch cần, số đã gửi và số còn thiếu. Gói Topstar tuyển chọn cho phép Admin phân công trực tiếp từ hồ sơ KOC đã duyệt; bắt buộc bổ sung đúng phần còn thiếu trước khi gửi Brand. Gói Brand tự chọn phải gửi ít nhất đủ số hồ sơ để Brand có thể chọn đủ KOC.
- Sửa lỗi phân công trực tiếp vi phạm CHECK `ShortlistItems.SelectionSource`: sử dụng giá trị hợp lệ `Manual`, đồng thời bắt `DbUpdateException` và trả thông báo nghiệp vụ thay vì lộ trang lỗi 500.
- Danh sách phân công trực tiếp được đưa vào modal: bảng KOC có tìm kiếm, bộ đếm số đã chọn, giới hạn đúng số còn thiếu và validation tại chỗ; trang chính chỉ giữ một khối hành động gọn.
- Brand được từ chối từng KOC do Topstar phân công trước khi chốt shortlist. Modal bắt buộc lý do 10–500 ký tự; application chuyển `NotSelected`, history/audit giữ nguyên lý do, hồ sơ không còn nằm trong danh sách chờ xác nhận và Admin phải bổ sung người thay thế. JavaScript chi tiết shortlist được tách riêng khỏi Razor.
## Chuẩn hóa icon validation modal Brand (01/08/2026)

- Loại bỏ icon validation SVG mặc định của Bootstrap trong các trường bọc bởi `campaign-validation-field`; giao diện chỉ hiển thị một icon Font Awesome tại góc phải.
- Với `textarea`, icon lỗi được canh ở góc trên bên phải thay vì nằm giữa nội dung.
- Modal thay/từ chối KOC giữ một icon trong trường và một icon nhỏ trước thông báo lỗi theo chuẩn chung.
- Đổi biểu tượng từ chối KOC sang `circle-xmark` để tránh hình ghép `user-xmark` khó nhận biết.
- Đã build thành công `0 warning / 0 error`; kiểm thử `248/248` đạt.
## Chuẩn hóa màn hình Admin tuyển chọn KOC (01/08/2026)

- Tách danh sách theo nghiệp vụ: KOC đang gửi Brand, hồ sơ ứng tuyển có thể bổ sung và lịch sử hồ sơ Brand đã từ chối.
- Không còn hiển thị `NotSelected`, `Shortlisted`, `Confirmed` trực tiếp trên giao diện; toàn bộ trạng thái đã được Việt hóa.
- Hồ sơ Brand đã từ chối không còn checkbox và không thể được gửi lại nhầm trong danh sách hiện tại.
- Cả luồng chọn hồ sơ ứng tuyển và phân công trực tiếp đều giới hạn, kiểm tra đúng số KOC còn thiếu trước khi gửi.
- Modal phân công trực tiếp có tìm kiếm, bộ đếm lựa chọn, bảng cố định phần đầu và thông báo lỗi theo chuẩn chung.
- Đã build `0 warning / 0 error`; kiểm thử `248/248` đạt. Không phát sinh thay đổi database.
## Sửa bộ đếm và trạng thái sau khi chốt KOC (01/08/2026)

- Bộ đếm KOC trên danh sách chiến dịch chỉ tính hồ sơ còn hiệu lực; loại `NotSelected` và `Withdrawn`.
- Trường hợp shortlist đã chốt nhưng quỹ chưa thanh toán vẫn giữ trạng thái kỹ thuật `Published`, nhưng giao diện Brand hiển thị đúng giai đoạn nghiệp vụ là `Chờ thanh toán chiến dịch`.
- Menu thao tác đổi từ `Quản lý KOC` sang `Xem KOC đã chốt` sau khi shortlist hoàn tất.
- Đã đối chiếu dữ liệu chiến dịch `Unbox bộ quà Beauty Box`: cần 3, hiệu lực 3, xác nhận 3, từ chối 1, shortlist đã chốt.
- Đã build `0 warning / 0 error`; kiểm thử `248/248` đạt. Không cần chạy SQL.
