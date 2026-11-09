# Quy chuẩn layout KOC

## Header và menu KOC (29/07/2026)

- Layout KOC dùng namespace riêng: `.koc-*`; không dùng class `.admin-*` hoặc class chuyên biệt của Brand.
- CSS riêng: `wwwroot/css/koc/koc.css`.
- JavaScript riêng: `wwwroot/js/koc/koc.js`.
- Font toàn khu vực KOC là Roboto; không kế thừa font Saira/Arimo cũ.
- Header dùng gradient tím theo nhận diện TopStar, logo chuyển trắng bằng CSS, trạng thái active dùng nền kem và điểm nhấn cam.
- Menu động trong database được gom hai cấp:
  - Tổng quan.
  - Công việc: Việc làm phù hợp, Đầu việc của tôi, Lịch booking, Đơn hàng, Lịch sử hợp tác.
  - Hồ sơ & uy tín: Hồ sơ KOC, Đánh giá từ Brand.
- Tài chính: Ví thu nhập.

## Ví thu nhập KOC

- Bố cục chuẩn theo thứ tự sử dụng: số dư có thể rút → bốn chỉ số tài chính → tài khoản nhận tiền/yêu cầu rút → lịch sử giao dịch.
- Màu nhấn ví dùng gradient hồng–coral–vàng thuộc bộ nhận diện KOC; không dùng khối cam phủ toàn chiều ngang như giao diện cũ.
- Trạng thái nghiệp vụ hiển thị hoàn toàn bằng tiếng Việt: `Đối soát`, `Khả dụng`, `Tạm giữ`, `Đang xử lý`, `Đã chuyển`, `Không thành công`.
- Thông báo sau thao tác dùng toast chuẩn `data-koc-flash`; không dùng alert trình duyệt.
- Lịch sử giao dịch dùng bảng gọn, dòng chẵn/lẻ rõ ràng và mở chi tiết đối soát tại từng dòng; responsive không tạo thanh cuộn cho toàn trang.
- Trạng thái rỗng phải có icon, tiêu đề và hướng dẫn hành động tiếp theo.
- Form ngân hàng và rút tiền giữ nguyên nghiệp vụ backend; giao diện dùng `novalidate` để không bật validation mặc định của trình duyệt.
- Trang ví được chia thành ba tab độc lập: `Tài khoản nhận tiền`, `Yêu cầu rút tiền`, `Lịch sử giao dịch`; số dư và KPI luôn hiển thị để KOC không mất ngữ cảnh.
- Danh sách yêu cầu rút và giao dịch dùng jQuery DataTables bản offline tại `/lib/datatables`, có tìm kiếm, lọc trạng thái, đánh số thứ tự và phân trang tiếng Việt.
- Nghiệp vụ hiện chỉ cho phép một tài khoản nhận tiền đang hoạt động. Tab tài khoản hiển thị tài khoản hiện tại và form cập nhật, không tạo danh sách nhiều tài khoản giả với dữ liệu chưa được backend hỗ trợ.
- Tài khoản nhận tiền có thể xóa nhưng bắt buộc nhập lý do 10–500 ký tự trong modal khóa backdrop. Xóa chỉ loại bỏ tài khoản hiện tại; payout cũ vẫn giữ snapshot và thao tác được ghi vào `AuditLogs` với ngân hàng, 4 số cuối và lý do.
- Validation tài khoản hiển thị ngay dưới đúng trường lỗi bằng `is-invalid` và icon cảnh báo; không đưa lỗi dữ liệu đầu vào lên toast. Toast chỉ dùng cho thành công hoặc lỗi hệ thống.
  - Hỗ trợ: Tranh chấp & hỗ trợ, Thông báo.
