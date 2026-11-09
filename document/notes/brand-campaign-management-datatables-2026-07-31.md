# Brand - quản lý chiến dịch AJAX/DataTables

Ngày cập nhật: 31/07/2026

## Trạng thái tiếp nối

- Hồ sơ Brand, checklist hoàn thiện hồ sơ và Dashboard đã hoàn thành.
- Hạng mục Brand tiếp theo theo checklist là chuẩn hóa trang quản lý chiến dịch trước khi tiếp tục hoàn thiện luồng tạo/sửa và duyệt nội dung.

## Phần đã hoàn thiện

- `/Brand/Campaigns` dùng DataTables offline và tải dữ liệu bằng AJAX từ Razor Page handler `Data`.
- Endpoint áp dụng đúng giao thức server-side DataTables: `draw`, `start`, `length`, tổng bản ghi, tổng bản ghi sau lọc và dữ liệu trang hiện tại.
- Tìm kiếm theo tên chiến dịch/mã package, lọc trạng thái, sắp xếp và `Skip/Take` đều thực hiện tại service/SQL; không tải toàn bộ chiến dịch về trình duyệt.
- Số thứ tự được tính theo vị trí toàn bộ tập kết quả nên không bị bắt đầu lại từ 1 khi chuyển trang.
- Bốn KPI trạng thái được tính trên toàn bộ chiến dịch của Brand, không phụ thuộc trang DataTables đang xem.
- Gửi duyệt dùng modal cố định, AJAX, anti-forgery token, trạng thái loading và toast tiếng Việt.
- Quyền sửa/gửi duyệt/shortlist/duyệt nội dung tiếp tục lấy từ transition rule và trạng thái nghiệp vụ ở backend.
- Service submit hiện có tiếp tục ghi `CampaignStatusHistories` và `AuditLogs`.
- Chuyển logic thêm/xóa/reindex deliverable khỏi Razor sang `wwwroot/js/brand/campaign-editor.js`; không còn JavaScript đặc thù Brand đặt inline trong trang.
- CSS và JavaScript tiếp tục tách riêng trong `wwwroot/css/brand` và `wwwroot/js/brand`.

## Kiểm tra

- Build solution: thành công, 0 warning, 0 error.
- Test: 238/238 bài test đạt.
- Website chạy tại `http://localhost:5201`.
- Truy cập chưa đăng nhập tới `/Brand/Campaigns` trả 302 về `/dang-nhap`, đúng phân quyền.
- Đăng nhập bằng Brand seed chưa đủ checklist trả về trang hoàn thiện hồ sơ, đúng profile guard.
- Database local hiện không có Brand vừa `Approved` vừa đủ toàn bộ 12 tiêu chí, nên chưa thể QA DataTables qua trình duyệt bằng tài khoản hiện có mà không sửa dữ liệu nghiệp vụ.

## Database

Không có thay đổi schema và không phát sinh file SQL mới cho hạng mục này.

## Hạng mục Brand tiếp theo

Chuẩn hóa trang tạo/sửa chiến dịch thành form Brand riêng: validation nội tuyến tiếng Việt đầy đủ, bố cục section rõ ràng, trạng thái read-only, checklist phản hồi Admin và luồng lưu nháp/gửi duyệt nhất quán.

## Bổ sung theo QA Brand Demo

- Hồ sơ trạng thái `Approved` không còn hiển thị `ReviewNote` cũ dưới dạng cảnh báo vàng.
- Phản hồi hồ sơ chỉ hiển thị ở các trạng thái Brand cần hành động hoặc cần biết lý do: `NeedUpdate`, `Rejected`, `Locked`.
- Database local xác nhận Brand Demo có một chiến dịch `Launch Serum Q2`, trạng thái `Published`, liên kết đúng `BrandProfileId`.
- Tài khoản `brand@topstar.local` hiện có trạng thái tài khoản `Locked`; không tự ý mở khóa dữ liệu trong quá trình QA.
- Bổ sung trang `/Brand/Packages` và menu `Chiến dịch → Mua gói KOC`.
- Catalog gói dùng dữ liệu thật từ `IPackagePolicyAdminService.GetPublicAsync`, không hardcode giá/quyền chọn KOC.
- Nút `Chọn gói và tạo chiến dịch` mở form tạo chiến dịch với package và ngân sách tối thiểu của gói đã chọn.
- Không thay đổi schema và không có SQL mới cần chạy.

