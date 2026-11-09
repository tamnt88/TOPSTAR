# Ghi chú triển khai Admin Menu CRUD (TopStar)

## Mục tiêu
- Quản lý menu Admin/KOC/Brand theo dạng CRUD đầy đủ, không dùng modal.
- Tách tài nguyên giao diện Admin ra file riêng để dễ bảo trì.

## Đã làm
1. Tách CSS/JS riêng cho Admin
- CSS: `backend/src/Topstar.Web/wwwroot/css/admin/admin.css`
- JS: `backend/src/Topstar.Web/wwwroot/js/admin/admin.js`
- Đã include vào layout Admin: `Pages/Admin/_AdminLayout.cshtml`

2. CRUD Menu chuyển sang page riêng
- Danh sách: `Pages/Admin/Settings/MenuManagement.cshtml`
- Create page: `Pages/Admin/Settings/MenuCreate.cshtml`
- Edit page: `Pages/Admin/Settings/MenuEdit.cshtml`
- Delete xử lý tại list bằng submit form + confirm.

3. Quy tắc Mã menu
- Mã menu chuẩn: `MENU_[PHAMVI]_[MA]`
- `PHAMVI`: `ADMIN`, `KOC`, `BRAND`
- Trên form, user chọn dropdown `Phạm vi` + nhập `Mã menu (đuôi)`.
- Hệ thống tự ghép thành mã hoàn chỉnh.

4. Danh sách menu
- Có lọc theo Từ khóa/Phạm vi/Hiển thị.
- Phân trang kiểu số.
- Mặc định 20 item/trang.
- Bỏ text `Số trang` ở phần chọn page size.

5. Spacing vùng nội dung Admin
- Bổ sung khoảng đệm cho `.admin-main-content` để không dính header/footer.

## Đang làm / cần QA
- Verify end-to-end trên môi trường local đang chạy app (hiện file dll bị lock khi build từ CLI).
- Kiểm tra quyền role khi thao tác menu (nếu cần phân quyền chi tiết hơn theo action).
- Cân nhắc tách toàn bộ block CSS admin còn nằm trong `site.css` sang `admin.css` để sạch triệt để.

## Lưu ý kỹ thuật
- Khi app đang chạy trong Visual Studio, lệnh build CLI có thể fail do lock file `Topstar.Web.dll`/`Topstar.Web.exe`.
- Nếu cần build CLI, dừng process `Topstar.Web` trước.

## Cập nhật mới
- Đã refactor theo quy tắc kiến trúc: không truy vấn DB trong .cshtml; toàn bộ dùng service dùng chung (Application/Infrastructure).
- Xem thêm: document/notes/architecture-critical-rules.md

## Cập nhật UI Admin app bar (28/07/2026)
- Header Admin được gom thành một app bar thống nhất: logo, menu cấp 1 và thông tin tài khoản cùng một hàng trên desktop.
- Menu cấp 1 hiển thị icon phía trên nhãn; trạng thái active/hover dùng nền vàng nhạt pha tím để nổi bật nhưng vẫn đồng bộ logo Top Star.
- Nền app bar dùng gradient tím đậm sang violet, có lớp sáng nhẹ để tạo chiều sâu.
- Menu cấp 2 tiếp tục hiển thị dạng dropdown, có viền tím và trạng thái active rõ ràng.
- Responsive dưới `992px`: logo/tài khoản/nút menu nằm hàng đầu, toàn bộ cây menu mở ở hàng thứ hai; nhãn và icon chuyển về dạng ngang để dễ chạm.
- Đã sửa các chuỗi tĩnh tiếng Việt trong header về UTF-8 chuẩn: `Đăng xuất`, `Menu quản trị`, `Điều hướng quản trị`.
- Không có thay đổi database và không phát sinh SQL cần chạy cho phần tinh chỉnh UI này.
- Xác minh: build thành công, `220/220` bài test đạt.

### Tinh chỉnh sau khi kiểm tra trực quan
- Bỏ nền, viền và bóng của khối logo; dùng CSS filter chuyển artwork logo sang màu trắng trên nền gradient.
- Loại bỏ icon mũi tên khỏi các menu cấp 1 có dropdown; khả năng mở menu cấp 2 vẫn giữ nguyên khi bấm toàn bộ ô.
- Tăng khoảng cách giữa các nhóm cấp 1, thu chiều cao mỗi ô và thêm bo góc để các chức năng không còn dính thành một dải.
- Gỡ `max-width: 1060px` khỏi `.admin-dashboard-wrap` và bỏ `.container` lồng bên trong Dashboard; chiều rộng nội dung nay chỉ do `.container` chung của Admin layout quản lý.

## Thiết kế lại Admin Dashboard theo nhu cầu điều hành (28/07/2026)
- Thay bố cục lấy tác vụ đồng bộ social làm trung tâm bằng dashboard ưu tiên quyết định của Admin.
- Thứ tự thông tin mới:
  1. Tiêu đề, thời điểm dữ liệu và lối tắt tới Báo cáo/Bàn vận hành.
  2. Bốn KPI thực: Brand hoạt động, KOC hoạt động, chiến dịch đang chạy và ngân sách đang vận hành.
  3. Hàng đợi cần xử lý: chiến dịch, submission và sự cố giao nhận; mỗi thẻ dẫn thẳng tới màn hình thao tác.
  4. Phân bổ trạng thái chiến dịch và timeline hoạt động hệ thống.
  5. Giám sát đồng bộ social dạng panel thu gọn; tự mở khi có job thất bại.