- Nhãn `Đầu việc` lỗi encoding đã được sửa bằng migration `86_group_koc_navigation_two_levels_fix_vietnamese.sql`.
- Desktop mở menu con bằng click; chỉ một dropdown được mở tại một thời điểm. Click ngoài hoặc nhấn Escape đóng menu.
- Mobile dùng nút hamburger và menu dạng lưới; không tạo thanh cuộn ngang.
- Khi hồ sơ chưa đủ điều kiện, nhóm Hồ sơ & uy tín vẫn mở được để KOC cập nhật hồ sơ; các chức năng nghiệp vụ khác tiếp tục bị khóa.
- Active menu dùng một hàm route chung: `/Koc/EditProfile` thuộc “Hồ sơ KOC”, `/Koc/CampaignDetail` thuộc “Việc làm phù hợp”, `/Koc/DisputeDetail` thuộc “Tranh chấp & hỗ trợ”. Menu cấp 1 và cấp 2 luôn active đồng nhất.
- Tab thông tin cơ bản của trang chỉnh sửa hồ sơ chia thành ba khối rõ ràng: tài khoản, ảnh/tài liệu và địa chỉ; avatar đi cùng trường tải ảnh, tài liệu hiện tại đi cùng trường tải hồ sơ để tránh khoảng trống và lệch lưới.
- Nhận diện KOC dùng bộ màu riêng “Creator”: gradient xanh đêm–cobalt–cyan, coral/gold làm màu hành động và active; không dùng gradient tím chung của Admin/Brand. Màu thương hiệu riêng của TikTok/Facebook/Instagram/YouTube vẫn được giữ nguyên.
- Admin duyệt hồ sơ xong dùng Razor Page `/Admin/Brands` kèm route `Type=KOC|Brand`, tương ứng URL `/Admin/Accounts/KOC|Brand`; không dùng page `/Admin/Kocs` vì page này không tồn tại. Lỗi nhập căn cứ duyệt hiển thị trực tiếp ngay dưới ô nhập.
- Thông báo Admin thống nhất dùng `AdminToast`: thành công xanh + icon check, cảnh báo vàng + icon tam giác, lỗi đỏ + icon x; tự đóng, không render khối Bootstrap alert và không dùng alert trình duyệt. Luồng duyệt phải truyền cả nội dung lẫn cờ thành công qua TempData.
- Trang KOC đánh giá Brand dùng layout tác vụ riêng: header có ngữ cảnh, KPI chờ/hoàn tất, card Brand–campaign, bộ chọn sao trực quan, phản hồi và yêu cầu Admin được phân cấp rõ; icon đi cùng tiêu đề, trạng thái và hành động. CSS đặt hoàn toàn trong `css/koc/koc.css`.
- Trang đánh giá rút gọn bằng 2 tab: “Brand đánh giá bạn” và “Bạn đánh giá Brand”. Đầu trang có 4 KPI (điểm trung bình, số đánh giá nhận, chờ phản hồi, chờ đánh giá); tab có badge và giữ đúng tab sau khi POST. Nếu còn tác vụ đánh giá, tab gửi đánh giá được ưu tiên mở mặc định.
- Thông báo KOC thống nhất dùng `KocToast` (success/error/warning/info, có icon, tự đóng 4,5 giây), không dùng `alert` nguyên khối hoặc alert trình duyệt. Form đánh giá/phản hồi gửi bằng AJAX, khóa nút và hiện spinner trong lúc xử lý để chống gửi kép; thành công tải lại KPI + nội dung tab bằng AJAX, không reload toàn trang. Dữ liệu dạng card/tab dùng AJAX fragment; dữ liệu bảng lớn mới dùng DataTables server-side.
- **Mẫu validation chuẩn toàn hệ thống:** form đặt `novalidate`, không dùng popup validation của trình duyệt; trường lỗi có `border` đỏ, icon cảnh báo nằm **bên trong góc phải của input/textarea**, thông báo lỗi tiếng Việt màu đỏ nằm ngay dưới trường và không lặp icon ở dòng lỗi. Lỗi tự mất khi người dùng nhập/chọn lại. JS kiểm tra trước AJAX, backend luôn kiểm tra lại. Với trang đánh giá: phản hồi 2–2.000 ký tự, căn cứ xem xét 10–2.000 ký tự, bắt buộc chọn 1–5 sao, nhận xét tối đa 2.000 ký tự.
- Thao tác phản hồi review phải idempotent: nếu `ReviewReplies.ReviewId` đã tồn tại (kể cả do hai request đồng thời), service trả kết quả thành công với bản ghi hiện có và UI tải lại dữ liệu; không báo lỗi giả “yêu cầu khác”. Khi `SaveChanges` lỗi, phải clear tracker, đọc lại bản ghi theo unique key rồi mới kết luận thất bại.
- Mọi notification tạo mới bắt buộc truyền `AudienceRole` (`ADMIN|BRAND|KOC`) theo migration/trigger `TR_Notifications_RequireAudienceRole`. Với phản hồi review, role người nhận được xác định từ tài khoản người đã viết review; thiếu role sẽ khiến trigger rollback cả phản hồi và không được diễn giải nhầm thành lỗi cạnh tranh.
- Rule chỉnh sửa: KOC được sửa đánh giá Brand trong 24 giờ nếu review còn `Completed`, chưa có reply và chưa có appeal; bắt buộc lý do 10–500 ký tự và ghi `ReviewRevisionHistories` append-only. Appeal `Open` chỉ được bổ sung (ghi `ReviewAppealSupplements`, không đổi căn cứ gốc) hoặc rút với lý do 10–500 ký tự; sau `Accepted|Rejected|Withdrawn` chỉ đọc. Migration áp dụng: `87_add_review_revision_and_appeal_followup_history.sql`.
- AJAX tác vụ đánh giá/khiếu nại luôn phải trả JSON, kể cả hồ sơ bị chặn hoặc backend phát sinh lỗi; không trả redirect/HTML cho request AJAX. JavaScript phải kiểm tra `Content-Type`, hiển thị đúng thông điệp từ máy chủ và chỉ dùng lỗi “không thể kết nối” khi fetch thực sự thất bại.
- Entity ánh xạ tới bảng SQL Server có trigger phải khai báo trigger trong EF (`ToTable(...HasTrigger(...))`) để EF không sinh `OUTPUT` không tương thích. `ReviewAppeals` dùng trigger `TR_ReviewAppeals_ProtectIdentity`; mọi cập nhật trạng thái phải giữ trigger và mapping này.
- Trạng thái khiếu nại trên UI KOC luôn dùng tiếng Việt và icon: `Open` → “Đang xem xét”, `Accepted` → “Đã chấp nhận”, `Rejected` → “Đã từ chối”, `Withdrawn` → “Đã rút”. Với yêu cầu do KOC tự rút, `DecisionNote` phải mang nhãn “Lý do rút yêu cầu”, không được trình bày thành “Kết luận Admin”.
- Danh sách đánh giá không tải cố định 100 bản ghi và không bung toàn bộ nội dung. Phải phân trang tại database (10/20/50 dòng), lọc theo Brand/chiến dịch, khoảng ngày, số sao và trạng thái phản hồi/xem xét; lọc và chuyển trang bằng AJAX. Mỗi card mặc định chỉ hiển thị Brand, chiến dịch, thời gian và số sao; nội dung, phản hồi và khiếu nại mở theo nhu cầu.
- Trong tab đã có nhãn và số lượng thì không lặp lại tiêu đề danh sách ngay dưới bộ lọc. Nút Lọc/Reset dùng typography và padding gọn của `btn-sm`, nhưng khung nút cao 38px để thẳng hàng control. Nút mở chi tiết card đặt sát phải, trình bày như button có icon và đổi hướng chevron khi mở.
- Thanh lọc và danh sách phải tách nhau bằng khoảng đệm tối thiểu 16px. Nút hành động phải thẳng cả mép trên và mép dưới với hàng input/select.
- Mọi control trong bộ lọc phải có label hiển thị. Nhóm nút Lọc/Tải lại cao 38px và nằm cùng hàng control. Card đánh giá không dùng đường kẻ ngăn header; nút mở nội dung đặt ngay dưới số sao ở góc phải header.
- Badge trạng thái gồm icon và chữ phải dùng `inline-flex`, căn giữa hai chiều và đặt line-height 1; không dựa vào baseline mặc định khiến chữ lệch giữa button/badge.
- Tab “Bạn đánh giá Brand” áp dụng cùng chuẩn dữ liệu lớn: phân trang database 10/20/50, tìm Brand/chiến dịch, lọc ngày, số sao và trạng thái chờ/đã đánh giá bằng AJAX. Card mặc định thu gọn; form chỉ mở qua “Đánh giá ngay” hoặc “Xem đánh giá”.
- Bộ chọn điểm đánh giá dùng 5 nút sao compact 34×32px, không hiển thị số lớn trong nút; giá trị số vẫn tồn tại dưới dạng nhãn hỗ trợ trình đọc màn hình. Sao được chọn dùng nền vàng nhẹ và viền rõ.
- Trang xem hồ sơ KOC không dùng đường kẻ để chia mọi subsection. Phân cấp bằng khoảng trắng, heading có icon và nền khối nhẹ; các nhóm chính gồm Thông tin cơ bản, Thông tin thể trạng, CV/Hồ sơ năng lực và Mạng xã hội.
- Trang xem hồ sơ KOC chia 3 tab: Tổng quan, Mạng xã hội, Thư viện. Cảnh báo/mức độ hoàn thiện nằm ngoài tab ở phía trên. Tab đồng bộ với URL hash (`#profile-overview-pane`, `#profile-social-pane`, `#profile-media-pane`) để có thể mở đúng nhóm và giữ trạng thái khi chia sẻ/tải lại.
- Nội dung Admin, Brand và KOC dùng `.container` của layout; đã bỏ `.page-wrap` khỏi các view và CSS. Trang public dùng `.container` trực tiếp khi cần căn nội dung.

