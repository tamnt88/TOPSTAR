# Đánh giá tính nhất quán website Admin / Brand / KOC

Ngày: 08/09/2026. Phạm vi: website; không đưa API/mobile vào kế hoạch.

## Phạm vi và mức xác minh

- Đối chiếu Razor PageModel, view, JavaScript, service lấy dữ liệu và tài liệu quy chuẩn.
- Rà các nhóm danh sách: tài khoản/hồ sơ, chiến dịch, gói dịch vụ, booking, giao nhận, đầu việc, đánh giá, thông báo, tài chính và lịch sử.
- Đây là đánh giá từ mã nguồn, chưa phải nghiệm thu trực quan từng màn hình trên trình duyệt hay đo tải với DB lớn. Không kết luận kích thước, màu sắc hoặc hiệu năng thực đo khi chưa kiểm tra runtime.
- Không sửa nghiệp vụ, giao diện hoặc database trong đợt đánh giá này. Không chạy lại test nghiệp vụ vì chỉ bổ sung tài liệu; kết quả 271 test đạt thuộc lần rà soát trước.

## Kết luận

Ba portal có cùng nền tảng nhưng chưa có một bộ thành phần và hành vi danh sách dùng chung. Đã có các điểm chuẩn hóa tốt theo module; phạm vi áp dụng chưa xuyên suốt ba portal. Cần thống nhất cả cách lấy dữ liệu, trạng thái bộ lọc, phản hồi lỗi và cách làm mới sau thao tác, bên cạnh CSS.

Admin có lớp chuẩn hóa phân trang riêng và các danh sách AJAX tốt. Brand còn nhiều bảng phân trang trong trình duyệt. KOC đã có nhiều danh sách phân trang tại server nhưng vẫn có ngoại lệ về số dòng, khởi tạo dữ liệu và làm mới trang.

## Phát hiện có căn cứ

### F01 — P1: Tìm kiếm thông báo bị giới hạn trong 100 bản ghi tải về

- `backend/src/Topstar.Infrastructure/Notifications/NotificationInboxService.cs:23`: `GetInboxAsync` áp dụng `Take(100)`; không có tham số trang hoặc từ khóa.
- `backend/src/Topstar.Web/Pages/Brand/Notifications.cshtml.cs`: lấy Inbox trong OnGet.
- `backend/src/Topstar.Web/wwwroot/js/brand/brand-notifications.js`: DataTables phân trang/tìm kiếm dữ liệu tại trình duyệt.
- Khi có hơn 100 thông báo phù hợp, tìm kiếm trên bảng không bao phủ phần còn lại. Số chưa đọc được đếm riêng trên DB nên có thể lớn hơn tập bản ghi đang duyệt.
- Đề xuất: lọc, đếm và phân trang tại DB; nếu chủ đích chỉ hiển thị một cửa sổ gần đây, phải ghi rõ phạm vi và có đường truy cập lịch sử.

### F02 — P1: Các cơ chế tải dữ liệu chưa được phân loại rõ

- Brand Campaigns dùng AJAX server-side; `OnGetAsync` chỉ kiểm tra truy cập và trả trang.
- Brand Bookings lấy toàn bộ danh sách theo phạm vi qua `ScheduleWorkflowService.LoadAsync`, kèm lịch sử, rồi DataTables phân trang tại client. Tải 10 dòng trên giao diện không tương đương chỉ truy vấn 10 dòng.
- Admin Package Policies dùng AJAX nhưng không bật server-side: AJAX không đồng nghĩa với phân trang ở DB. Danh mục nhỏ có thể phù hợp với cách này.
- Menu Management dùng GET và phân trang server-rendered.
- Đề xuất: danh sách tăng theo thời gian dùng server-side; danh mục nhỏ, hữu hạn có thể dùng client-side. Hai cách phải có cùng giao diện và hành vi.

### F03 — P2: Đầu việc KOC truy vấn danh sách hai lần lúc mở trang

- `backend/src/Topstar.Web/Pages/Koc/Tasks.cshtml.cs:28`: OnGet gọi `GetKocTaskPageAsync` để lấy cả danh sách và thống kê.
- `backend/src/Topstar.Web/wwwroot/js/koc/tasks.js:185`: khởi tạo DataTables server-side rồi gọi handler Data, handler lại gọi cùng service.
- Service thực hiện nhiều phép đếm, lấy trang và lịch sử. KPI được render từ lần đầu, bảng lấy từ lần sau.
- Đề xuất: một response cho bảng và summary như mẫu Admin Accounts; hoặc tách query summary gọn nếu cần server-render ban đầu. Không tải danh sách rồi bỏ để gọi lại ngay.

### F04 — P2: Kích thước trang có nhiều chuẩn xung đột

