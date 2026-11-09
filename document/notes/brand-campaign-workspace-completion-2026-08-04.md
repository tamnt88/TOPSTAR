# Hoàn thiện Trung tâm điều hành chiến dịch Brand — 04/08/2026

## Kết quả rà soát

Màn hình `/Brand/CampaignWorkspace` phải xác định nhiệm vụ tiếp theo từ dữ liệu thực tế của từng chiến dịch, không chỉ dựa vào trạng thái kỹ thuật tổng quát.

Các lỗi đã xác định trên chiến dịch `Unbox bộ quà Beauty Box`:

- Quỹ đã ký ngày 04/08/2026 nhưng ngày bắt đầu là 30/08/2026; giao diện không được thông báo KOC đã bước vào giai đoạn thực hiện khi chưa đến ngày.
- Chiến dịch yêu cầu gửi sản phẩm cho 3 KOC nhưng chưa có vận đơn; việc tiếp theo phải là giao nhận, không phải duyệt nội dung.
- Ba nội dung đang `NotSubmitted`; không được tính là nội dung Brand đang chờ duyệt.
- Nút hành động ở Workspace phải mở đúng tab con thay vì luôn rơi vào Booking hoặc Tranh chấp.
- Sau khi ký quỹ, tab Tài chính phải tiếp tục hiển thị mã yêu cầu, phương thức, ngày thanh toán, mã đối soát, tổng phí và tiến độ nghiệm thu/hoàn tiền.

## Thứ tự ưu tiên nhiệm vụ

1. Hoàn thiện/chốt danh sách KOC.
2. Lập và thanh toán yêu cầu ký quỹ.
3. Xử lý tranh chấp hoặc sự cố giao nhận đang mở.
4. Tạo đủ vận đơn nếu chiến dịch cần gửi sản phẩm.
5. Xác nhận đủ lịch nếu chiến dịch cần booking.
6. Nếu chưa đến ngày bắt đầu: hiển thị `Đã ký quỹ – Chờ bắt đầu`.
7. Duyệt nội dung thực sự ở trạng thái `Submitted`.
8. Theo dõi nội dung `RevisionRequested` hoặc `NotSubmitted` bằng câu chữ riêng.
9. Khi hoàn thành: mở trực tiếp Báo cáo; việc nghiệm thu/giải ngân được theo dõi tại Tài chính.

## Quy chuẩn điều hướng

- Hash hỗ trợ cả tab chính và nhiệm vụ con, ví dụ `#tab-execution-shipments`, `#tab-execution-deliverables`, `#tab-execution-reports`, `#tab-support-profile`.
- Nút `Thực hiện ngay` và hành động trên header phải mở đúng nhiệm vụ con.
- Mỗi tab vẫn là một nhiệm vụ độc lập; không chuyển người dùng về danh sách tổng khi đang xử lý một chiến dịch.

## Database

Không thay đổi schema và không có file SQL mới trong phần hoàn thiện này.

## Kết quả triển khai và kiểm tra

- Đã tách `CampaignWorkspaceRules` để xác định giai đoạn và nhiệm vụ tiếp theo bằng dữ liệu KOC, ký quỹ, giao nhận, booking, nội dung và tranh chấp.
- Chiến dịch `Unbox bộ quà Beauty Box` hiện ưu tiên `Chuẩn bị sản phẩm` và mở thẳng `#tab-execution-shipments` vì còn thiếu 3 vận đơn.
- Khi đủ giao nhận/booking nhưng chưa tới ngày bắt đầu, Workspace hiển thị `Đã ký quỹ – Chờ bắt đầu`.
- `Submitted` mới được tính là nội dung Brand cần duyệt; `NotSubmitted` và `RevisionRequested` có thông báo theo dõi riêng.
- Tab Tài chính sau ký quỹ hiển thị mã yêu cầu, phương thức, ngày thanh toán, mã giao dịch, tổng phí, chứng từ nếu có và tiến độ nghiệm thu/hoàn tiền.
- `dotnet build backend/Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- `dotnet test backend/Topstar.sln --no-restore`: 253/253 test đạt; bổ sung 5 test quy tắc Workspace.
- Runtime có đăng nhập Brand: Workspace và Campaign Funding đều HTTP 200, không có trang lỗi; nội dung `Chuẩn bị sản phẩm`, `Tạo vận đơn`, deep-link `shipments`, `Thông tin ký quỹ` và `Tổng đã thanh toán` đều xuất hiện đúng.
- Website đang chạy tại `http://localhost:5201`.

### Rà soát vòng cuối 04/08/2026