## Hướng dẫn hoàn thiện hồ sơ

## Dashboard KOC

- Dashboard KOC là “bàn làm việc hôm nay”, không phải trang liệt kê số liệu chung chung. Thứ tự ưu tiên cố định: việc cần xử lý, lịch booking sắp tới, tài chính có thể sử dụng, thông báo và độ hoàn thiện hồ sơ.
- Khối KPI chỉ hiển thị bốn chỉ số có hành động tiếp theo rõ ràng: hồ sơ chờ phản hồi, chiến dịch đang thực hiện, số dư khả dụng/thu nhập ước tính và điểm uy tín. Toàn bộ card là liên kết tới đúng màn hình nghiệp vụ.
- Dải cảnh báo nhanh dùng cho nội dung cần sửa, lịch sắp tới, thông báo chưa đọc và điểm hồ sơ; không lặp lại KPI và không dùng số liệu giả.
- Danh sách công việc và lịch booking lấy trực tiếp từ database, giới hạn các bản ghi gần nhất để dashboard không biến thành trang quản trị danh sách. Trang chi tiết tương ứng chịu trách nhiệm lọc và phân trang.
- Cột phụ luôn dành cho tình trạng hồ sơ/uy tín, thông báo mới và truy cập nhanh. Trạng thái phải dùng tiếng Việt, có icon và chỉ dẫn hành động.
- Trạng thái rỗng phải có giải thích và nút hành động phù hợp; không hiển thị dòng trống kiểu “Chưa có công việc”.
- Dashboard dùng hoàn toàn namespace `.koc-dashboard-*` trong `css/koc/koc.css`, responsive không có thanh cuộn ngang và không tác động CSS/JS của Brand hoặc Admin.
- Màu nhận diện dashboard dùng gradient Creator xanh đêm–cobalt–cyan, vàng/coral làm điểm nhấn hành động; không dùng gradient tím của Admin/Brand.
- Truy vấn danh sách chiến dịch phải lọc/sắp xếp trên projection mà EF Core dịch được. Không tạo positional record (`new CampaignRow(...)`) rồi mới gọi `Where`; dùng member-initializer hoặc lọc entity/anonymous projection trước khi dựng DTO. Subquery lấy deliverable phải có `OrderBy` ổn định trước `FirstOrDefault`.
- Trang `/Koc/Jobs` luôn hiển thị bộ lọc có đầy đủ label; không thu bộ lọc vào accordion. Danh sách phân trang tại database với lựa chọn 12/24/48 chiến dịch, giữ toàn bộ điều kiện lọc và sắp xếp khi chuyển trang.
- Card cơ hội chỉ trình bày thông tin giúp KOC quyết định nhanh: Brand, tên chiến dịch, ngân sách, loại nội dung, yêu cầu booking/giao nhận, thời hạn và trạng thái ứng tuyển. “Đăng ký của tôi” là vùng thu gọn riêng để không kéo dài danh sách khám phá chiến dịch.
- Danh sách “đang tuyển” chỉ lấy campaign `Published`, đang hiển thị và có `StartDate` lớn hơn ngày hiện tại; không đưa chiến dịch đã bắt đầu hoặc có khoảng ngày không hợp lệ vào cơ hội mới.
- Không hiển thị mã package kỹ thuật (`SELECT_*`, `ASSIGNED_*`) trên card KOC. Khi Brand chưa có trường/logo thật trong database, dùng monogram từ tên Brand làm fallback; không dùng icon tòa nhà giả làm logo.
- Typography card cơ hội tối thiểu 11px cho metadata, 16px cho tên chiến dịch và dùng màu chữ đủ tương phản; không dùng chữ 8–9px cho nội dung KOC cần đọc để ra quyết định.
- Bộ lọc cơ hội KOC chỉ giữ bốn điều kiện có giá trị quét nhanh: từ khóa, hình thức phối hợp, sắp xếp và số lượng hiển thị. Không đưa khoảng ngân sách và loại nội dung vào thanh lọc mặc định.
- Bộ lọc chỉ có hành động “Lọc”, không đặt thêm nút reload/reset khi người dùng có thể xóa nội dung hoặc chọn lại giá trị. Card dùng nút compact “Xem chi tiết” ở góc phải; không dùng CTA toàn chiều rộng và không ghi “ứng tuyển” trước khi KOC xem điều kiện chi tiết.
- Ngày chiến dịch trình bày như timeline Bắt đầu → Kết thúc, mỗi mốc có icon, nhãn và ngày; tránh hai cột metadata rời rạc khó quét.
- `Campaigns.BudgetAmount` là tổng ngân sách Brand dự kiến dành cho toàn chiến dịch, không phải thù lao KOC chắc chắn nhận được. Card phải ghi rõ “Tổng ngân sách dự kiến” và chú thích “Thù lao xác nhận khi được chọn”; chỉ hiển thị “Thù lao KOC” khi hệ thống có offer/participation amount riêng.
- Card cơ hội dùng viền, dải màu nhận diện, khối ngân sách và typography tương phản rõ; không dùng quá nhiều màu xám nhạt khiến Brand, thời gian và điều kiện khó đọc.
- Lưới “Chiến dịch đang tuyển” không đặt thêm card/box bao ngoài; tiêu đề và danh sách nằm trực tiếp trên nền trang. Card campaign dùng nền trắng trung tính và không có dải border màu ở cạnh trái.
- Khối tổng ngân sách trên card không dùng box và không lặp dòng diễn giải dưới giá; ý nghĩa chi tiết nằm trong tooltip của icon trợ giúp. Giá dùng xanh lá tài chính, avatar/CTA dùng xanh Creator; cam chỉ dùng làm điểm nhấn, không phủ đồng loạt các thành phần.
- Các nhãn hình thức phối hợp/đầu ra của campaign đặt ở góc trái footer card, cùng hàng với nút “Xem chi tiết” ở góc phải. Khối lịch phía trên chỉ trình bày ngày bắt đầu–kết thúc; thứ bậc đọc là nhận diện → ngân sách → lịch → điều kiện và thao tác.
- Tên chiến dịch trên card hiển thị tối đa 2 dòng với chiều cao cố định để lưới không lệch hàng; tên đầy đủ được giữ trong tooltip native. Tên Brand chỉ hiển thị một dòng và ellipsis khi quá dài.
- Trang “Đầu việc của tôi” truy vấn phân trang từ database (12/24/48), có lọc từ khóa, trạng thái, deadline và sắp xếp; không tải toàn bộ submission về trình duyệt. Card thống kê luôn tính trên toàn bộ tập đầu việc của KOC, độc lập với bộ lọc danh sách.
- Mỗi đầu việc hiển thị Brand, campaign, loại/số lượng deliverable, deadline, phiên bản, lần gửi gần nhất, trạng thái tiếng Việt, phản hồi và lịch sử append-only. Form nộp link dùng kiểm tra lỗi nội tuyến chuẩn KOC, `novalidate`, AJAX và toast chung; không dùng validation popup/alert của trình duyệt.
- Dữ liệu kiểm thử đầu việc dùng script `89_seed_koc_deliverable_tasks.sql`, tạo idempotent 24 submission cùng deadline, phản hồi, lịch sử và audit cho `seed.koc.0001@seed.topstar.local`.
- Card đầu việc mặc định thu gọn; KOC mở từng card để xem deadline, phản hồi, lịch sử và thao tác. Badge chiến dịch tách khỏi badge đầu việc, hiển thị rõ `Đang thực hiện / Đã hết hạn / Đã hoàn tất`.
- Nộp nội dung dùng modal Bootstrap `backdrop: static`, không đóng khi click vùng ngoài. Một phiên bản cho phép link hoặc tệp (ít nhất một), ghi chú tùy chọn; tệp hỗ trợ JPG/PNG/WEBP/PDF/DOC/DOCX/MP4/MOV tối đa 25 MB.
- Link, tệp, tên tệp và ghi chú được snapshot vào cả `Submissions` và `SubmissionHistories` để mọi lần nộp lại có thể đối chiếu. Schema được bổ sung bằng `90_add_submission_version_content.sql`; file lỗi nghiệp vụ phải được xóa khỏi storage để không tạo tệp mồ côi.
- Lịch sử nộp bài không dùng dropdown/popup nổi nhỏ trên card. Mở bằng modal timeline rộng, cuộn bên trong và không đóng khi click vùng ngoài; mỗi sự kiện phải có số phiên bản, mốc thời gian, trạng thái tiếng Việt, ghi chú KOC, phản hồi Brand, link và tệp snapshot. Phiên bản mới nhất phải được đánh dấu rõ.
- Khi bài đang `Submitted/Chờ Brand duyệt` và campaign còn thực hiện, KOC được dùng “Thay thế bài nộp” để sửa trường hợp gửi nhầm. Thao tác phải tăng revision, tạo history `Replaced`, snapshot đầy đủ nội dung cũ/mới, ghi audit và thông báo lại Brand; không cập nhật đè lịch sử. Bài đã duyệt hoặc từ chối là kết quả cuối, không được sửa trực tiếp.
- Ngoại lệ bảo mật cho lịch sử append-only: nếu KOC nhập nhầm link, ghi chú hoặc tải nhầm tệp chứa thông tin cá nhân/nhạy cảm, KOC được “Ẩn dữ liệu nhạy cảm” trên đúng phiên bản và chọn từng thành phần cần xử lý. Hệ thống phải kiểm tra quyền sở hữu, bắt buộc lý do 10–500 ký tự, xóa file vật lý, null hóa link/ghi chú/tên tệp khỏi snapshot và current record tương ứng, đồng thời append sự kiện `SensitiveDataRedacted` và audit chỉ gồm loại dữ liệu + lý do. Không sao chép nội dung nhạy cảm vào audit. Trigger append-only chỉ cho phép null hóa các trường snapshot nhạy cảm, vẫn cấm sửa nội dung khác và cấm delete; áp dụng bằng `91_allow_submission_history_sensitive_redaction.sql`.
- Khối tổng ngân sách trên card không dùng box và không lặp dòng diễn giải dưới giá; ý nghĩa chi tiết nằm trong tooltip icon trợ giúp. Giá dùng xanh lá tài chính, avatar/CTA dùng xanh Creator, card không có dải border màu ở cạnh trái và cam không được dùng phủ toàn bộ thành phần.
- Seed campaign không dùng ngày cố định. Script `88_refresh_seed_campaign_discovery_dates.sql` đảm bảo tối thiểu 60 và tạo 120 campaign seed `Published` có ngày bắt đầu trong tương lai; script phải chạy bằng UTF-8 (`sqlcmd -f 65001`) để khớp tiêu đề tiếng Việt và giữ lịch sử chuyển trạng thái append-only.