- Chỉ sử dụng dữ liệu thật từ `IDashboardDataService` và `ISocialSyncOrchestrator`; không tạo KPI, tỷ lệ hay biểu đồ minh họa bằng dữ liệu giả.
- Giới hạn bảng social ở 6 job gần nhất trên Dashboard; toàn bộ công cụ đồng bộ vẫn được giữ nhưng không lấn át thông tin điều hành.
- Responsive: KPI chuyển 4 → 2 → 1 cột; hàng đợi và hai panel phân tích chuyển về một cột trên tablet/mobile.
- Xác minh sau thay đổi: build thành công, `220/220` bài test đạt.

## Khôi phục điều hướng quản lý tài khoản KOC/Brand
- Phát hiện `/Admin/Brands` đã có danh sách, tìm kiếm, lọc, xem hồ sơ, khóa/mở khóa và lịch sử tài khoản nhưng chưa được gắn vào menu hai cấp sau migration 75.
- Thêm migration `database/sql/76_expose_admin_koc_brand_account_management.sql`.
- Nhóm `Tài khoản` sau migration:
  1. `Quản lý KOC & Brand` → `/Admin/Brands`.
  2. `Hồ sơ chờ duyệt` → `/Admin/Profiles`.
  3. `Vi phạm tài khoản` → `/Admin/Violations`.
- Quy tắc dữ liệu: không hard-delete Brand/KOC vì tài khoản liên kết với chiến dịch, hợp tác, tài chính và audit. “Delete” nghiệp vụ được thay bằng khóa tài khoản, thu hồi session và lưu `UserAccountStatusHistories` + `AuditLogs`.

### Sửa lỗi truy vấn danh bạ tài khoản
- Sửa `InvalidOperationException` tại `/Admin/Brands`: EF Core không dịch được `OrderBy` sau khi project trực tiếp vào record có correlated subquery.
- Query mới thực hiện lọc, sắp xếp và `Take(150)` hoàn toàn trên SQL trước; sau đó mới map anonymous row sang `BrandDirectoryItem`/`KocRankingItem`.
- Áp dụng đồng thời cho Brand và KOC để tránh lỗi tương tự khi đổi tab hoặc kiểu sắp xếp.
- Xác minh: build thành công, `220/220` bài test đạt.

### Chuẩn hóa giao diện quản trị tài khoản thành CRUD data-grid
- Loại bỏ bố cục card/bảng xếp hạng không phù hợp ngữ cảnh quản trị.
- `/Admin/Brands` nay dùng một data-grid thống nhất với hai chế độ Brand/KOC.
- Cấu trúc theo chuẩn CRUD Admin:
  - Header và hành động chính.
  - Thanh lọc một hàng: từ khóa, trạng thái, sắp xếp, số dòng.
  - Bảng có STT, định danh tài khoản, dữ liệu nghiệp vụ, ngày tham gia, trạng thái và thao tác.
  - Phân trang và tổng kết kết quả ở cuối bảng.
- Thao tác từng dòng: xem hồ sơ, xem vi phạm, khóa/mở khóa có nhập lý do và xác nhận.
- Không hiển thị nút “Thêm tài khoản” giả. Chỉ bổ sung Create khi có service tạo đồng bộ `User + Role + Brand/KOC Profile + Activation + Audit`.
- Responsive giữ data-grid cuộn ngang trên màn hình nhỏ để không làm mất cột quản trị quan trọng.
- Xác minh: build thành công, `220/220` bài test đạt.

### Tách riêng quản lý Brand và KOC
- Route độc lập:
  - `/Admin/Accounts/Brand`: chỉ hiển thị và quản lý Brand.
  - `/Admin/Accounts/KOC`: chỉ hiển thị và quản lý KOC.
- Bỏ tab Brand/KOC trong nội dung; mỗi trang có tiêu đề, mô tả, bộ lọc và cột nghiệp vụ riêng.
- Vẫn dùng chung `IUserDirectoryService` và handler khóa/mở khóa để quy tắc trạng thái, lịch sử và audit nhất quán.
- Migration `database/sql/77_split_admin_brand_koc_account_menus.sql` tạo hai menu cấp 2 riêng:
  1. Quản lý Brand.
  2. Quản lý KOC.
  3. Hồ sơ chờ duyệt.
  4. Vi phạm tài khoản.
- Migration idempotent và đã chạy thành công trên database local.
- Xác minh: build thành công, `220/220` bài test đạt.

## AJAX/DataTables offline cho quản lý Brand/KOC
- Sử dụng DataTables `2.3.8` nhánh jQuery + Bootstrap 5; assets được lưu local tại `wwwroot/lib/datatables`, không gọi CDN khi chạy.
- Trang `/Admin/Accounts/Brand` và `/Admin/Accounts/KOC` chỉ render khung bảng; dữ liệu được tải bằng AJAX từ Razor Page handler `Data`.
- Áp dụng đúng giao thức server-side DataTables:
  - Request: `draw`, `start`, `length` và bộ lọc tùy chỉnh.
  - Response: `draw`, `recordsTotal`, `recordsFiltered`, `data`.