- `Topstar.Core/Common/Pagination/PaginationDefaults.cs`: mặc định 20, tối thiểu 20, lựa chọn 20/50/100.
- `wwwroot/js/brand/brand-bookings.js:64`, billing/collaborations/disputes/notifications/reports: 5/10/20/50.
- `wwwroot/js/koc/wallet.js:26`, Brand Shipments và nhiều trang Admin: 10/20/50.
- `Pages/Koc/Tasks.cshtml:49`, PageModel và DeliverableWorkflowService: 12/24/48, dù hiển thị bảng.
- Đề xuất chuẩn bảng: mặc định 20, lựa chọn 10/20/50/100, tối đa server 100. Đây là đề xuất cần áp dụng đồng thời ở HTML, JS, contract và service; không chỉ đổi dropdown. 12/24/48 chỉ giữ cho lưới thẻ khi có lý do về bố cục.

### F05 — P2: Bộ lọc có hành vi khác nhau và chưa giữ trạng thái đầy đủ

- Admin Accounts và Brand Campaigns: chọn trạng thái tải ngay; từ khóa dùng Enter/nút Lọc.
- KOC Tasks: submit form mới tải bộ lọc; đổi số dòng tải riêng.
- Brand Bookings: tìm kiếm mặc định DataTables trên tập dữ liệu đã tải.
- Các script Admin Accounts, Brand Campaigns và KOC Tasks không có cơ chế lưu đầy đủ trạng thái bảng qua URL/stateSave. KOC có đọc một số query lúc khởi tạo nhưng không đồng bộ lại URL khi người dùng thao tác AJAX.
- `wwwroot/js/koc/tasks.js:441`: nộp thành công gọi `location.reload()`; các lựa chọn chỉ nằm trong DOM có thể mất. Brand Bookings redirect sau thao tác chỉ giữ campaignId, không giữ Month hay trạng thái bảng.
- Đề xuất: từ khóa Enter/nút Lọc; dropdown áp dụng ngay; có Xóa lọc; đổi điều kiện về trang 1; URL giữ query/filter/sort/page/pageSize; làm mới sau thao tác giữ trạng thái và cập nhật KPI liên quan.

### F06 — P2: Pagination dùng chung chưa thực sự dùng chung

- `wwwroot/js/admin/admin.js:111`: normalizeAdminPagination chuẩn hóa icon và nhãn truy cập chỉ ở Admin.
- Brand/KOC vẫn có cấu hình riêng với Đầu/Cuối, ký tự mũi tên và CSS riêng.
- `Pages/Shared/_Pager.cshtml`: nhận RouteValues dạng dictionary nhưng chỉ truyền keyword/scope/visible; chưa tổng quát cho các bộ lọc khác. Hiện dùng tại Menu Management nên đây là hạn chế tái sử dụng, không khẳng định bộ lọc trang Menu đang bị mất.
- Menu Management có chọn pageSize trên toolbar; _Pager lại render thêm lựa chọn pageSize ở chân bảng.
- Đề xuất: một vị trí chọn số dòng, một footer chung, icon/nhãn/disabled/active/aria-current nhất quán. Truyền toàn bộ route values hợp lệ. Xử lý trang rỗng, hết trang, bàn phím và màn hình hẹp.

### F07 — P2: Loading, lỗi tải và làm mới chưa có quy ước chung

- Brand Campaigns có error handler hiển thị lỗi trong vùng bảng và toast.
- Admin Accounts có error handler/toast và tự ẩn processing.
- Admin Campaigns có processing/serverSide nhưng không có error handler riêng trong cấu hình; chưa thấy một lớp xử lý lỗi DataTables dùng chung cho cả ba portal.
- KOC Tasks báo lỗi bằng toast; sau một số thao tác reload bảng, sau nộp bài reload toàn trang.
- Đề xuất: loading ban đầu, đang làm mới, rỗng, không khớp bộ lọc, lỗi và hết phiên là các trạng thái riêng. Lỗi có nút Thử lại; không giả làm danh sách rỗng; không xóa bộ lọc. Không để phản hồi cũ ghi đè lựa chọn mới.

### F08 — P2: Giao ước tổng số bản ghi chưa đồng nhất

- Brand Campaigns trả riêng recordsTotal và recordsFiltered.
- KOC Tasks trả cả hai từ result.TotalCount, trong khi service đếm sau lọc. Không còn biểu diễn đúng tổng trước lọc theo giao ước DataTables.
- Đề xuất: trả tổng trong phạm vi được phép truy cập, tổng sau lọc, items và summary với phạm vi được định nghĩa rõ. Không tính tổng trên dữ liệu của người khác.

### F09 — P2: Format và validation còn lặp theo trang