- Trang `/Koc/Profile` dùng trực tiếp kết quả `IProfileAccessService.CheckKocAsync`; checklist hiển thị và quy tắc khóa tính năng luôn dùng cùng một nguồn sự thật.
- Hiển thị phần trăm hoàn thiện trên 13 yêu cầu bắt buộc, trạng thái duyệt và số mục còn thiếu.
- Mỗi mục còn thiếu có tên tiếng Việt, giải thích cần nhập gì và liên kết tới đúng tab của trang Chỉnh sửa hồ sơ.
- Các nhóm được hướng dẫn gồm thông tin cơ bản, địa chỉ, thể trạng/cá nhân, kênh social và tối thiểu ba lĩnh vực nội dung.
- Nếu thông tin đã đủ nhưng chưa được duyệt, UI không yêu cầu KOC nhập thêm mà thông báo rõ đang chờ Admin xử lý.
- Cảnh báo chung trên layout liên kết thẳng tới khối `Mức độ hoàn thiện hồ sơ`, không còn thông báo chung chung.

## Form chỉnh sửa mạng xã hội

- Mỗi nền tảng là một accordion card riêng; kênh có dữ liệu hoặc có lỗi validation được mở sẵn, kênh trống mặc định thu gọn.
- Header card hiển thị tóm tắt followers và số lĩnh vực đã chọn, giúp KOC quét nhanh mà không cần mở tất cả.
- Form trong card dùng bố cục Link kênh 8/12 và Followers 4/12; mobile tự chuyển một cột.
- Lĩnh vực nội dung dùng component chip riêng trong CSS KOC, có dấu tick và trạng thái chọn rõ ràng; không dùng hàng loạt button Bootstrap gây rối.
- Accordion và chip chỉ dùng `koc.js`/`koc.css`, không ảnh hưởng Brand hoặc Admin.
# Quy chuẩn danh sách đầu việc KOC