- `IUserDirectoryService.GetPageAsync` thực hiện tìm kiếm, lọc, sắp xếp, `Skip` và `Take` tại SQL Server; không còn tải trước tối đa 150 bản ghi.
- DataTables dùng bộ lọc bên ngoài theo layout hiện có; đổi bộ lọc/phân trang chỉ cập nhật vùng bảng, không reload toàn trang.
- Khóa/mở khóa dùng một modal/form dùng chung thay vì render một form cho mỗi dòng; business rule, history, audit và chống CSRF vẫn xử lý ở backend.
- Đã kiểm tra endpoint thật bằng phiên đăng nhập Admin:
  - Brand: HTTP 200, JSON hợp lệ.
  - KOC: HTTP 200, JSON hợp lệ và sắp xếp theo điểm hoạt động.
- Gỡ `min-width` cũ và gutter âm của DataTables Bootstrap để không xuất hiện thanh cuộn ngang; tablet/mobile ẩn dần các cột phụ, luôn giữ định danh, trạng thái và thao tác.
- Bổ sung gutter riêng cho footer DataTables: thông tin kết quả cách viền trái và phân trang cách viền phải; không dùng padding làm tăng chiều rộng wrapper.
- Footer DataTables dùng selector hàng cuối với `min-height: 70px` và `padding-bottom: 20px` để không còn dính viền dưới.
- Gutter trái/phải được đặt trực tiếp trên `.dt-layout-start` và `.dt-layout-end` của Bootstrap adapter; đặt trên `.dt-info` không đủ vì cell cha vẫn neo sát viền.
- Do Bootstrap/DataTables vẫn triệt tiêu gutter cell ở runtime, `.dt-info` được dịch trực tiếp `16px` (`12px` mobile) để bảo đảm khoảng cách hiển thị.
- Bổ sung zebra rows: dòng chẵn dùng nền tím-xám nhạt `#f8f7fc`; hover dùng `#f1edfb`.
- Pagination được dịch trực tiếp sang trái `16px` (`12px` mobile); khung DataTables có `padding-bottom: 14px` (`10px` mobile) để footer không chạm viền dưới.
- Chuyển khoảng cách đáy sang đúng lớp `.account-crud-card` (`15px`) và bỏ padding giả trong `.account-table-wrap`; thead dùng gradient tím-xám `#f4f0fc → #ece6f8` để phân cấp rõ với dữ liệu.
- Giữ khoảng cách `12px` giữa thanh filter và bảng để hai khối không dính nhau; bỏ riêng border đáy dưới pagination, giữ ba cạnh còn lại của bảng.

### Hoàn thiện thao tác CRUD tài khoản
- Add: modal Admin tạo User + Role + hồ sơ Draft qua `IAuthService`; không thay đổi phiên đăng nhập Admin; ghi AuditLog người tạo.
- Read: icon mắt mở hồ sơ và lịch sử trạng thái.
- Update: icon bút mở modal sửa tên, email, khu vực/ngành hàng; kiểm tra email trùng và ghi snapshot trước/sau vào AuditLog.
- Delete: icon thùng rác thực hiện xóa mềm bằng khóa tài khoản, thu hồi session, ghi `UserAccountStatusHistories`, AuditLog và notification; icon khôi phục xuất hiện với tài khoản đã khóa.
- Không hard-delete dữ liệu liên quan chiến dịch, hợp tác hoặc tài chính.
- Xác minh: build thành công, `220/220` bài test đạt.
- Hai hành động header `Thêm KOC/Brand` và `Hồ sơ chờ duyệt` dùng Bootstrap `btn-sm`, cao `34px` để cân đối với tiêu đề trang.
- Cột thao tác dùng một nút menu dọc duy nhất. Dropdown nhóm `Xem hồ sơ`, `Sửa thông tin`, `Xem vi phạm`; thao tác xóa mềm/khôi phục nằm cuối, có divider và màu ngữ nghĩa.
# Bổ sung quản trị bảo mật tài khoản (28/07/2026)

- Thêm trang `/Admin/Accounts/Security/{userId}` từ menu thao tác KOC/Brand.
- Chức năng: khóa/mở khóa; gửi lại email kích hoạt; gửi liên kết đặt lại mật khẩu; xác minh/bỏ xác minh email và số điện thoại; xem/thu hồi từng phiên hoặc toàn bộ phiên; ghi chú nội bộ; lịch sử đăng nhập; audit quản trị tài khoản.
- Mọi thay đổi bảo mật ghi `AuditLogs` với actor snapshot, lý do và dữ liệu trước/sau khi phù hợp.
- Ghi chú nội bộ lưu ở `UserAccountAdminNotes`, chỉ được ghi thêm. Trigger DB chặn `UPDATE` và `DELETE`.
- SQL cần chạy: `database/sql/78_create_user_account_admin_notes.sql`.

## Font chữ Admin

- Toàn bộ layout Admin (header, nội dung, footer, bảng, DataTables, form và modal) sử dụng Google Font Roboto.
- Khai báo tại `wwwroot/css/admin/admin.css`; không ảnh hưởng giao diện Public, KOC hoặc Brand.
- Font tải từ Google Fonts theo yêu cầu, fallback về `sans-serif` nếu mạng không tải được.

## Trạng thái nhanh trong danh sách tài khoản

