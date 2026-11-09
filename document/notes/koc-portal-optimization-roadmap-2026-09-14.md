# Roadmap tối ưu KOC Portal

Ngày bắt đầu: 14/09/2026  
Nguyên tắc: xử lý tuần tự, hoàn tất và kiểm thử từng nhóm trước khi chuyển nhóm tiếp theo.

## Giai đoạn 1 — Nền giao diện dùng chung

- [x] Rà soát phạm vi trang, CSS, JavaScript và truy vấn KOC.
- [x] Tạo bộ dịch trạng thái tiếng Việt dùng chung cho Razor Pages.
- [x] Chuẩn hóa nhãn trạng thái Razor qua một bộ dùng chung; các map JS chỉ giữ class/icon trình bày.
- [x] Chuẩn hóa typography, card surface, empty state và nền trình bày modal/form trên toàn KOC.
- [x] Tối ưu điều hướng mobile: đưa “Cơ hội” vào bottom navigation, chuyển Thông báo lên header có badge.
- [x] Chuẩn hóa DataTables: table trên desktop và card có nhãn cột trên mobile.
- [x] Bổ sung focus-visible, aria-live cho phản hồi động, nhãn nút đóng và reduced-motion.

## Giai đoạn 2 — Dashboard và cơ hội chiến dịch

- [x] Bổ sung “Ưu tiên tiếp theo” và chọn tự động hành động quan trọng nhất trên Dashboard.
- [x] Thêm deadline chi tiết và mức độ khẩn cấp cho từng tác vụ trong danh sách.
- [x] Tối ưu trang Cơ hội: hiển thị quy mô tuyển, deliverable, deadline và cảnh báo sắp hết hạn từ dữ liệu thật.
- [x] Bổ sung lý do phù hợp/chưa phù hợp và trạng thái hồ sơ ứng tuyển rõ ràng.
- [x] Tối ưu truy vấn Dashboard, cache menu và badge thông báo.

## Giai đoạn 3 — Hồ sơ KOC

- [x] Chia form hồ sơ thành các section/bước dễ hoàn thành.
- [x] Lưu riêng từng section, hiển thị thời điểm lưu và cảnh báo thay đổi chưa lưu.
- [x] Tách các PageModel hồ sơ lớn thành service nghiệp vụ.
  - [x] `EditProfileModel`: read snapshot, lưu từng section và file storage.
  - [x] `ProfileModel`: dữ liệu trang xem hồ sơ và kết nối nền tảng.
- [x] Bao transaction cho cập nhật nhiều bảng.
- [x] Dọn file upload nếu lưu database thất bại.
- [x] Nén ảnh lớn phía trình duyệt, preview ảnh/video/tài liệu và hiển thị tiến độ upload.

## Giai đoạn 4 — Timeline vận hành

- [x] Thống nhất hành trình: được chọn → ký quỹ → nhận hàng → booking → nội dung → duyệt → đăng bài → đối soát.
- [x] Liên kết Tasks, Bookings, Shipments và Collaborations bằng timeline chung.
- [x] Làm nổi bật bước đang chờ KOC và bước đang chờ Brand/Admin.
- [x] Tối ưu luồng báo sự cố, bằng chứng và tranh chấp.

## Giai đoạn 5 — Ví, thông báo và học viện

- [x] Tách rõ số dư khả dụng, chờ đối soát và đang xử lý.
- [x] Hiển thị phí, thực nhận và thời gian dự kiến trước khi rút tiền.
- [x] Đọc thời hạn OTP từ cấu hình thay vì nội dung cố định.
- [x] Phân nhóm thông báo và đưa CTA trực tiếp vào thông báo cần hành động.
- [x] Tối ưu “Tiếp tục học”, bài tiếp theo, tiến độ và chứng nhận.

## Giai đoạn 6 — Kỹ thuật và kiểm thử