- Trang `/Koc/Tasks` sử dụng jQuery DataTables từ thư viện offline trong `wwwroot/lib/datatables`; không dùng CDN.
- Dữ liệu tải bằng AJAX server-side từ database. Tìm kiếm, lọc, sắp xếp và phân trang phải thực hiện ở server; không tải toàn bộ dữ liệu xuống trình duyệt.
- Bảng không bật cuộn ngang. Ở màn hình hẹp, ẩn các cột phụ và giữ lại chiến dịch, trạng thái, thao tác; nội dung đầy đủ nằm trong dòng mở rộng.
- Typography bảng ở màn hình desktop: nội dung chính tối thiểu 14px, tiêu đề cột 13px, nội dung phụ tối thiểu 12px; dùng màu tương phản và độ đậm từ 500 để tránh chữ nhỏ, nhạt hoặc nhòe.
- Mỗi dòng chỉ hiển thị thông tin đủ để quét nhanh. Nộp bài, thay thế phiên bản, xem lịch sử và ẩn dữ liệu nhạy cảm nằm trong phần chi tiết/modal.
- Phân trang chuẩn dùng 12, 24 hoặc 48 dòng. Bộ lọc ngoài DataTables phải tải lại AJAX và quay về trang đầu.
- Modal thao tác phải dùng backdrop tĩnh, bẫy lỗi nội tuyến bằng tiếng Việt và không dùng `alert()` hay validation mặc định của trình duyệt.

## Lịch booking KOC