- Tách riêng hai cột `Trạng thái tài khoản` và `Trạng thái hồ sơ`.
- Trạng thái tài khoản dùng công tắc nhanh; xác nhận lý do trong modal và gửi AJAX, DataTable chỉ tải lại trang hiện tại.
- Khóa tài khoản vẫn là giao dịch nguyên tử: đổi trạng thái User/Profile, thu hồi session, ghi lịch sử, audit và notification.
- Trạng thái hồ sơ là workflow nhiều trạng thái nên hiển thị badge và nút mở màn hình duyệt; không dùng công tắc nhị phân để tránh duyệt nhầm hồ sơ chưa đầy đủ.
- Công tắc trạng thái tài khoản không lặp lại badge chữ; nhãn trạng thái được giữ bằng tooltip và `aria-label`.
- Bổ sung icon khóa/mở khóa trong tiêu đề và các nút xác nhận của modal.
- Modal trạng thái validate lý do tại chỗ: trim khoảng trắng, bắt buộc 10–1.000 ký tự, hiển thị `invalid-feedback` cả với lỗi client và lỗi nghiệp vụ từ server.
- Form trạng thái dùng `novalidate` để không xuất hiện tooltip validation tiếng Anh mặc định của trình duyệt; toàn bộ lỗi hiển thị thống nhất bằng tiếng Việt.
- AJAX gửi anti-forgery token ở cả form data và header; phân loại lỗi 400/403/500 thành thông báo tiếng Việt rõ ràng tại ô lý do.
- Trước giao dịch khóa/mở khóa, connection bật đầy đủ SQL Server SET options bắt buộc (`QUOTED_IDENTIFIER`, `ANSI_NULLS`, `ANSI_PADDING`, `ANSI_WARNINGS`, `ARITHABORT`, `CONCAT_NULL_YIELDS_NULL`) để cập nhật bảng có filtered index/trigger ổn định.
- Khai báo trigger lịch sử của `BrandProfiles` và `KocProfiles` trong EF mapping để EF Core không phát sinh `OUTPUT` không tương thích với trigger SQL Server.
- Thay thông báo native `window.alert` trong Admin bằng hệ thống Bootstrap toast dùng chung, hỗ trợ success/error/warning/info, icon, nút đóng và tự ẩn.
- Chuẩn hóa typography Admin: nội dung cơ sở 14px; menu 14px; form/bảng 13–13.5px; metadata/status 11.5–12px. Giữ breakpoint giảm menu nhẹ để tránh tràn.
- Chuẩn hóa màn hình duyệt hồ sơ: toàn bộ status hiển thị tiếng Việt, dịch dữ liệu kỹ thuật phổ biến, button có icon, timeline lịch sử và bố cục thông tin dễ quét.
- Toàn bộ form Admin tắt native browser validation; dùng validation inline tiếng Việt dùng chung cho required, minlength, maxlength và email, focus lỗi đầu tiên và vẫn giữ kiểm tra server-side.
- Chuyển `Gói KOC cho Brand` từ nhóm Cài đặt hệ thống sang nhóm Chiến dịch; đây là cấu hình package matching/shortlist mà Brand mua theo ngân sách, không phải bán tài khoản KOC trực tiếp. Migration 79.
- Chuẩn hóa `Gói KOC cho Brand` thành CRUD: trang danh sách riêng có tìm kiếm/lọc/trạng thái; nút Thêm mở `/Admin/Settings/PackagePolicyCreate`; nút Sửa mở `/Admin/Settings/PackagePolicyEdit/{id}`. Không chỉnh sửa inline và không dùng modal.
- Gói đã phát sinh nghiệp vụ không hard-delete; Admin ngừng áp dụng tại trang Sửa. Mọi lần thêm/sửa đều ghi history và audit, đồng thời kiểm tra tính liên tục của các khoảng ngân sách đang hoạt động.
- Danh sách gói KOC dùng jQuery DataTables 2.3.8 offline, tải JSON bằng AJAX từ handler `?handler=Data`; tìm kiếm, lọc trạng thái, đổi số dòng và phân trang không tải lại toàn trang.
- Menu thao tác mỗi dòng gồm Sửa và Xóa. Xóa là soft-delete qua AJAX, bắt buộc lý do 10–1.000 ký tự, ghi history + audit và vẫn bảo toàn mã gói trong chiến dịch cũ. Migration cần chạy: `database/sql/80_add_campaign_package_policy_soft_delete.sql`.
- Nâng gói KOC thành catalog thương mại cho Brand: bổ sung giá bán, mô tả, quyền lợi nhiều dòng, thời hạn sử dụng, gói nổi bật và thứ tự hiển thị. Tách khu vực “Thông tin bán hàng” khỏi “Quy tắc campaign/matching” để không nhầm giá gói với ngân sách campaign.
- Trang public `/goi-koc` hiển thị đúng giá bán, mô tả, quyền lợi và thời hạn. Migration cần chạy: `database/sql/81_add_commercial_package_details.sql`; mọi metadata thương mại cũng được snapshot trong bảng history.
- Form Thêm/Sửa gói chia ba tab: Thông tin bán hàng, Campaign & Matching, Lý do & Audit. Validation inline dùng viền/icon đỏ và thông báo tiếng Việt; khi submit, tự mở tab chứa lỗi đầu tiên và focus đúng trường, không dùng native browser validation.
- Bổ sung ảnh đại diện gói: upload JPG/PNG/WEBP tối đa 5 MB, kiểm tra magic bytes, preview trước khi lưu, lưu dưới `wwwroot/uploads/package-images` và snapshot URL vào history.
- Quyền lợi dùng CKEditor 5 `48.3.1` self-hosted/offline, giao diện tiếng Việt, license GPL và toolbar giới hạn cho nội dung quyền lợi. HTML được sanitize allowlist ở server trước khi lưu/chèn bằng `Html.Raw`.
- Giá bán nhập bằng ô text số có phân cách hàng nghìn theo `vi-VN`; giá trị decimal sạch nằm ở hidden field để model binding và validation server không phụ thuộc dấu phân cách.
- Migration cần chạy: `database/sql/82_add_package_image_and_rich_benefits.sql`.
- CKEditor quyền lợi dùng toolbar mở rộng: undo/redo, heading, font family/size/color/background, highlight, bold/italic/underline/strike, sub/superscript, code/remove format, alignment, bullet/number/todo, indent, link, table, quote, code block, horizontal line, special characters, upload image, media embed và source editing. Ảnh trong editor upload qua Razor handler có anti-forgery token, không lưu base64.
- Bỏ tiêu đề/icon/mô tả lặp lại bên trong từng tab của form gói KOC; nhãn tab là nguồn định vị duy nhất, nội dung bắt đầu trực tiếp từ trường nhập liệu.
- Quyết định duyệt hồ sơ Brand/KOC bắt buộc nhập thông tin và căn cứ xử lý từ 10–1.000 ký tự cho cả Duyệt, Yêu cầu cập nhật và Từ chối. Nội dung được giữ trong hồ sơ, lịch sử trạng thái và audit. Nút quay lại và xử lý thành công đều điều hướng về đúng danh sách tài khoản theo loại hồ sơ: `/Admin/Accounts/KOC` hoặc `/Admin/Accounts/Brand`.
- Tách đúng trạng thái tài khoản và trạng thái hồ sơ: khóa/mở khóa chỉ cập nhật `Users.Status`, không ghi đè kết quả duyệt trong `BrandProfiles/KocProfiles`. Admin được điều chỉnh hoặc duyệt lại hồ sơ ở trạng thái Chờ duyệt, Cần bổ sung, Đã duyệt, Đã từ chối và trạng thái `Locked` tồn đọng từ dữ liệu cũ; mọi lần điều chỉnh tiếp tục lưu history và audit.
- Nâng trang xem/duyệt KOC thành hồ sơ 360° dành cho Admin: hero nhận diện và KPI; social theo từng kênh với followers, view trung bình, engagement và xác minh; danh mục nội dung; portfolio; gallery; thông tin cá nhân, thể trạng, kinh nghiệm, năng lực sản xuất, ngân sách và khu vực nhận job. Dữ liệu được query theo DTO có cấu trúc, không ghép thành chuỗi và không dùng dữ liệu giả; bố cục responsive 2 cột trên desktop, 1 cột trên tablet/mobile. Brand tiếp tục dùng layout hồ sơ doanh nghiệp riêng.
- Tinh chỉnh hồ sơ KOC 360° sau kiểm tra trực quan: tên KOC dùng màu trắng và text-shadow trên hero gradient; KPI follower fallback sang tổng follower thực tế của các kênh social khi trường tổng hợp của hồ sơ chưa được đồng bộ.
- Trang chi tiết/duyệt hồ sơ đặt `AdminActivePath` về đúng `/Admin/Accounts/KOC` hoặc `/Admin/Accounts/Brand`, nên menu cấp 1 “Tài khoản” và menu con tương ứng luôn active. Bổ sung dữ liệu gia đình KOC từ `KocChildren`: tình trạng hôn nhân, có con, tổng số con và từng con với tuổi/năm sinh, giới tính; không chỉ hiển thị cờ tổng hợp.
- Gallery hồ sơ KOC dùng `object-fit: contain` cho cả thumbnail và ảnh lớn để không cắt nội dung. Click thumbnail mở modal gallery tại đúng ảnh, có ảnh trước/sau, bộ đếm và dải thumbnail điều hướng; modal tuân thủ quy chuẩn Admin không đóng khi click backdrop hoặc nhấn Esc.
- Hồ sơ KOC 360° bổ sung khu “Lịch sử hoạt động KOC” theo 7 tab: Hợp tác, Ứng tuyển, Đánh giá, Tài chính, Vi phạm, Tài khoản/đăng nhập và Đồng bộ social. Mỗi tab query tối đa 20 bản ghi gần nhất từ đúng bảng nghiệp vụ, hiển thị trạng thái tiếng Việt, thời điểm, diễn giải và số tiền khi có; lịch sử trạng thái hồ sơ/audit duyệt vẫn giữ riêng phía dưới.
- Bảy tab lịch sử KOC trình bày bằng jQuery DataTables offline: tìm kiếm, sắp xếp thời gian, chọn số dòng, thông tin kết quả và phân trang. Bảng dùng zebra rows, fixed layout và không bật `scrollX`; mobile ẩn cột diễn giải phụ để không phát sinh thanh cuộn ngang.
- Thanh tab lịch sử dùng `flex-wrap` và `overflow: visible`, tự xuống dòng theo chiều rộng thay vì xuất hiện thanh cuộn/thanh mũi tên ở cạnh phải.
- Quy chuẩn toàn bộ modal Admin: backdrop `static`, `keyboard=false`; click ngoài modal hoặc nhấn Esc không làm đóng modal và mất dữ liệu.