## Bổ sung sửa lỗi danh sách chiến dịch Brand

- Xác nhận endpoint `/Brand/Campaigns?handler=Data` trả đúng 13 chiến dịch seed; lỗi không nằm ở dữ liệu hoặc truy vấn.
- Nguyên nhân phía giao diện: các tệp DataTables dùng URL fingerprint của static web assets không thực thi trong phiên trình duyệt QA, làm `window.DataTable` và `jQuery.fn.DataTable` đều không được đăng ký.
- Chuyển ba tài nguyên DataTables/Brand Campaign sang URL vật lý kèm query version để vẫn kiểm soát cache nhưng tránh route fingerprint lỗi.
- JavaScript có trạng thái lỗi hiển thị trong bảng và toast nếu thư viện hoặc API không tải được; không còn tình trạng bảng trống mà không báo lỗi.
- QA trình duyệt sau sửa: DataTables wrapper đã khởi tạo, trang đầu hiển thị 10/13 chiến dịch, KPI bản nháp hiển thị 3 và dữ liệu tiếng Việt đúng.
- Build `Topstar.Web`: thành công, 0 warning, 0 error. Website chạy tại `http://localhost:5201`.
- Không thay đổi database cho bản sửa lỗi này; không có file SQL mới cần chạy.
## Form tạo/chỉnh sửa chiến dịch

- Form dùng các tab `Thông tin chung`, `Nội dung bàn giao`, `KOC mục tiêu`, `Yêu cầu thực hiện`.
- CKEditor dùng toolbar gọn, không hiển thị nhóm công cụ kỹ thuật như sửa mã nguồn.
- Cho phép tải ảnh JPG, PNG, WEBP tối đa 5 MB; ảnh được kiểm tra nội dung file và lưu tại `wwwroot/uploads/campaign-content`.
- Các nút điều hướng, thêm/xóa sản phẩm bàn giao và lưu nháp phải có icon rõ nghĩa.
- Validation form tuân thủ `ui-validation-standard.md`: không dùng browser bubble, hiển thị viền đỏ, icon trong trường và thông báo tiếng Việt; nếu lỗi thuộc tab khác phải tự mở tab đó và focus trường lỗi đầu tiên.
- Ngân sách hiển thị phân cách hàng nghìn theo `vi-VN`, còn giá trị gửi backend là số nguyên sạch.
- Không hiển thị mã kỹ thuật của gói cho Brand; mã và purchase ID chỉ giữ ở hidden để kiểm soát nghiệp vụ.
- Ngày bắt đầu/kết thúc là bắt buộc; ngày kết thúc phải bằng hoặc sau ngày bắt đầu.
- Nút submit dùng `Tạo chiến dịch` khi tạo mới và `Lưu thay đổi` khi chỉnh sửa.
- Bộ lọc `Tỉnh/thành` và `Nhóm nội dung` dùng multi-select có tìm kiếm; mục đã chọn hiển thị dạng chip, cho phép bỏ từng mục và vẫn bind danh sách `Guid` về backend.

## Yêu cầu thực hiện chiến dịch

- Tab riêng `Yêu cầu thực hiện` thay cho hai checkbox kỹ thuật booking/giao hàng.
- Hỗ trợ: review sản phẩm từ xa, review món ăn tại quán, review cửa hàng, trải nghiệm dịch vụ, tham dự sự kiện và livestream trực tuyến.
- Review tại địa điểm bắt buộc tên địa điểm, địa chỉ, người liên hệ, điện thoại và khung thời gian.
- Review sản phẩm từ xa bắt buộc mô tả sản phẩm/kiện hàng dự kiến gửi; livestream bắt buộc lịch dự kiến.
- Quyền lợi/chi phí hỗ trợ và yêu cầu quay/đăng nội dung được lưu riêng.
- Backend tự suy ra `RequiresBooking`/`RequiresShipment` để tương thích các workflow hiện có.
- Dữ liệu được đưa vào payload audit khi tạo/cập nhật chiến dịch và hiển thị cho Admin duyệt, KOC xem chi tiết.
- Schema được bổ sung bằng `database/sql/109_add_campaign_execution_requirements.sql`; file chạy lặp an toàn.
- `Nội dung bàn giao` là video/bài viết/livestream KOC phải nộp, không phải hàng hóa Brand gửi.
- Khi review từ xa, form chiến dịch không nhận hãng vận chuyển/mã vận đơn. Sau khi KOC được chốt, module `Vận hành → Giao nhận` tạo một work item theo từng participation; Brand tạo vận đơn, snapshot địa chỉ KOC, xuất kho và tracking tại đó.
## Bổ sung 31/07/2026 - sửa điều hướng Tuyển chọn KOC