- Trang booking phải có tổng quan số lịch sắp tới, lịch cần phản hồi và lịch đã xác nhận.
- Lịch tháng dùng màu trạng thái nhất quán: xanh dương chờ xác nhận, xanh lá đã xác nhận, vàng đề xuất lại, xám đã hủy.
- Calendar là lịch làm việc hợp nhất: hiển thị booking và deadline đầu việc. Deadline dùng màu cam và liên kết tới trang đầu việc; không được đồng nhất deadline với một booking.
- Khi nhấn deadline, trang đầu việc phải lọc và định vị bằng `SubmissionId`, tự mở chi tiết và mở modal nộp bài nếu công việc còn được phép xử lý.
- Booking chỉ tồn tại khi chiến dịch yêu cầu đặt lịch và Brand đã tạo đề xuất thời gian. Có đầu việc không đồng nghĩa đã có booking.
- Mọi trạng thái hiển thị bằng tiếng Việt. Nhấn sự kiện trên lịch sẽ điều hướng đến đúng lịch trong danh sách.
- Đề xuất lại và hủy lịch dùng modal backdrop tĩnh; thời gian và lý do được kiểm tra nội tuyến, không dùng validation mặc định của trình duyệt.
- Lịch sử thay đổi phải được giữ nguyên để đối chiếu; không xóa hoặc ghi đè sự kiện cũ.
## Sản phẩm chiến dịch / vận đơn KOC (29/07/2026)

- `/Koc/Shipments` là workspace giao nhận, không còn là một empty card đơn lẻ: đầu trang có thống kê `Đang giao / Chờ Brand gửi / Đã nhận / Cần xử lý`, bộ lọc từ khóa + trạng thái và danh sách vận đơn đóng/mở.
- Mỗi vận đơn hiển thị rõ campaign, hãng vận chuyển, mã vận đơn, snapshot người nhận/địa chỉ, checkpoint gần nhất và timeline. Trạng thái dùng tiếng Việt, màu theo ngữ nghĩa; không hiển thị enum kỹ thuật.
- Thao tác `Đã nhận hàng`, `Báo sự cố`, `Thêm ảnh`, `Lịch sử`, `Ảnh bằng chứng` nằm trong ngữ cảnh vận đơn. Form mở bằng modal static backdrop; validation nội tuyến, không dùng validation popup mặc định của trình duyệt.
- History, tracking và evidence tiếp tục là append-only. Seed không sửa/xóa journal cũ; script có khóa deterministic để chạy lặp an toàn.
- SQL dữ liệu kiểm thử: chạy `92_seed_koc_shipment_demo_dataset.sql`, sau đó `93_verify_koc_shipment_demo_dataset.sql`. Bộ seed tạo 6 vận đơn cho `seed.koc.0001@seed.topstar.local`, phủ `Created / InTransit / Delivered / Issue / Returned`, kèm trạng thái participation, shipment history, tracking checkpoint và evidence có file tĩnh thật.
- CSS/JS trang này chỉ đặt trong vùng KOC: `.shipment-*` ở `wwwroot/css/koc/koc.css` và `wwwroot/js/koc/shipments.js`; không dùng chung hoặc ghi đè layout Brand/Admin.
- Danh sách sản phẩm sử dụng jQuery DataTables bản offline và tải bằng AJAX handler `?handler=Data`. Handler áp dụng tìm kiếm, trạng thái và giới hạn trang trước khi trả JSON; không trả toàn bộ dòng vào DataTables. Bảng chỉ giữ các cột quyết định nhanh; hành trình, địa chỉ, evidence và thao tác nằm trong child row để không tạo thanh cuộn ngang ở desktop.
- Xác nhận nhận hàng bắt buộc có một ảnh JPG/PNG/WEBP tối đa 5 MB và ghi chú từ 10–500 ký tự. Ảnh, ghi chú, trạng thái `Delivered`, history, notification và audit phải được ghi trong cùng transaction; nếu nghiệp vụ thất bại thì file vừa tải lên phải được dọn.
- Validation của modal phải dùng `novalidate` và bẫy lỗi nội tuyến thống nhất: viền đỏ, biểu tượng cảnh báo bên phải ô, thông báo tiếng Việt ngay dưới ô và focus vào lỗi đầu tiên. Không dùng popup validation của trình duyệt.
- Footer DataTables phải có padding riêng ở cả cạnh dưới và cạnh ngang; nội dung đếm và cụm phân trang không được chạm border của card.
- Khi dùng DataTables Bootstrap phải phủ cả hai cấu trúc footer có thể sinh ra (`.dt-layout-row` và `.row`). Wrapper bảng giữ khoảng đệm đáy độc lập; không dựa riêng vào margin của `.dt-info` hoặc `.pagination` vì sẽ làm hai góc footer dính sát border.

### Kết quả review vận đơn KOC (30/07/2026)