## Bộ dữ liệu DEV/QA quy mô lớn (29/07/2026)

- Migration `83_decouple_account_and_profile_status_history.sql` nới đúng constraint lịch sử: khóa/mở tài khoản không làm thay đổi kết quả duyệt hồ sơ.
- Seed `84_seed_large_dev_activity_dataset.sql` tạo 300 KOC, 250 Brand và 300 chiến dịch bằng GUID xác định; có thể chạy lại mà không nhân đôi dữ liệu.
- Tài khoản seed dùng miền `seed.topstar.local`, mật khẩu chung `Topstar@123`; phân bố trạng thái tài khoản và hồ sơ đủ để kiểm tra lọc, duyệt, khóa/mở và phân trang DataTables.
- KOC có hồ sơ 360°: thể trạng/gia đình, social, danh mục, kinh nghiệm, năng lực sản xuất, nhu cầu job, portfolio và gallery.
- Dữ liệu hoạt động bao phủ chiến dịch, ứng tuyển, hợp tác, đánh giá, ví, vi phạm, đăng nhập, đồng bộ social, snapshot chỉ số, lịch sử trạng thái và audit của cả KOC/Brand.
- Seed chỉ `INSERT`, không xóa và không cập nhật dữ liệu nghiệp vụ hiện hữu; có transaction và rollback toàn bộ khi gặp lỗi.
- `85_verify_large_dev_activity_dataset.sql` chỉ đọc, dùng để kiểm tra số lượng, phân bố trạng thái và mẫu hồ sơ KOC/Brand sau khi seed.
- Đã sửa seed theo constraint hủy chiến dịch hiện hành: dữ liệu mẫu không tạo trạng thái `Cancelled` khi chưa có hồ sơ tranh chấp và metadata hủy hợp lệ. Truy vấn kiểm tra luôn định danh alias cho cột `Status` để không mơ hồ khi join.
- Đã chạy migration 83, seed 84 và verify 85 thành công trực tiếp trên `TopstarDb`; chạy lại seed lần hai giữ nguyên số lượng, xác nhận không nhân đôi dữ liệu.