- Sửa `/brand/shortlist` không còn trả 404 khi menu không truyền `id` chiến dịch.
- Khi chưa có `id`, trang hiển thị danh sách các chiến dịch đang tuyển KOC, đang thực hiện hoặc đã hoàn thành để Brand chọn đúng chiến dịch.
- Danh sách dùng DataTables AJAX theo chuẩn Brand: có thống kê, tìm theo tên, lọc trạng thái, chọn số dòng, sắp xếp, phân trang và trạng thái lỗi/rỗng.
- Bố cục bảng gồm STT, chiến dịch, thời gian, số KOC đăng ký, trạng thái tiếng Việt và thao tác xem danh sách.
- Làm lại màn hình chi tiết tuyển chọn: tiêu đề và nội dung thuần nghiệp vụ Brand, tổng quan số hồ sơ/đã xác nhận/trạng thái chốt, giải thích cơ chế tuyển chọn, thẻ hồ sơ KOC và trạng thái rỗng có hướng dẫn.
- Thay cách diễn đạt mang tính nội bộ như “Admin phân công” bằng “Topstar tuyển chọn KOC phù hợp”.
- Làm lại màn hình Brand duyệt nội dung: hiển thị tên chiến dịch, 4 chỉ số nghiệp vụ, tiến độ nghiệm thu, trạng thái rỗng có hướng dẫn và danh sách nội dung bàn giao theo thẻ rõ ràng.
- Trạng thái nội dung được Việt hóa; các nút xem, duyệt, yêu cầu sửa, từ chối và hoàn tất đều có icon, CSS tiếp tục tách riêng trong vùng Brand.
- Sửa lịch booking không còn dàn 31 ngày thành một hàng: lịch desktop dùng lưới 7 cột theo tuần, có ngày ngoài tháng, ngày hiện tại và sự kiện trong từng ngày; mobile chuyển sang danh sách theo ngày.
- Việt hóa trạng thái booking và đổi tiêu đề thành ngôn ngữ nghiệp vụ Brand; bổ sung icon điều hướng tháng và quy định số lần đổi lịch.
- Thu gọn chiều cao từng ô lịch tháng để cân đối phần lịch và danh sách công việc.
- Làm lại thẻ booking toàn chiều rộng: nhóm thông tin chiến dịch/KOC, ngày, khung giờ, số lần đổi lịch, trạng thái và thao tác trên cùng một cấu trúc; form đổi/hủy lịch và lịch sử mở ngay trong thẻ thay vì các liên kết rời rạc.
- Đồng bộ mẫu lịch Brand với lịch KOC: tiêu đề tháng bên trái, cụm điều hướng có nút “Hôm nay”, chú giải trạng thái theo màu, bảng lịch liền khối và sự kiện hiển thị trong ngày; chỉ thay màu nhận diện và nội dung phù hợp vai trò Brand.
- Form tạo/đổi/hủy booking bắt buộc dùng validation tùy biến: `novalidate`, viền đỏ, icon `circle-exclamation` trong ô và lỗi tiếng Việt ngay dưới trường; không dùng popup validation mặc định của trình duyệt.
- Kiểm tra bắt buộc thời gian bắt đầu/kết thúc, thời gian kết thúc phải sau bắt đầu, lý do đổi/hủy từ 10 đến 1.000 ký tự; backend vẫn kiểm tra lại toàn bộ quy tắc.
- Icon lỗi booking phải dùng `fa-solid fa-circle-exclamation`; không dùng biến thể `fa-regular` không tồn tại vì sẽ hiển thị ký tự ô vuông. Dòng lỗi dùng flex để icon và nội dung luôn thẳng hàng.
- Các khối đổi lịch, hủy lịch và lịch sử trong một thẻ booking hoạt động theo accordion: mỗi lần chỉ mở một khối, tránh kéo dài/chồng form. Nút gửi đề xuất mới chiếm trọn hàng; nút xác nhận hủy được căn cố định theo khối lý do.
- Chi tiết chiến dịch ở trạng thái không chỉnh sửa phải dùng giao diện đọc riêng, không hiển thị form input bị disable. Trạng thái được Việt hóa; brief được render HTML đã làm sạch thay vì hiện thẻ HTML thô; ngân sách, thời gian, bàn giao, KOC mục tiêu và yêu cầu thực hiện được chia thành khối nghiệp vụ.
- Làm lại trang Brand đánh giá KOC: 3 chỉ số tổng quan, tabs “Đánh giá KOC”/“Phản hồi từ KOC”, thẻ đánh giá rõ chiến dịch và KOC, trạng thái rỗng có hướng dẫn, hoàn toàn dùng từ “chiến dịch” thay cho “campaign”.
- Form chấm sao dùng validation tùy biến tiếng Việt và không gọi popup `required` mặc định của trình duyệt.
- Sửa lỗi `/brand/reports`: không `Join` KOC trước rồi `GroupBy` object tổng hợp vì EF Core không dịch được. Truy vấn phải tổng hợp metric theo `KocProfileId` trước, sau đó mới join hồ sơ KOC để lấy tên hiển thị; toàn bộ lọc/tổng hợp vẫn chạy phía database.
- Làm lại báo cáo Brand: Việt hóa toàn bộ KPI, nhóm độ phủ/tương tác/ROI, tổng quan tài chính, trạng thái rỗng và bảng hiệu quả KOC theo chuẩn giao diện Brand.
- Form đồng bộ chiến dịch dùng validation tùy biến có viền đỏ, icon và lỗi tiếng Việt; không dùng `required` mặc định của trình duyệt.
- Dropdown chọn chiến dịch trên báo cáo phải có ô tìm kiếm theo tên, trạng thái không có kết quả, đánh dấu lựa chọn hiện tại và đồng bộ giá trị về select gốc để giữ nguyên binding/validation phía server.
- Làm lại cổng tranh chấp Brand: thống kê tổng/đang xử lý/đã giải quyết, hướng dẫn khi nào nên tạo yêu cầu, danh sách hồ sơ và trạng thái rỗng. Tạo yêu cầu bằng modal có validation tùy biến cho chiến dịch, nhóm vấn đề, tiêu đề 5–200 và mô tả 10–2.000 ký tự.
- Ô chọn chiến dịch/KOC khi tạo tranh chấp dùng dropdown tìm kiếm theo cả tên chiến dịch và tên KOC, có trạng thái không tìm thấy và giữ nguyên validation/binding của select gốc.
- Khi có `id`, hệ thống giữ nguyên màn hình shortlist chi tiết và tiếp tục kiểm tra quyền sở hữu chiến dịch của Brand.
- Trạng thái trên màn hình chọn chiến dịch được Việt hóa; giao diện và CSS tiếp tục nằm riêng trong vùng Brand.
- Không thay đổi database, không có file SQL cần chạy.
## Cập nhật 01/08/2026 - Chi tiết yêu cầu hỗ trợ Brand