- Đã đối chiếu lại quyền sở hữu ở tất cả thao tác: KOC chỉ đọc/cập nhật vận đơn thuộc hồ sơ của chính mình; Brand được thông báo khi KOC xác nhận hoặc báo sự cố.
- Xác nhận nhận hàng chỉ hợp lệ khi vận đơn `InTransit`; bắt buộc ảnh hợp lệ và ghi chú 10–500 ký tự. Rule dùng chung ở PageModel và service, có unit test riêng để tránh lệch validation client/server.
- Báo sự cố dùng cùng rule chuẩn 10–1.000 ký tự; trạng thái, notification, history và audit không bị ghi đè.
- Upload ảnh độc lập và upload lúc xác nhận đều dọn file vật lý nếu nghiệp vụ hoặc transaction thất bại. Evidence giới hạn 10 ảnh cho mỗi vai trò/vận đơn và tiếp tục append-only.
- Mỗi ảnh evidence bổ sung bắt buộc có ghi chú 10–500 ký tự. Evidence đã gửi là chứng từ append-only: KOC/Brand không có thao tác sửa hoặc xóa; nếu cần làm rõ phải tải thêm bản ghi mới để giữ nguyên lịch sử đối chiếu và audit.
- Child row đã được thu gọn thành ba card thông tin. Timeline chia checkpoint theo tỷ lệ bằng nhau, đường nối chạy từ tâm điểm này đến tâm điểm kế tiếp; mobile mới chuyển sang cuộn ngang.
- Nút evidence dùng nhãn ngắn theo nguồn (`Ảnh Brand gửi`, `Ảnh KOC nhận`, `Ảnh KOC gửi`); caption đầy đủ nằm trong tooltip, tránh làm vỡ layout.
- Validation không dùng popup trình duyệt: viền đỏ, icon CSS ổn định không phụ thuộc biến thể Font Awesome, thông báo dưới ô, `aria-invalid` và focus vào lỗi đầu tiên.
- Không cần SQL mới cho vòng review này; schema evidence/tracking hiện tại đã đáp ứng và lịch sử quan trọng tiếp tục được giữ nguyên.

## Quy chuẩn phóng to ảnh và gallery KOC (30/07/2026)

- Colorbox được nạp một lần tại `_KocLayout`; từng trang không khai báo lặp CSS/JS. Tiện ích dùng chung là `wwwroot/js/koc/koc-gallery.js`.
- Ảnh nội dung cần phóng to dùng `data-koc-colorbox`; `data-gallery` là khóa nhóm ảnh. Hiện áp dụng cho thư viện hồ sơ, ảnh đại diện hiện tại và bằng chứng giao nhận; không gắn vào logo, icon hoặc ảnh trang trí.
- Khu vực KOC dùng thống nhất `jquery-colorbox` 1.6.4 bản offline tại `wwwroot/lib/colorbox`; không tải Colorbox/Magnific Popup từ CDN và không áp CSS/JS này sang Brand hoặc Admin.
- Ảnh trong cùng một ngữ cảnh nghiệp vụ phải được gom thành một gallery riêng: thư viện hồ sơ là một nhóm; bằng chứng của từng vận đơn là một nhóm độc lập. Không được chuyển ảnh giữa hai vận đơn khi bấm Trước/Sau.
- Lightbox giới hạn theo viewport, giữ nguyên tỷ lệ ảnh, không cắt ảnh, hiển thị caption, số thứ tự và nút Trước/Sau/Đóng bằng tiếng Việt. Hỗ trợ phím mũi tên, ESC, bẫy focus và trả focus về ảnh đã mở.
- Không đóng gallery khi bấm vùng overlay (`overlayClose: false`) để tránh người dùng mất ngữ cảnh. Liên kết ảnh vẫn giữ `href` và `target="_blank"` làm phương án dự phòng khi JavaScript không tải được.
- Ảnh bằng chứng trong child row DataTables phải khởi tạo Colorbox sau khi dòng chi tiết được render; không bind trước lên phần tử chưa tồn tại.
- Colorbox upstream đã được archive; dự án khóa phiên bản 1.6.4 và lưu kèm giấy phép MIT. Không tự động nâng phiên bản hoặc thay thư viện nếu chưa review tương thích, accessibility và bảo mật.
- Kích thước Colorbox 1.6.4 phải cấu hình bằng `%` hoặc pixel (`maxWidth: 92%`, `maxHeight: 86%`); không truyền đơn vị viewport `vw/vh` vì plugin cũ chỉ lấy phần số và có thể thu gallery xuống khoảng 92×86 px.

## Đồng bộ schema hồ sơ (30/07/2026)

- Model đăng nhập và hồ sơ phụ thuộc `Users.DateOfBirth`, `KocProfiles.AvatarUrl` cùng bốn cột pháp lý của `BrandProfiles`; thiếu một trong các cột này có thể làm truy vấn đăng nhập thất bại trước khi vào trang KOC.
- Migration chuẩn nằm trong chuỗi `database/sql`: chạy `94_add_profile_identity_and_media_schema.sql`, sau đó chạy `95_verify_profile_identity_and_media_schema.sql`.
- Script 94 chỉ bổ sung schema còn thiếu, có transaction và chạy lặp an toàn; không sửa hoặc xóa dữ liệu lịch sử. Script 95 chỉ đọc metadata nên không phát sinh lỗi biên dịch `Invalid column name` khi kiểm tra schema thiếu.
- 30/07/2026: Sửa quyền truy cập menu **Công việc → Lịch sử hợp tác**. Razor Page trước đó khai báo role `KOC` trong khi claim đăng nhập chuẩn của hệ thống là `Koc`; do đối chiếu role phân biệt hoa/thường nên người dùng bị chuyển về trang đăng nhập. Quy chuẩn: toàn bộ `[Authorize(Roles = ...)]` của khu vực KOC phải dùng chính xác `Koc`.
- 30/07/2026: Toàn bộ asset của layout KOC (Bootstrap, Font Awesome, DataTables, Colorbox, CSS/JS chung và CSS/JS KOC) dùng URL tĩnh thay cho URL fingerprint sinh lại theo mỗi lần build; document KOC cũng khai báo không cache HTML. Việc này ngăn tab đang mở gọi fingerprint cũ và rơi về giao diện HTML thô sau khi website được build/khởi động lại trong môi trường phát triển.
- 30/07/2026: `Program.cs` phải đăng ký `UseStaticFiles()` trước routing/`MapStaticAssets()`. URL asset ổn định của KOC được phục vụ trực tiếp từ `wwwroot` bằng static-file middleware; không dựa vào endpoint fingerprint của .NET trong môi trường development.
- 30/07/2026: Chuẩn URL tĩnh được áp dụng cho cả bốn layout Public, KOC, Brand và Admin. Bỏ tham chiếu `Topstar.Web.styles.css` do bundle scoped CSS này không được copy vào output development và endpoint trả 500; nội dung bundle chỉ là CSS template mặc định, không thuộc giao diện TopStar.