- Thanh tiến trình không còn coi `Completed` là đã quyết toán: bước `Nghiệm thu` chỉ hoàn tất khi quỹ đã `Released` hoặc `Refunded`.
- Chiến dịch đã hoàn thành công việc nhưng quỹ còn `Paid`/`PartiallyReleased` được điều hướng về `Tài chính` với trạng thái `Chờ nghiệm thu và giải ngân`.
- Tranh chấp đang mở được ưu tiên trước quyết toán, tránh hiển thị chiến dịch là hoàn tất trong khi còn hồ sơ phải xử lý.
- Trước ngày bắt đầu hiển thị `Chờ bắt đầu`; chiến dịch còn thiếu giao nhận/booking hiển thị `Đang chuẩn bị`.
- Đã kiểm tra runtime bằng tài khoản Brand cho Workspace và 10 trang con: Shortlist, CampaignFunding, Bookings, Shipments, Deliverables, Reports, Disputes, Reviews, Collaborations, CampaignEdit; tất cả trả HTTP 200 và không có trang lỗi.
- Test nghiệp vụ: `255/255` đạt. Build solution: `0 warning`, `0 error`.

### Rà soát tính nhất quán dữ liệu

- Chỉ các hồ sơ `Applied`, `Shortlisted`, `Confirmed` được tính vào số hồ sơ hiệu lực/đăng ký. `NotSelected`, `Withdrawn`, `CancelledByAdmin` không còn làm tăng số lượng trên danh sách và Workspace.
- Shortlist chỉ được coi là chốt khi có thời điểm chốt và số KOC xác nhận đúng bằng `DesiredKocCount`; dữ liệu thiếu hoặc dư KOC sẽ quay lại nhiệm vụ tuyển chọn để xử lý.
- Trạng thái quỹ `Refunded` được coi là đã đi qua bước ký quỹ và hiển thị `Đã hoàn tiền`, không làm thanh tiến trình quay ngược về `Chưa hoàn tất`.
- Bổ sung kiểm thử cho toàn bộ trạng thái hồ sơ; tổng test hiện tại `261/261` đạt.

### Hoàn thiện giao nhận trong Workspace

- Không hiển thị mã kỹ thuật `NotCreated` cho Brand; trạng thái được Việt hóa thành `Chưa tạo vận đơn`.
- Card chuẩn bị giao nhận gộp đúng hai nhóm cần thao tác: `Chưa tạo vận đơn` và `Chờ xuất kho`, với nhãn `Cần tạo / xuất kho`.
- Bộ lọc card dùng biểu thức chính xác cho cả hai trạng thái, không ảnh hưởng các vận đơn đang giao, đã giao hoặc có sự cố.
- Hoàn thiện vòng đời thao tác trong modal giao nhận: tạo vận đơn, xuất kho, cập nhật hành trình và xử lý sự cố theo hướng tiếp tục vận chuyển hoặc hoàn hàng.
- Validation xử lý sự cố và hành trình hiển thị nội tuyến theo chuẩn chung; backend kiểm tra lại ghi chú xử lý từ 10 đến 1.000 ký tự.
- Đơn hoàn hàng được tính vào nhóm `Sự cố / hoàn hàng` và số việc cần xử lý của Campaign Workspace, tránh báo sai là đã hoàn tất giao nhận.
- DataTables vận chuyển chỉ cho chọn từ 10 dòng trở lên; không còn tùy chọn 5 dòng.

### Rà soát khép kín vòng đời giao nhận

- Hồ sơ cộng tác đã `Completed` nhưng chưa từng có vận đơn không còn xuất hiện như một việc có thể tạo mới; hồ sơ hoàn tất có vận đơn cũ vẫn được giữ để tra cứu lịch sử.
- Đơn `Đã hoàn hàng` cho phép Brand nhập lại nhà vận chuyển và mã vận đơn để gửi lại, thay vì bị kẹt vĩnh viễn ở nhóm ngoại lệ.
- Form cập nhật checkpoint được đặt trong tab `Hành trình`; tab `Xử lý` chỉ giữ đúng thao tác nghiệp vụ của trạng thái hiện tại.
- Admin và Brand dùng chung quy tắc ghi chú xử lý sự cố từ 10 đến 1.000 ký tự; select trong modal nhận cùng kiểu validation nội tuyến với input và textarea.
- Thống kê KOC gộp cả sự cố và hoàn hàng dưới nhãn `Ngoại lệ giao nhận`.
- Thay đổi vòng cuối không yêu cầu SQL mới.
- Build toàn solution đạt `0 warning`, `0 error`; bộ kiểm thử đạt `262/262`; website đã chạy lại tại `http://localhost:5201`.