- Tách giao diện chi tiết yêu cầu hỗ trợ của Brand khỏi partial cũ dùng chung.
- Việt hóa trạng thái và nhóm yêu cầu; không hiển thị mã hệ thống như `Open`, `Shipment` trên giao diện.
- Bố trí lại thành hồ sơ hỗ trợ hai cột: nội dung/trao đổi/phản hồi và thông tin yêu cầu/tiến trình xử lý.
- Chuẩn hóa form phản hồi theo validation chung: không dùng validation mặc định trình duyệt; viền đỏ, icon `circle-exclamation` trong ô và thông báo tiếng Việt ngay dưới trường.
- Link bằng chứng chỉ chấp nhận `http://` hoặc `https://`; nội dung phản hồi yêu cầu từ 10 đến 2.000 ký tự.
- Lịch sử trạng thái và phản hồi tiếp tục được lưu qua `IDisputeService` và audit hiện có.
- CSS/JavaScript mới chỉ đặt trong tài nguyên Brand: `brand.css`, `brand-dispute-detail.js`.
## Cập nhật 01/08/2026 - Trung tâm thông báo Brand

- Thay giao diện inbox dùng chung bằng giao diện riêng cho Brand theo chuẩn portal.
- Việt hóa loại thông báo, không hiển thị mã kỹ thuật như `AccountStatus` và không công khai mã nguồn entity cho người dùng.
- Bổ sung tabs Chưa đọc/Tất cả/Đã lưu trữ, số chưa đọc, empty state, card phân biệt rõ thông báo mới.
- Nhóm thao tác Mở công việc/Đánh dấu đã đọc/Lưu trữ/Khôi phục trong từng card và bổ sung icon.
- Lịch sử trạng thái thu gọn theo từng thông báo; các thao tác vẫn dùng service hiện có và giữ nguyên audit/event history.
### DataTables cho thông báo Brand