- [x] Tách `koc.css` và JavaScript theo module.
- [x] Chuẩn hóa CancellationToken, ownership và phân quyền endpoint AJAX.
- [x] Thêm optimistic concurrency cho hồ sơ.
- [x] Bổ sung audit cho payout account, social account và dữ liệu định danh.
- [x] Thêm test responsive, accessibility, phân quyền và các luồng nghiệp vụ chính.

## Nhật ký

- 14/09/2026: hoàn tất audit ban đầu; bắt đầu Giai đoạn 1 bằng bộ nhãn trạng thái dùng chung.
- 14/09/2026: cập nhật bottom navigation KOC và notification badge trên header.
- 14/09/2026: hợp nhất nhãn trạng thái và bổ sung cơ chế table-to-card dùng chung cho danh sách KOC.
- 14/09/2026: hoàn tất nền giao diện và accessibility Giai đoạn 1; sẵn sàng chuyển sang Dashboard.
- 14/09/2026: bắt đầu Giai đoạn 2; Dashboard có CTA ưu tiên duy nhất theo trạng thái thực tế của KOC.
- 14/09/2026: bổ sung mức độ khẩn cấp cho đầu việc và dữ liệu ra quyết định trên card Cơ hội, không suy diễn thù lao KOC từ tổng ngân sách chiến dịch.
- 14/09/2026: đối chiếu targeting theo hồ sơ KOC ngay trên danh sách Cơ hội; tách rõ kết quả phù hợp với trạng thái đăng ký.
- 14/09/2026: hoàn tất Giai đoạn 2; hợp nhất truy vấn tài khoản Dashboard, cache menu theo vai trò có cơ chế xóa cache và tái sử dụng badge thông báo đã tải.
- 14/09/2026: bắt đầu Giai đoạn 3; gia cố lưu hồ sơ theo section bằng transaction, kiểm tra upload, dọn file lỗi và cảnh báo thay đổi chưa lưu.
- 14/09/2026: tối ưu trang xem hồ sơ: bỏ chỉ số mẫu 98%/4.9 không có nguồn dữ liệu, hợp nhất truy vấn media và bổ sung cancellation token.
- 14/09/2026: bổ sung preview file tại trình duyệt, dung lượng từng file và tiến độ upload hồ sơ.
- 14/09/2026: hoàn tất pipeline upload hồ sơ: ảnh avatar/thư viện lớn được thu về tối đa 1920px và WebP khi dung lượng giảm; giữ file gốc nếu tối ưu không có lợi.
- 14/09/2026: bắt đầu tách PageModel hồ sơ; chuyển tạo tên, ghi và xóa upload KOC sang `IFileStorageService`, áp chính sách thư mục/đuôi file/dung lượng tập trung và kiểm tra đường dẫn khi xóa.
- 14/09/2026: gia cố upload bằng magic bytes cho ảnh, PDF/Word và video; file giả mạo phần mở rộng bị từ chối tại service.
- 14/09/2026: tách nghiệp vụ lưu kênh mạng xã hội và lĩnh vực KOC sang `IKocProfileSectionService`; tiếp tục dùng chung transaction của luồng lưu hồ sơ.
- 14/09/2026: tách lưu thể trạng, con cái và ngân sách mong muốn sang `IKocProfileSectionService`; bổ sung giới hạn server-side cho số đo, tuổi con và ngân sách âm.
- 14/09/2026: tách lưu/sắp xếp/ẩn thư viện ảnh và video sang `IKocProfileSectionService`; loại bỏ truy vấn từng media khi đổi thứ tự bằng một truy vấn theo section.
- 14/09/2026: tách cập nhật tài khoản, địa chỉ, trạng thái duyệt, avatar và CV của section cơ bản sang `IKocProfileSectionService`; truy vấn tài khoản/hồ sơ khi lưu được hợp nhất.
- 14/09/2026: hoàn tất tách read/write của `EditProfileModel`; dữ liệu form được tải qua `KocProfileEditSnapshot`, media chỉ tải một lượt rồi phân nhóm trong service.
- 14/09/2026: hoàn tất `IKocProfileViewService`; `ProfileModel` không còn tự tải tài khoản, địa chỉ, social, portfolio, thể trạng, media và kết nối nền tảng. Hoàn tất hạng mục tách hai PageModel hồ sơ.
- 14/09/2026: hoàn tất Giai đoạn 3 — hồ sơ KOC đã tách read/write service, lưu độc lập theo section trong transaction và dùng chung pipeline upload có kiểm tra nội dung, dọn file lỗi, preview, nén ảnh và hiển thị tiến độ.
- 14/09/2026: bắt đầu Giai đoạn 4; timeline Hợp tác đã nối trạng thái tham gia, ký quỹ, giao hàng, booking, nội dung, tranh chấp và đối soát; bổ sung tóm tắt bước hiện tại, bên đang chờ và hành động kế tiếp cho KOC.
- 14/09/2026: hoàn tất Giai đoạn 4; KOC có thể đính kèm ảnh/PDF bằng chứng trực tiếp khi trao đổi tranh chấp, với giới hạn 5 MB, kiểm tra magic bytes, chống gửi đồng thời file và URL, đồng thời tự dọn file nếu lưu phản hồi thất bại.
- 14/09/2026: bắt đầu Giai đoạn 5; xác nhận Ví đã tách số dư khả dụng, đang đối soát, tạm giữ và đang chờ chuyển. Loại bỏ nội dung OTP cố định 5 phút: email, thông báo và modal nay dùng thời hạn thực tế từ `OperationalRuntimePolicy`, đồng thời giữ nguyên hạn khi nhập lại OTP lỗi.
- 14/09/2026: hoàn tất phần Ví và Thông báo của Giai đoạn 5; modal rút tiền hiển thị số tiền yêu cầu, phí, thực nhận và SLA dự kiến 1–3 ngày làm việc. Hộp thư KOC có bộ lọc Công việc/Tài chính/Hỗ trợ và CTA “Xử lý” trực tiếp cho thông báo có đích hợp lệ.
- 14/09/2026: hoàn tất Giai đoạn 5; Học viện KOC có khối “Tiếp tục lộ trình”, hiển thị khóa đang học, tiến độ thực tế, CTA đến bài chưa hoàn thành kế tiếp và thẻ chứng nhận dẫn đến trang xác minh công khai.
- 14/09/2026: sửa CSS trang Hồ sơ KOC theo phản hồi QA: khóa màu chữ tương phản trên gradient tiến độ, cân lại padding/line-height, làm rõ vòng phần trăm và trạng thái hover, đồng thời thu gọn bố cục danh sách thiếu thông tin trên mobile.
- 14/09/2026: bắt đầu Giai đoạn 6; chuẩn hóa `CancellationToken` cho các GET chính của Dashboard, Cơ hội và Đầu việc KOC, không còn đọc gián tiếp qua `HttpContext.RequestAborted` tại các luồng này.
- 14/09/2026: tiếp tục chuẩn hóa `CancellationToken` cho Hồ sơ, Ví và Đánh giá KOC; endpoint DataTables của Đánh giá và Tranh chấp nay trả HTTP 403 kèm lỗi có cấu trúc khi hồ sơ chưa đủ điều kiện, thay vì phản hồi 200 với danh sách rỗng gây hiểu nhầm.
- 14/09/2026: hoàn tất optimistic concurrency cho chỉnh sửa hồ sơ KOC; mỗi form mang phiên bản `UpdatedAtUtc`, thao tác lưu dùng conditional update trong transaction và từ chối ghi đè khi hồ sơ đã đổi ở tab/phiên khác, sau đó tải lại dữ liệu mới nhất để người dùng đối chiếu.
- 14/09/2026: hoàn tất audit dữ liệu nhạy cảm KOC; tài khoản nhận tiền tiếp tục chỉ ghi mã ngân hàng và 4 số cuối, còn cập nhật định danh, social và thông tin cá nhân ghi metadata tối thiểu (nhóm trường, nền tảng, số lượng), không đưa email, số điện thoại, địa chỉ, URL kênh, số tài khoản đầy đủ hay số đo thực tế vào payload.
- 14/09/2026: hoàn tất rà soát cancellation/ownership/phân quyền KOC; toàn bộ handler bất đồng bộ công khai nhận cancellation token hoặc dùng request-aborted tại guard dùng chung, các trang kế thừa guard `Koc` theo role, truy vấn và mutation truyền `UserId` hiện hành xuống service để kiểm tra ownership, còn endpoint DataTables bị chặn trả HTTP 403 thay vì dữ liệu rỗng giả.
- 14/09/2026: chạy regression test tầng Application: 271/271 test vượt qua, không có test bị bỏ qua. Browser test responsive/DataTables hiện chưa chạy được trên máy do môi trường chưa có Node.js; giữ hạng mục kiểm thử ở trạng thái đang thực hiện, chưa đánh dấu hoàn tất.
- 14/09/2026: hoàn tất tách tài nguyên KOC theo module; stylesheet 8.043 dòng được chia thành `koc-core`, `koc-workspaces`, `koc-logistics` và `koc-theme`, tải độc lập với cache-busting. JavaScript chỉnh sửa hồ sơ được đưa khỏi Razor sang `edit-profile.js`, song song với các module Tasks, Reviews, Wallet, Shipments, Bookings, Notifications và Disputes đã có. Đồng thời loại bỏ import Roboto bên ngoài và chuẩn hóa phần KOC về BeVietnamPro/Segoe UI/Tahoma.
- 14/09/2026: hoàn tất kiểm thử Giai đoạn 6; 271/271 unit test nghiệp vụ vượt qua và browser test headless vượt qua các tình huống khôi phục bộ lọc URL, phân trang/nhãn accessibility, lưu trạng thái, lỗi tải và thử lại, trang rỗng, responsive mobile pager cùng danh sách thông báo Brand/KOC. Node.js chỉ được dùng dạng portable trong thư mục tạm, không cài vào hệ thống.
- 14/09/2026: hoàn tất toàn bộ roadmap tối ưu KOC Portal từ Giai đoạn 1 đến Giai đoạn 6; build cuối đạt 0 warning/0 error và website tiếp tục chạy tại cổng 5201.
- 14/09/2026: hậu kiểm sau hoàn tất — loại bỏ bốn inline script tự mở modal khỏi trang Ví, chuyển hành vi sang module `wallet.js` qua data attribute; đồng thời chuẩn hóa cache-busting cho Wallet và DataTables. Toàn bộ Razor KOC không còn inline script/style thực thi, thuận lợi cho CSP và bảo trì.
- 14/09/2026: hoàn tất vòng tinh chỉnh UX hậu kiểm KOC — tăng cỡ chữ metadata quan trọng lên tối thiểu 12px; cho hồ sơ đầy đủ đang chờ duyệt xem Dashboard, Cơ hội và Học viện nhưng vẫn khóa ứng tuyển/công việc/tài chính; đồng bộ trạng thái khóa giữa menu desktop và mobile; bỏ dải thống kê trùng lặp trên Dashboard; hiển thị trạng thái kết nối nền tảng; thay drag-only bằng nút sắp xếp media dùng được với touch/bàn phím và loại bỏ jQuery UI CDN.
- 14/09/2026: kiểm thử vòng UX hậu kiểm đạt 276/276 unit test (bổ sung 5 ca cho quyền xem khi chờ duyệt), browser regression test đạt, JavaScript chỉnh sửa hồ sơ hợp lệ, build 0 warning/0 error và các asset mới trả HTTP 200.
- 14/09/2026: sửa cách trình bày kết nối mạng xã hội sau phản hồi trực quan — bỏ khối OAuth rời rạc ở đầu hồ sơ, đưa tóm tắt đồng bộ vào tab Mạng xã hội, đổi trạng thái mơ hồ “Chưa kết nối” thành “Khai báo thủ công” và “Đã kết nối” thành “Đồng bộ tự động”; chỉ hiển thị thao tác đồng bộ/ngắt khi thực sự có kết nối nền tảng.
- 15/09/2026: hoàn tất tinh chỉnh mobile KOC theo ảnh QA — menu mở dạng bảng điều hướng hai cột với icon/surface/trạng thái active rõ ràng; toolbar hồ sơ xếp dọc và CTA đủ vùng chạm; tab hồ sơ cuộn ngang không lộ scrollbar; notification DataTable chuyển thành card vừa viewport, nội dung không còn bị cắt và nhóm thao tác tự xuống dòng.
- 15/09/2026: hoàn thiện đúng cấu trúc menu mobile hai cấp — nhóm cấp 1 có chevron và trạng thái mở/đóng, nhóm đang active tự bung khi mở menu; submenu cấp 2 hiển thị thành lưới riêng bên dưới, active/disabled rõ ràng và tự chuyển một cột trên màn hình rất hẹp.
- 15/09/2026: sửa card Thông báo trên mobile — dòng nội dung chính dùng toàn chiều rộng thay vì bị bó bởi nhãn cột, tiêu đề/nội dung có không gian đọc rõ hơn; cột metadata gọn còn 74px và toàn bộ CTA, xem, lưu trữ được giữ trên cùng một hàng linh hoạt.
- 15/09/2026: điều chỉnh menu mobile theo phản hồi sử dụng — cấp 1 và submenu cấp 2 đều trình bày một mục trên mỗi hàng, tránh cắt tên và làm rõ cấu trúc accordion; giữ nguyên icon, chevron, active và disabled.
- 15/09/2026: gia cố layout Thông báo mobile sau kiểm tra thiết bị thực — vô hiệu hóa `colgroup` DataTables trên màn hình nhỏ, ép header card thành grid 40px + nội dung co giãn toàn chiều rộng và chuyển hàng thao tác thành footer toàn card để không còn tiêu đề xếp từng chữ hoặc nút lưu trữ rơi dòng.
- 15/09/2026: bổ sung regression assertion đo trực tiếp chiều rộng header card và vị trí các CTA Thông báo ở viewport 390px; đồng thời sửa fixture browser thiếu viewport meta. Test nay xác nhận nội dung chính rộng toàn card và các nút nằm cùng hàng, thay vì chỉ kiểm tra số dòng/phân trang.
- 15/09/2026: sửa trang Tranh chấp KOC theo kiểm tra trực quan — vô hiệu hóa toolbar “Lọc / Xóa lọc” do lớp DataTables dùng chung tự chèn khi trang đã có sẵn hàng bộ lọc, giữ duy nhất nút Lọc đúng vị trí phía trên danh sách và bổ sung cache-busting cho script của trang.
- 15/09/2026: sửa độ tương phản tiêu đề hero trang Tranh chấp — quy tắc tiêu đề nền sáng dùng chung không còn ghi đè màu chữ trắng của hero gradient.
- 15/09/2026: hoàn tất vòng audit KOC Portal trực tiếp bằng tài khoản test trên 12 route chính ở desktop 1440px và mobile 390px (24 lượt trang). Không phát hiện tràn ngang, ID trùng, ảnh hỏng, tiêu đề thiếu hay lỗi mã hóa tiếng Việt; toàn bộ route đúng quyền truy cập. Điểm còn cần tối ưu trước khi kết luận hoàn toàn chuẩn là vùng chạm mobile dưới 36px tại một số link/`btn-sm`, tập trung ở Dashboard, Booking, Academy và thao tác lọc của các danh sách.
- 15/09/2026: tinh gọn điều hướng theo góc nhìn KOC — nhóm menu chỉ có một chức năng con nay dẫn thẳng đến chức năng đó, không yêu cầu mở dropdown/accordion trung gian; áp dụng trực tiếp cho “Tài chính → Ví thu nhập” trên cả desktop và mobile.
- 15/09/2026: hoàn tất vòng tối ưu theo hành trình KOC — đổi “Cơ hội chiến dịch/Cơ hội” thành “Tìm việc KOC/Tìm việc”, đổi hai tab thành “Việc đang tuyển” và “Đã ứng tuyển”; chuẩn hóa nút/CTA chính trên mobile đạt vùng chạm tối thiểu 40px, bao gồm bộ lọc, thao tác danh sách, liên kết Dashboard và điều hướng lịch.
- 15/09/2026: nghiệm thu lại sau tinh chỉnh bằng audit đăng nhập thật: 24/24 lượt trang KOC trên desktop/mobile đạt, 0 lỗi route, tràn ngang, ảnh hỏng, ID trùng, mã hóa tiếng Việt, CTA không tên hoặc vùng chạm dưới 36px; bộ browser regression DataTables/Thông báo cũng đạt.
- 15/09/2026: chốt roadmap trải nghiệm KOC lấy mục tiêu kiếm việc và nhận tiền làm trung tâm: Tìm việc → Ứng tuyển → Việc cần làm → Workspace chiến dịch → Thu nhập → Uy tín. Bản hoàn thiện đầu tiên đã mở các bộ lọc ngân sách/loại nội dung vốn được backend hỗ trợ, hiển thị thu nhập ước tính trên mỗi KOC kèm tổng ngân sách và số lượt ứng tuyển, đồng thời bổ sung timeline “Đã gửi → Brand cân nhắc → Được chọn” cùng chỉ dẫn hành động cho từng trạng thái hồ sơ.
- 15/09/2026: sửa luồng menu KOC theo đúng hành trình sử dụng thay vì chỉ đổi nhãn — tách “Tìm việc” khỏi nhóm “Công việc” thành mục cấp 1 ngay sau Tổng quan; nhóm Công việc chỉ còn Đầu việc, Lịch booking, Sản phẩm và Lịch sử hợp tác; Tài chính tiếp tục là liên kết một cấp. Cấu trúc này dùng chung cho desktop và menu mobile.
- 15/09/2026: hoàn tất ba chức năng sản phẩm còn thiếu của luồng tìm việc KOC: lưu/bỏ lưu chiến dịch bằng dữ liệu database riêng; nhập mức thù lao đề xuất khi ứng tuyển và hiển thị cho Brand trong shortlist; SLA phản hồi 3 ngày làm việc hiển thị trên hồ sơ ứng tuyển, kèm cảnh báo quá hạn. Đã áp dụng script database idempotent không ảnh hưởng dữ liệu cũ, build 0 lỗi/0 cảnh báo, 276/276 unit test đạt và audit đăng nhập thật 24/24 lượt trang desktop/mobile đạt.