## Lịch sử hợp tác KOC (30/07/2026)

- Trang `/Koc/Collaborations` sử dụng jQuery DataTables bản offline và tải dữ liệu qua AJAX handler `?handler=Data`; không render danh sách card dài trực tiếp từ Razor.
- Bảng chỉ hiển thị thông tin phục vụ quét nhanh: chiến dịch, Brand, ngày xác nhận, trạng thái, số sự kiện và thao tác. Timeline chi tiết nằm trong child row, giúp trang gọn khi có nhiều lịch sử.
- Tìm kiếm theo chiến dịch/Brand, lọc trạng thái, chọn số dòng và phân trang dùng chung một DataTable. Footer bảng phải có padding, không chạm mép card và không tạo thanh cuộn ngang.
- Trạng thái nghiệp vụ phải hiển thị bằng tiếng Việt và màu theo ngữ nghĩa: xanh dương đang hợp tác, xanh lá hoàn tất, vàng chờ xử lý, đỏ đã hủy/từ chối.
- CSS/JS được tách riêng trong phạm vi KOC (`.koc-collaboration-*` và `wwwroot/js/koc/collaborations.js`), không thay đổi partial lịch sử dùng bởi Brand/Admin.
- Timeline trong child row dùng từng dòng sự kiện cô đọng, không trải nội dung thành ba vùng xa nhau. Loại sự kiện và trạng thái nằm cùng nhóm; ghi chú nằm kế bên; thời gian và vai trò thao tác nằm cùng cột meta bên phải. Mobile tự xếp thành một cột.
# Chuẩn màu sắc và typography KOC

- KOC sử dụng hệ màu riêng, chỉ scope trong `.dashboard-body-koc`; tuyệt đối không để Brand hoặc Admin kế thừa.
- Gradient nhận diện chính: `#fa61da → #ff8891 → #ffcd5a`. Hồng là màu hành động chính, coral là điểm nhấn, vàng chỉ dùng có kiểm soát cho active/cảnh báo.
- Các màu trạng thái nghiệp vụ vẫn giữ đúng ngữ nghĩa: xanh lá thành công, đỏ lỗi/sự cố, vàng chờ xử lý. Không đổi tất cả badge sang màu nhận diện.
- Cỡ chữ nền desktop là `15px`, mobile là `14px`. Nội dung bảng/form khoảng `14px`; chữ phụ không nhỏ hơn khoảng `12px`.
- Heading ưu tiên `700`, nội dung chính `400–500`, nhãn/giá trị quan trọng `600–650`. Không dùng tràn lan `800/850`, không làm chữ phụ quá nhạt.
- Các bảng KOC dùng cùng một chuẩn: header một dòng, nền hồng rất nhạt, dòng chẵn khác màu nhẹ, nội dung và phân trang có khoảng thở.
- Dashboard, Công việc, Đầu việc, Hồ sơ và Lịch booking phải dùng chung token nhận diện KOC; không tự hard-code xanh dương cho icon, active, nút hoặc card thông thường.
- Lịch booking dùng hồng/coral cho nhận diện và ngày hiện tại; trạng thái lịch vẫn dùng màu nghiệp vụ riêng. Tên lịch, thời gian, lịch sử và hướng dẫn không dùng cỡ chữ 10–11px.
- Các nhãn phụ, metadata, chú thích và badge có mức chữ thống nhất khoảng `.78–.84rem`; tiêu đề card dùng `650–700`. Chỉ số KPI lớn được phép nổi bật nhưng không dùng `800/850` cho mọi dòng chữ.
- CSS KOC dùng URL tĩnh `/css/koc/koc.css` kèm query version thủ công. Không dùng `asp-append-version` tại layout KOC vì pipeline `MapStaticAssets` đổi thành fingerprint path và đã gây trường hợp Chrome không áp dụng stylesheet dù endpoint trả `200`.

## Quy chuẩn ContentRoot và static assets (30/07/2026)

- Không được giả định thư mục làm việc hiện tại là thư mục dự án Web. Khi chạy trực tiếp `bin/.../Topstar.Web.exe` từ root repo, ASP.NET có thể nhận sai `ContentRoot/WebRoot`, khiến URL CSS/JS trả `200` nhưng nội dung rỗng và giao diện chỉ còn HTML thô.
- `Program.cs` phải xác định `ContentRootPath` bằng thư mục thực sự chứa đồng thời `Pages` và `wwwroot`; ưu tiên working directory hợp lệ, thư mục cạnh executable khi publish, sau đó mới dùng đường dẫn project của bản build development.
- `WebRootPath` phải được gắn rõ thành `<ContentRoot>/wwwroot`. Không để static-file middleware tự suy ra từ một working directory tùy ý.
- Script `devops/run-local.ps1` phải khởi động từng project với đúng `-WorkingDirectory`. Đây là lớp bảo vệ bổ sung; ứng dụng vẫn phải tự chạy đúng khi người phát triển mở trực tiếp executable.
- Kiểm tra bàn giao static assets không chỉ nhìn HTTP status. Các URL `/css`, `/js`, `/lib` quan trọng phải trả `200`, đúng `Content-Type` và `Content-Length` lớn hơn `0`.