- Danh sách card thông báo được quản lý bằng jQuery DataTables phía client.
- Có tìm kiếm theo tiêu đề/nội dung, chọn 5/10/20/50 dòng, thông tin tổng số và phân trang tiếng Việt.
- Giữ card một cột để nội dung và các nút nghiệp vụ không bị ép vào bảng nhiều cột; tabs trạng thái vẫn lọc dữ liệu từ service.
### Chuyển thông báo sang bảng

- DataTables hiển thị theo cột: STT, Thông báo, Nội dung, Thời gian, Trạng thái, Thao tác.
- Cho phép sắp xếp theo thời gian; STT tự tính lại theo trang và kết quả tìm kiếm.
- Lịch sử được giữ dạng thu gọn trong ô Nội dung; thao tác đặt riêng trong cột cuối.
- Bổ sung khoảng cách giữa toolbar, bảng và footer; tăng padding hàng/cột, tách rõ Trạng thái và Thao tác; sửa STT đánh lại tuần tự sau khi sắp xếp/phân trang.
- Phân trang DataTables dùng bảng màu teal đồng bộ header Brand; trạng thái active, hover và disabled được tách rõ.
### DataTables cho Tranh chấp và hỗ trợ

- Danh sách yêu cầu Brand chuyển từ card tĩnh sang bảng DataTables.
- Các cột: STT, Yêu cầu hỗ trợ, Chiến dịch và KOC, Ngày gửi, Trạng thái, Thao tác.
- Có tìm kiếm, chọn số dòng, sắp xếp theo ngày, phân trang tiếng Việt và tự đánh lại STT.
- Sửa icon tìm kiếm DataTables thông báo: neo trong mép trái input và căn giữa dọc, không còn văng lên viền phải.
### DataTables cho báo cáo KOC

- Bảng Hiệu quả theo KOC đã load jQuery DataTables.
- Có tìm kiếm KOC, chọn số dòng, sắp xếp metric, phân trang tiếng Việt và STT theo trang.
- Các cột số dùng giá trị thô trong `data-order` để sắp xếp chính xác thay vì chuỗi đã định dạng.
- Căn lại toolbar báo cáo: ô tìm kiếm neo trái, bộ chọn số dòng neo phải; loại bỏ `text-align:right` mặc định của DataTables.
### DataTables cho danh sách booking Brand

- Calendar tháng tiếp tục dùng component lịch; danh sách Lịch làm việc với KOC phía dưới đã load jQuery DataTables.
- Có tìm theo KOC/chiến dịch, chọn 5/10/20/50 dòng và phân trang tiếng Việt.
- Giữ nguyên toàn bộ form xác nhận, đổi lịch, hủy lịch, validation và lịch sử trong từng bản ghi.
- Bổ sung 5 card thống kê phía trên lịch tháng: tổng lịch, chờ xác nhận, đã xác nhận, đề xuất đổi lịch và đã hủy; dữ liệu lấy trực tiếp từ danh sách booking Brand.
### DataTables cho vận chuyển Brand