## Chuẩn hóa hàng đợi duyệt hồ sơ (04/08/2026)

- Trang `/Admin/Profiles` dùng bảng DataTables thay cho danh sách card kéo dài.
- Bổ sung card tổng số hồ sơ, số Brand và số KOC đang chờ duyệt.
- Có tìm kiếm theo tên/email/số điện thoại, lọc loại hồ sơ, chọn 10/20/50 dòng và phân trang.
- Hành động `Xem và duyệt` dùng nút icon thống nhất kích thước với các bảng Admin khác.
- CSS và JavaScript riêng cho Admin; không dùng chung tài nguyên Brand/KOC.
- Không thay đổi database và không yêu cầu chạy SQL.
- Kiểm tra: build 0 warning/0 error, test 262/262; website chạy tại `http://localhost:5201`.

## Chuẩn hóa quản lý vi phạm Brand/KOC (04/08/2026)

- Trang `/Admin/Violations` được tổ chức thành dashboard một trang, không còn danh sách card kéo dài.
- Bổ sung thống kê tổng vi phạm, đang hiệu lực, đã thu hồi và tổng điểm phạt hiệu lực.
- DataTables hỗ trợ tìm kiếm, lọc đối tượng, mức độ, tab trạng thái, 10/20/50 dòng và phân trang.
- Việt hóa trạng thái và mức độ; hành động chi tiết dùng icon chuẩn Admin.
- Ghi nhận vi phạm và thu hồi được thực hiện trong modal, có validation nội tuyến và icon lỗi theo quy chuẩn chung.
- Chi tiết modal hiển thị căn cứ, điểm phạt, trạng thái, lịch sử và căn cứ thu hồi tại một nơi.
- CSS/JavaScript đặt riêng trong khu vực Admin; không thay đổi database, không yêu cầu SQL.
- Kiểm tra: build 0 warning/0 error, test 262/262; website chạy tại `http://localhost:5201`.
### Ngữ cảnh xem vi phạm từ danh sách tài khoản (04/08/2026)

- Hành động **Xem vi phạm** của KOC/Brand phải truyền `accountUserId`; không được mở mặc định toàn bộ ledger.
- Trang `/Admin/Violations` truy vấn giới hạn theo `UserId` ở database, hiển thị thanh ngữ cảnh gồm tên, email, loại tài khoản và đường quay lại đúng danh sách KOC/Brand.
- Người quản trị chỉ chuyển sang ledger tổng khi chủ động chọn **Xem tất cả vi phạm**.
- Tìm kiếm, thống kê, tabs trạng thái và phân trang trên màn hình chi tiết tài khoản chỉ tính dữ liệu của tài khoản đang xem.
- Không phát sinh thay đổi database/SQL cho điều chỉnh này.
### Trung tâm quản trị Brand/KOC tại danh sách tài khoản (04/08/2026)