## Biên bản hoàn tất KOC Portal — 15/09/2026

### Hành trình sản phẩm

- Lấy hành trình `Tìm việc → Ứng tuyển → Làm việc → Nhận tiền → Xây uy tín → Hỗ trợ` làm cấu trúc trải nghiệm chính.
- Tách **Tìm việc** thành menu cấp 1 ngay sau Tổng quan; không còn nằm trong nhóm quản lý công việc đã nhận.
- Nhóm **Công việc** gồm Đầu việc, Lịch booking, Sản phẩm và Lịch sử hợp tác.
- **Tài chính** dẫn thẳng tới Ví, không tạo menu con chỉ có một mục.
- Đồng bộ cùng một cấu trúc menu trên desktop, mobile và thanh truy cập nhanh.

### Tìm việc và ứng tuyển

- Đổi ngôn ngữ hệ thống từ “Cơ hội chiến dịch” sang “Tìm việc KOC”, với hai khu vực “Việc đang tuyển” và “Đã ứng tuyển”.
- Bộ lọc hỗ trợ từ khóa, booking/giao sản phẩm, loại nội dung, khoảng ngân sách, thứ tự ưu tiên và số kết quả; trạng thái lọc được giữ khi phân trang.
- Thẻ việc hiển thị thu nhập ước tính trên mỗi KOC, tổng ngân sách, số vị trí, số lượt ứng tuyển, nội dung phải làm, deadline và độ phù hợp hồ sơ.
- KOC có thể lưu/bỏ lưu việc; dữ liệu lưu trong bảng `KocCampaignFavorites`, không phụ thuộc trình duyệt.
- Form ứng tuyển cho phép gửi lời giới thiệu và mức thù lao đề xuất; Brand thấy mức giá này trong shortlist.
- Hồ sơ ứng tuyển có timeline “Đã gửi → Brand cân nhắc → Được chọn”, hướng dẫn bước tiếp theo và SLA phản hồi 3 ngày làm việc kèm cảnh báo quá hạn.