- Danh sách vận chuyển đã load jQuery DataTables với tìm kiếm KOC/chiến dịch/mã vận đơn, số dòng và phân trang.
- Việt hóa trạng thái vận chuyển, không hiển thị `Created`, `InTransit` trên giao diện Brand.
- Giữ nguyên form xuất kho, xử lý sự cố, checkpoint, ảnh bằng chứng và timeline trong từng bản ghi.
- Chuyển vận chuyển từ DataTables một cột chứa card sang bảng 6 cột thực: STT, KOC/chiến dịch, người nhận, vận đơn, trạng thái, xử lý.
- Bổ sung card thống kê Tất cả/Chờ xuất kho/Đang vận chuyển/Đã giao hàng/Có sự cố; card đồng thời là bộ lọc nhanh cột Trạng thái của DataTable.
- Xác nhận xuất kho và tải ảnh bằng chứng chuyển sang Bootstrap modal, không bung form trong ô bảng.
- Modal dùng validation chuẩn Brand: `novalidate`, viền đỏ, icon `fa-circle-exclamation` trong trường và thông báo ngay dưới; kiểm tra ảnh JPG/PNG/WEBP tối đa 5 MB và mô tả 10–500 ký tự.
- Tách card thống kê khỏi mô tả đầu trang; cột Xử lý chỉ còn nút icon xem. Modal hồ sơ vận chuyển chia tabs Tổng quan, Xử lý, Bằng chứng, Hành trình, Lịch sử.
- Chuẩn hóa file upload bằng dropzone: kéo-thả/chọn ảnh, preview, tên file, dung lượng và nút bỏ ảnh; vẫn áp dụng validation ảnh và thông báo lỗi chung.
- Lịch sử bằng chứng hiển thị dạng gallery card có ảnh, mô tả, người gửi, thời gian và nút xem ảnh; tách rõ khỏi khu vực tải ảnh mới.
### DataTables cho lịch sử hợp tác Brand

- Chuyển danh sách hợp tác tĩnh sang bảng DataTables có tìm kiếm, sắp xếp ngày, số dòng và phân trang.
- Việt hóa trạng thái; nút xem mở modal timeline tổng hợp với nhãn miền nghiệp vụ, người cập nhật và thời gian.
- Bổ sung 4 card thống kê: tổng lượt hợp tác, đang hợp tác, đã hoàn thành và tổng sự kiện đã ghi nhận.
- Tách tiêu đề, card thống kê và khung DataTables bằng khoảng cách đồng nhất; không còn dính section.
## Campaign Workspace — trung tâm điều hành chiến dịch (03/08/2026)

- Thêm `/Brand/CampaignWorkspace?id=<campaignId>` làm điểm vào chính khi Brand xem một chiến dịch.
- Workspace tổng hợp theo đúng campaign: giai đoạn hiện tại, việc tiếp theo, ngân sách, KOC chính thức, lịch cần xử lý, nội dung cần duyệt, ký quỹ, vận chuyển, tranh chấp và báo cáo.
- Luồng trạng thái phân biệt rõ mua gói và ký quỹ chiến dịch: shortlist chưa chốt → tuyển KOC; shortlist đã chốt/chưa có quỹ → cần thanh toán; yêu cầu đã gửi → chờ xác nhận; quỹ đã trả → thực hiện; hoàn tất → báo cáo/đánh giá.
- Tên chiến dịch và menu `Trung tâm điều hành` trong DataTable đều mở workspace; `Hồ sơ chiến dịch` được giữ riêng để xem brief và cấu hình gốc.
- Các nhóm chức năng KOC, tài chính, thực hiện, hỗ trợ được gom trong cùng một màn hình kèm bộ đếm và deep-link đúng `campaignId` khi trang đích hỗ trợ.
- `/Brand/Reports` nhận `campaignId` từ query string và tự chọn đúng chiến dịch.
- QA bằng Brand seed trên chiến dịch `Unbox bộ quà Beauty Box`: workspace HTTP 200, hiển thị 3/3 KOC và việc tiếp theo `Lập bảng thanh toán`.
- Build `0 warning / 0 error`; test `248/248` đạt. Không phát sinh SQL.
## Rà soát tiện dụng Campaign Workspace (03/08/2026)

- Không coi trang chi tiết chiến dịch là một danh sách liên kết thuần túy: bổ sung tiến trình 4 bước `Chốt KOC → Ký quỹ → Thực hiện → Nghiệm thu`, thể hiện rõ bước đã xong, bước hiện tại và bước chưa mở.
- Thống kê tuyển chọn hiển thị theo mục tiêu `đã xác nhận/số KOC mong muốn` và phần trăm hoàn thành; hồ sơ bị từ chối hoặc rút không được tính vào số hiệu lực.
- Hành động chính và các đường dẫn nghiệp vụ truyền đúng `campaignId` đang xem để các màn hình hỗ trợ có thể giữ ngữ cảnh chiến dịch, tránh Brand phải tìm lại từ danh sách tổng.
- Booking và vận chuyển đã nhận và lọc theo `campaignId`; thao tác xong giữ nguyên bộ lọc để Brand quay lại đúng chiến dịch thay vì rơi về danh sách tổng.
- Tách toàn bộ truy vấn tổng hợp khỏi Razor PageModel sang `ICampaignWorkspaceService` (Application/Infrastructure). Web chỉ điều phối và render, đúng quy tắc kiến trúc dùng chung cho Web/API.
- Không thay đổi database và không có SQL cần chạy cho đợt rà soát này.
- Kiểm tra kỹ thuật: build đạt 0 warning/0 error; 248/248 test đạt.