- Danh sách tài khoản có 5 card dữ liệu thật: tổng tài khoản, đang hoạt động, chờ duyệt, đã khóa và có vi phạm hiệu lực.
- Bộ lọc tách độc lập **trạng thái hồ sơ**, **trạng thái tài khoản**, ngành hàng/khu vực và tình trạng vi phạm; DataTables vẫn phân trang server-side tối thiểu 20 dòng theo thiết kế màn hình.
- Dòng Brand hiển thị tổng chiến dịch, số đang vận hành, số hoàn tất và vi phạm hiệu lực; tên Brand mở trực tiếp trung tâm chi tiết.
- Modal trung tâm gồm các nhiệm vụ riêng: **Tổng quan – Chiến dịch – Bảo mật – Vi phạm – Lịch sử**. Dữ liệu được tải AJAX theo `UserId`, không làm mất bộ lọc hoặc trang danh sách hiện tại.
- Tổng quan cho phép mở modal sửa thông tin ngay tại chỗ; các liên kết hồ sơ/vi phạm luôn mang đúng định danh tài khoản.
- Chiến dịch hiển thị 10 chiến dịch gần nhất cùng ngân sách, số KOC cần và trạng thái ký quỹ; bảo mật dùng dữ liệu xác minh/phiên đăng nhập; lịch sử lấy audit thật.
- CSS và JavaScript tiếp tục tách riêng trong nhóm Admin. Không phát sinh thay đổi schema hay file SQL.
## Rà soát màn hình quản lý Brand/KOC - 04/08/2026

- Màn hình quản lý tài khoản dùng DataTables server-side, có thống kê tổng quan, tìm kiếm, bộ lọc tách riêng trạng thái hồ sơ/tài khoản, ngành hàng hoặc khu vực, vi phạm, sắp xếp và phân trang.
- Tên tài khoản và hành động `Trung tâm Brand/KOC` mở modal điều hành tại chỗ gồm: tổng quan, chiến dịch, bảo mật, vi phạm và lịch sử audit.
- Modal, biểu tượng và câu chữ tự đổi đúng theo ngữ cảnh Brand hoặc KOC; endpoint chi tiết không nhận chéo loại tài khoản.
- Bộ lọc vi phạm truyền Boolean chuẩn về server; trạng thái vi phạm dùng nhãn nghiệp vụ `Đang hiệu lực`, `Đã thu hồi`, `Đã xử lý`.
- Hành động khóa/mở khóa và sửa thông tin tiếp tục yêu cầu lý do, lưu audit và dùng thông báo chuẩn của Admin.
- Kiểm tra kỹ thuật: JavaScript hợp lệ; build solution không warning/error; 262/262 unit test đạt.

### Trung tâm quản trị bảo mật tài khoản

- Trang quản trị bảo mật được tổ chức thành dashboard 5 tab: Tổng quan, Phiên đăng nhập, Bảo mật, Ghi chú và Lịch sử.
- Các thao tác xác minh email/SĐT, gửi lại kích hoạt, đặt lại mật khẩu, khóa/mở khóa và thu hồi phiên đều thực hiện bằng modal; lý do bắt buộc từ 10 đến 1.000 ký tự và hiển thị lỗi inline có icon.
- Ghi chú nội bộ yêu cầu từ 10 đến 2.000 ký tự, không sửa/xóa, tiếp tục lưu lịch sử theo quy định audit.
- Phiên đăng nhập và lịch sử đăng nhập/audit được tách theo đúng nhiệm vụ, tránh trang dài và giảm nhầm thao tác.
- CSS và JavaScript nằm riêng trong `css/admin/account-security.css` và `js/admin/account-security.js`.
# Cập nhật 04/08/2026 - Trung tâm quản lý chiến dịch Admin

- Menu **Chiến dịch** không còn là danh sách thẻ chỉ hiển thị hồ sơ chờ duyệt. Đây là dashboard quản lý toàn bộ chiến dịch.
- Dashboard gồm 6 chỉ số đối soát đủ tổng số: tổng chiến dịch, bản nháp, chờ duyệt, đang hoạt động, hoàn thành và cần xử lý. Mỗi thẻ là một bộ lọc nhanh có hỗ trợ bàn phím.
- Tách 6 phạm vi ngay trên cùng màn hình: **Tất cả – Bản nháp – Chờ duyệt – Đang hoạt động – Hoàn thành – Cần xử lý**. Chờ duyệt là một hàng đợi nghiệp vụ, không phải toàn bộ chức năng quản lý.
- Danh sách dùng jQuery DataTables server-side, mặc định 10 dòng; hỗ trợ tìm theo tên chiến dịch/Brand/mã gói, lọc trạng thái, sắp xếp và phân trang.
- Trạng thái dùng tag tiếng Việt; thao tác chờ duyệt mở quy trình xem và duyệt, các trạng thái khác mở chi tiết chỉ đọc theo đúng ngữ cảnh, không hiện nhầm nút ra quyết định.
- Phạm vi **Đang hoạt động** gộp đang tuyển KOC và đang thực hiện; **Cần xử lý** gộp cần cập nhật, bị từ chối và đã hủy.
- Query Admin dùng phân trang và tổng hợp trực tiếp tại database, không tải toàn bộ dữ liệu về trình duyệt.
- Không thay đổi database và không phát sinh file SQL.
- Kiểm tra ngày 04/08/2026: build solution 0 warning/0 error, 262/262 unit test đạt; website chạy tại `http://localhost:5201`.

### Trung tâm chi tiết chiến dịch Admin