### Vận hành, hồ sơ và tài chính

- Dashboard ưu tiên việc cần làm, deadline, booking, thông báo, hồ sơ và đường dẫn tìm việc.
- Luồng công việc đã nối đầu việc, booking, giao nhận sản phẩm, nội dung, lịch sử hợp tác, tranh chấp và thanh toán.
- Hồ sơ hiển thị mức hoàn thiện, mục còn thiếu, trạng thái duyệt, mạng xã hội, thư viện và trạng thái đồng bộ nền tảng.
- Ví phân tách tiền khả dụng, chờ đối soát, tạm giữ và chờ chuyển; OTP dùng thời hạn chính sách thực tế.
- Thông báo dẫn tới đúng hành động; tranh chấp hỗ trợ bằng chứng ảnh/PDF và lịch sử trao đổi.

### Giao diện và kỹ thuật

- Chuẩn hóa BeVietnamPro/Segoe UI/Tahoma; loại bỏ Saira Stencil khỏi public và tiêu đề KOC.
- Tinh chỉnh màu, độ tương phản, card, border, heading và các hero; sửa màu chữ hero Tranh chấp bị quy tắc nền sáng ghi đè.
- Menu mobile hai cấp dùng một mục trên mỗi hàng; notification và DataTables chuyển thành card đúng chiều rộng viewport.
- Chuẩn hóa CTA mobile tối thiểu 40px, focus-visible, reduced-motion, trạng thái rỗng/lỗi và accessibility label.
- Tách CSS/JavaScript KOC theo module, bỏ inline executable script, thêm cache-busting và kiểm tra cú pháp JavaScript.
- Thêm script database idempotent `20260915_koc_job_favorites_proposed_amount.sql`; giữ nguyên dữ liệu tài khoản, Brand, KOC, chiến dịch và ứng tuyển hiện hữu.

### Nghiệm thu

- Build cuối: **0 lỗi, 0 cảnh báo**.
- Unit test tầng Application: **276/276 đạt**.
- Audit đăng nhập thật: **24/24 lượt trang** tại desktop 1440px và mobile 390px đạt.
- Không phát hiện route sai, tràn ngang, ID trùng, ảnh hỏng, lỗi mã hóa tiếng Việt, CTA thiếu tên hoặc vùng chạm dưới chuẩn kiểm tra.
- Browser regression cho DataTables, URL state, phân trang, tải lỗi/thử lại và thông báo Brand/KOC: **đạt**.
- Website vận hành tại `http://localhost:5201` và phản hồi HTTP 200.

### Phạm vi ngoài nghiệm thu production

- Trước khi phát hành chính thức vẫn cần UAT với KOC/Brand thật, kiểm thử bảo mật và tải, cấu hình email/OAuth/thanh toán production, giám sát và phương án backup/restore database.