### Điều chỉnh tab nhiệm vụ và thao tác tại chỗ

- Navigation của Workspace là tab thật, không còn dùng anchor để cuộn tới section.
- Mỗi tab chỉ phục vụ một nhóm nhiệm vụ: `Tổng quan`, `KOC`, `Tài chính`, `Thực hiện`, `Hỗ trợ`.
- Các màn hình chuyên sâu được tải lười trong vùng làm việc cùng origin của tab; Brand thao tác form/modal ngay tại Workspace và không bị chuyển khỏi ngữ cảnh chiến dịch.
- Tab `Thực hiện` có các nhiệm vụ con Booking, Giao nhận, Nội dung, Báo cáo; tab `Hỗ trợ` có Tranh chấp, Đánh giá, Lịch sử, Hồ sơ.
- Khi màn hình nghiệp vụ chạy trong Workspace, layout tự chuyển sang chế độ embedded: ẩn header lặp, giữ đầy đủ CSS/JavaScript/modal và tự đồng bộ chiều cao với trang cha.
- Nút hành động chính chuyển trực tiếp tới tab phù hợp thay vì điều hướng sang một trang độc lập.
- Hash `#tab-{name}` lưu tab hiện tại; tải lại hoặc quay lại Workspace vẫn mở đúng khu vực công việc.
- Không được cố định iframe phủ toàn viewport khi mở modal: lớp iframe trong suốt sẽ chặn tương tác của Workspace và làm mất dữ liệu nền của tab. Modal giữ trong đúng vùng nhiệm vụ embedded; iframe chỉ tự đồng bộ chiều cao và không thay đổi stacking context của trang cha.
- Trong chế độ embedded, modal căn theo đầu vùng nhiệm vụ (`24px`) thay vì căn giữa theo toàn bộ chiều cao iframe; backdrop dùng phạm vi vùng nhiệm vụ và nút quay về danh sách tổng của màn hình con được ẩn.
- Modal embedded gửi trạng thái mở/đóng cho Workspace. Trang cha tạo backdrop cố định phủ toàn viewport ở lớp `z-index: 2000`; iframe hiện tại được nâng lên `2002` nhưng giữ nguyên vị trí và kích thước. Nhờ đó header/summary cũng được làm mờ, dữ liệu tab không biến mất và modal vẫn nhận tương tác.
- Trước khi khóa cuộn khi mở modal, Workspace lưu `window.scrollY` và cuộn iframe nhiệm vụ lên đầu vùng nhìn; tránh trường hợp bấm hành động ở cuối danh sách nhưng modal tuyệt đối nằm ngoài màn hình. Khi đóng modal, khôi phục đúng vị trí cuộn cũ.

### Ngữ cảnh chiến dịch trong màn hình nhúng

- Khi màn hình nghiệp vụ được mở từ Workspace, `campaignId` của Workspace là ngữ cảnh duy nhất.
- Màn hình Thanh toán chiến dịch nhúng phải ẩn tiêu đề lặp và bộ chọn chiến dịch; Brand thao tác trực tiếp trên chiến dịch đang mở.
- Bộ chọn chiến dịch chỉ giữ ở trang Tài chính độc lập để Brand có thể chuyển giữa các chiến dịch.

### Phạm vi và điều kiện đánh giá KOC

- Tab `Đánh giá` trong Workspace bắt buộc lọc cả đánh giá cần gửi và phản hồi nhận được theo đúng `campaignId` đang mở; không được hiển thị dữ liệu của chiến dịch khác.
- Chỉ chiến dịch `Completed` và lượt cộng tác `Completed` mới xuất hiện trong danh sách đánh giá. Backend tiếp tục kiểm tra lại điều kiện này khi gửi để không thể vượt quy trình bằng request trực tiếp.
- Chiến dịch chưa hoàn tất hiển thị trạng thái khóa với giải thích rõ; không render biểu mẫu chấm điểm.
- Sau khi gửi hoặc xử lý đánh giá, redirect phải giữ `campaignId` để không rơi khỏi Workspace hiện tại.