- Có chuỗi tiền `đ` và `₫`; JavaScript có explicit vi-VN trong khi nhiều Razor dùng ToString("N0") theo culture hiện hành.
- Ngày giờ có ToLocalTime tại server và formatter Date tại client. Cần xác định múi giờ hiển thị chung; chưa kết luận runtime hiện đang lệch giờ.
- `wwwroot/js/brand/brand-bookings.js:9`: thêm icon vào dòng thông báo lỗi, trái tài liệu ui-validation-standard yêu cầu dòng lỗi không lặp icon.
- Nhiều renderer trạng thái được khai báo lại trong từng JS. Cùng mã trạng thái có thể cần nhãn nghiệp vụ theo vai trò, nhưng phải chung nguồn quy tắc và ngữ nghĩa màu.
- Đề xuất: formatter tiền, số, phần trăm, ngày giờ và trạng thái dùng chung; sort bằng giá trị gốc, không bằng chuỗi đã định dạng. Một cơ chế validation và submit loading dùng chung với màu theo portal.

## Bộ quy chuẩn đề xuất cho website

| Thành phần | Quy ước |
|---|---|
| Khung danh sách | Tiêu đề/mô tả và hành động chính; thống kê nếu cần; bộ lọc; bảng; footer |
| Bộ lọc | Label rõ ràng; từ khóa, trạng thái, khoảng ngày theo nghiệp vụ; Enter/Lọc; Xóa lọc |
| State | URL lưu trạng thái có thể chia sẻ; đổi lọc về trang 1; sau cập nhật giữ trang nếu còn hợp lệ |
| Số dòng | Mặc định 20; 10/20/50/100; chọn một vị trí; server giới hạn 100 |
| Footer | Hiển thị X–Y / Z kết quả; đầu/trước/số trang/sau/cuối; nhãn tiếng Việt, keyboard và aria |
| Server-side | Danh sách phát sinh theo thời gian; filter/order/count/Skip/Take ở DB, order có khóa phụ ổn định |
| Client-side | Danh mục nhỏ có giới hạn rõ; không dùng làm toàn bộ lịch sử khi server đã cắt dữ liệu |
| Loading/lỗi | Chỉ báo trong vùng bảng; lỗi kèm Thử lại; phân biệt chưa có dữ liệu và không khớp bộ lọc |
| Sau thao tác | Khóa nút khi gửi; tránh gửi lặp; giữ modal khi lỗi; thành công cập nhật bảng, KPI và badge liên quan |
| Format | vi-VN; một quy ước VND; dd/MM/yyyy, dd/MM/yyyy HH:mm; múi giờ nghiệp vụ xác định |
| Table | STT liên tục; số/tiền căn phải, thao tác nhất quán; dữ liệu gốc cho sort; nội dung người dùng được escape |
| Responsive | Bộ lọc xếp lại trên màn hình nhỏ; footer không tràn; bảng có cách đọc cột quan trọng rõ ràng |

Ba portal có thể giữ màu nhận diện và thông tin phù hợp vai trò. Bố cục, điều khiển và phản hồi của cùng loại thành phần cần nhất quán.

## Thứ tự triển khai đề xuất

1. Tạo thành phần danh sách dùng chung: filter state, DataTables defaults/adapter, pager, formatter, loading/error và action feedback. Dùng Razor handlers của website và service hiện có.
2. Áp dụng mẫu trên Admin Accounts, Brand Campaigns và KOC Tasks để chứng minh chuẩn chạy được ở cả ba portal.
3. Ưu tiên sửa giới hạn thông báo, luồng tải lặp và mất bộ lọc; chuyển các danh sách lịch sử lớn sang phân trang DB.
4. Nhân rộng theo nhóm: chiến dịch/tài khoản; giao nhận/booking/đầu việc; tài chính/thông báo/đánh giá; danh mục và bảng lịch sử trong modal.
5. Kiểm tra browser có đăng nhập trên cả ba portal và viewport nhỏ/lớn; kiểm tra với tập dữ liệu lớn và tình huống lỗi.

## Tiêu chí nghiệm thu

- 0/1/20/21 bản ghi và nhiều trang: số dòng, STT, tổng số và nút điều hướng chính xác.
- Lọc ở trang cuối, xóa dòng cuối, đổi pageSize: không rơi vào trang trống không có đường quay lại.
- Enter, Lọc, Xóa lọc, refresh, mở lại URL và quay lại từ chi tiết cho cùng trạng thái dự kiến.
- Từ khóa thông báo tìm được bản ghi nằm ngoài 100 kết quả đầu.
- Sau thao tác: bảng, KPI, badge cùng phản ánh dữ liệu mới; không reset bộ lọc ngoài chủ đích.
- Server chậm, mất mạng, 403/hết phiên và 500: có phản hồi riêng, không báo thành công giả hoặc rỗng giả.
- Ngày qua tháng/năm, tiền nhiều chữ số, giá trị thiếu: format và sort đúng.
- Màu, kích thước điều khiển, focus, nhãn truy cập và responsive được xác minh bằng browser, không chỉ build/test.