- Trang chi tiết không còn trình bày như biểu mẫu đọc thô: bổ sung banner giai đoạn hiện tại, thời gian dự kiến và 4 chỉ số ngân sách/KOC/phân bổ.
- Chiến dịch ngoài hàng đợi duyệt hiển thị nhóm thao tác theo giai đoạn: tuyển chọn KOC, ký quỹ và đối soát, vận hành; không hiện nhầm nút duyệt.
- Chiến dịch chờ duyệt vẫn giữ checklist và ba quyết định duyệt, yêu cầu cập nhật hoặc từ chối.
- Yêu cầu thực hiện, địa điểm, liên hệ, giao nhận, nội dung bàn giao và KOC mục tiêu được gom trong một hồ sơ rõ ràng.
- CSS riêng tại `css/admin/campaign-review.css`, đồng bộ tông tím Admin và responsive trên màn hình nhỏ.
## 2026-08-05 - Trung tâm điều hành một chiến dịch trên một màn hình

- Trang `/Admin/CampaignReview?id={campaignId}` là màn hình điều hành chính của một chiến dịch; không dùng các nút điều hướng rời rạc sang danh sách tuyển chọn, ký quỹ hoặc vận hành tổng.
- Mỗi tab là một nhiệm vụ độc lập và chỉ panel đang chọn được hiển thị: `Tổng quan`, `Duyệt hồ sơ`, `Tuyển chọn`, `Ký quỹ`, `Vận hành`, `Lịch sử`.
- Mọi dữ liệu phải được giới hạn theo `CampaignId`. Sau POST phải quay lại đúng chiến dịch và đúng tab bằng tham số `tab`, không quay về danh sách tổng.
- Tuyển chọn hỗ trợ gửi shortlist và bổ sung KOC trực tiếp ngay trong tab; tiến độ luôn so sánh số KOC đã xác nhận với `DesiredKocCount`.
- Tab ký quỹ hiển thị snapshot phí, phân bổ KOC, chứng từ Brand và cho phép Admin xác nhận giao dịch chuyển khoản tại chỗ.
- Tab vận hành chỉ hiển thị lịch, giao nhận, nội dung quá hạn và tranh chấp của chiến dịch đang mở; không trộn dữ liệu chiến dịch khác.
- Validation phía trình duyệt dùng `novalidate`, viền đỏ, icon `fa-circle-exclamation` bên phải và thông báo lỗi có icon bên dưới; server vẫn kiểm tra lại toàn bộ đầu vào.
- CSS/JavaScript tách riêng tại `wwwroot/css/admin/campaign-review.css` và `wwwroot/js/admin/campaign-review.js`; giao diện dùng tông tím Admin.
- Build: 0 lỗi, 0 cảnh báo. Unit test: 262/262 đạt. Không thay đổi database và không cần chạy SQL.
- Route `/Admin/CampaignReview` phải kích hoạt menu cấp cao **Chiến dịch** bằng `AdminActivePath`; không để người dùng mất định vị khi đi từ danh sách vào trung tâm điều hành.
- Giai đoạn hiển thị không được lấy riêng `Campaign.Status`: phải kết hợp trạng thái duyệt, số KOC cần/đã chốt và trạng thái ký quỹ. Khi đã chốt đủ KOC nhưng quỹ còn Draft/chưa có, nhiệm vụ kế tiếp là **Cần lập yêu cầu ký quỹ**, không còn là **Đang tuyển KOC**.
- Banner giai đoạn phải có hành động ngữ cảnh mở trực tiếp tab nhiệm vụ kế tiếp trên cùng màn hình, không điều hướng sang trang rời.

### Rà soát hoàn thiện trung tâm chiến dịch - 05/08/2026

- Tab Vận hành lấy dữ liệu bằng `GetCampaignDashboardAsync(CampaignId)`; lịch, giao nhận, submission quá hạn, SLA duyệt nội dung và tranh chấp được giới hạn theo khóa chiến dịch, tuyệt đối không lọc theo tên/tiêu đề.
- Các POST xử lý vận đơn, hủy lịch và tiếp nhận cảnh báo kiểm tra lại quan hệ với chiến dịch đang mở trước khi gọi nghiệp vụ; không thể sửa đối tượng của chiến dịch khác bằng cách thay ID trong request.
- Gửi shortlist và phân công trực tiếp chỉ được phép khi chiến dịch ở trạng thái `Published`. Giao diện khóa form ngoài giai đoạn tuyển KOC và backend tiếp tục kiểm tra độc lập.
- Bảng hồ sơ tuyển chọn dùng jQuery DataTables, mặc định 10 dòng, có tìm kiếm, chọn 10/20/50 dòng và phân trang tiếng Việt. Khi mở tab ẩn, DataTables tự cân lại cột.
- Lịch sử chiến dịch lấy đầy đủ `CampaignStatusHistories` theo `CampaignId`, sắp xếp mới nhất trước; không chỉ hiển thị trạng thái snapshot hiện tại.
- Asset CSS/JavaScript vẫn tách riêng cho Admin; validation giữ `novalidate`, lỗi inline và không dùng popup trình duyệt.
- Kiểm tra kỹ thuật: build `0 warning / 0 error`, JavaScript hợp lệ, `262/262` unit test đạt. Website chạy tại `http://localhost:5201` và phản hồi HTTP 302 đúng về đăng nhập khi chưa có phiên.
- Không thay đổi database trong đợt rà soát này; không có file SQL mới cần chạy.