### Phạm vi lịch sử hợp tác trong Workspace

- Tab `Lịch sử` nhận `campaignId` và chỉ truy vấn các lượt cộng tác thuộc chiến dịch đang mở; không tái sử dụng nguyên dữ liệu danh sách lịch sử toàn Brand.
- Khi nhúng trong Workspace, ẩn tiêu đề và card thống kê tổng của trang độc lập để tránh lặp bố cục. Bảng KOC và timeline vẫn giữ đầy đủ thao tác chi tiết.
- Trang `Lịch sử hợp tác` mở độc lập không có `campaignId` vẫn hiển thị toàn bộ dữ liệu Brand và thống kê tổng.

### Lưu bảng phân bổ quỹ chiến dịch

- Khi tạo phiên bản quỹ `Draft` lần đầu, `TotalPayableSnapshot` phải được khởi tạo bằng `CampaignBudgetSnapshot`; phí nền tảng và phí cổng thanh toán chỉ được tính lại khi Brand gửi yêu cầu thanh toán.
- Điều kiện này bảo đảm bản nháp thỏa `CK_CampaignFundings_Amounts` nhưng chưa khóa snapshot phí trước thời điểm lựa chọn phương thức thanh toán.
- `DbUpdateException` khi lưu phân bổ phải được chuyển thành kết quả nghiệp vụ an toàn để UI hiển thị thông báo; không để exception làm iframe chuyển sang trang Error.
- Chế độ `Chia đều ngân sách` phải hiển thị ngay số tiền dự kiến của từng KOC dù chưa lưu bản nháp. Giá trị xem trước dùng trực tiếp `CampaignFundingRules.SplitEqual`, không tự tính lại bằng JavaScript để tránh sai phần dư.
- Khi chuyển qua lại giữa `Chia đều` và `Nhập riêng`, giao diện giữ riêng giá trị tùy chỉnh của Brand; ô chia đều ở trạng thái chỉ đọc nhưng không được để trống.
- Nhập thù lao riêng phải cập nhật tức thời `Đã phân bổ` và `Còn lại`, không cho một lần nhập làm tổng vượt ngân sách, đồng thời vô hiệu hóa nút lưu cho đến khi tổng khớp chính xác.
- Cung cấp thao tác nhanh `Chia đều nhanh`, `Điền phần còn lại` và `Xóa số tiền`; các thao tác này chỉ cập nhật bản nháp trên giao diện, Brand vẫn phải bấm lưu để ghi nhận và tạo audit.
- Thù lao chiến dịch hiển thị và nhập theo VND nguyên. Chia đều phân phối phần dư theo đơn vị 1 ₫ theo thứ tự KOC ổn định; không sinh phần thập phân để tránh xung đột với dấu phân cách hàng nghìn tiếng Việt.
- 03/08/2026: Sửa lỗi tạo yêu cầu thanh toán chiến dịch: tắt SQL `OUTPUT` cho các bảng quỹ có AFTER trigger và bổ sung bẫy lỗi an toàn tại handler. Chuyển khoản chuyển sang chờ Topstar đối soát; OnePAY chuyển sang bước chọn thẻ/QR và chỉ xác nhận qua callback/IPN hợp lệ.
- 03/08/2026: Thiết kế lại section thông tin thanh toán Brand: header/trạng thái rõ ràng, tổng chi phí cân đối, thông tin chuyển khoản dùng toàn chiều ngang, nội dung chuyển khoản nổi bật, hướng dẫn đối soát và nút hủy nằm trong footer cùng section.
- 04/08/2026: Bổ sung Brand gửi ảnh bằng chứng chuyển khoản (JPG/PNG/WEBP tối đa 5 MB, ghi chú 10–500 ký tự), lưu theo quỹ và audit; Admin xem được ảnh/ghi chú/thời điểm để đối soát. Cần chạy `database/sql/118_add_campaign_payment_proof.sql`. Đồng thời bổ sung khoảng cách dưới mô tả phân bổ thù lao.
- 04/08/2026: Chuẩn hóa câu chữ chứng từ chuyển khoản, rút gọn nút thao tác thành `Gửi`, đặt cùng hàng với ảnh và ghi chú; sửa icon validation textarea không còn rơi khỏi field.
