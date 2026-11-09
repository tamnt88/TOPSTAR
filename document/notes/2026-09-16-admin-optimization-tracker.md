# Admin optimization tracker

Ngày bắt đầu: 16/09/2026

## Mục tiêu

- Tổ chức Admin theo công việc vận hành và mức độ ưu tiên.
- Gom cấu hình kỹ thuật vào một khu vực rõ ràng.
- Cho phép Admin quản lý email, Google Login và Facebook Login.
- Giữ nguyên nghiệp vụ Brand/KOC và không để CSS Admin ảnh hưởng hai portal này.

## Checklist

- [x] Rà soát menu, dashboard và 34 route Admin hiện có.
- [x] Chuẩn hóa nền giao diện Admin desktop/mobile, Roboto, màu `#6f36c8` và phân trang.
- [x] Chốt kiến trúc menu và nguyên tắc hàng đợi vận hành.
- [x] Tạo cấu hình đăng nhập Google/Facebook trong database.
- [x] Tạo màn hình Admin quản lý đăng nhập mạng xã hội.
- [x] Bật/tắt nút Google/Facebook trên trang đăng nhập và đăng ký.
- [x] Chặn yêu cầu OAuth ở backend khi nhà cung cấp bị tắt.
- [x] Đưa thông tin xác thực OAuth từ cấu hình Admin vào lúc khởi động ứng dụng.
- [x] Bổ sung kiểm tra SMTP và gửi email thử.
- [x] Đưa tài khoản email mặc định trong Admin vào luồng gửi email nghiệp vụ.
- [x] Sắp xếp lại menu Admin theo Công việc, Đối tác, Chiến dịch, Vận hành, Tài chính, Nội dung và Cấu hình.
- [x] Bổ sung hàng đợi xử lý chung, badge và cảnh báo SLA trên Dashboard.
  - [x] Tổng hợp escalation submission, Brand review và tranh chấp.
  - [x] Hiển thị Critical, chưa tiếp nhận, đang theo dõi và sự cố giao nhận.
  - [x] Hiển thị cảnh báo Critical trên Dashboard.
  - [x] Hiển thị badge động trên menu Công việc.
- [x] Bổ sung tìm kiếm toàn cục và ghi nhớ bộ lọc.
- [x] Bổ sung giám sát tình trạng tích hợp email, Google và Facebook trên Dashboard.
- [x] Rút gọn menu Admin về 7 mục chính và gom chức năng ít dùng vào trang Quản trị.

## Nguyên tắc cấu hình

- Google Login và Google/YouTube OAuth là hai chức năng độc lập.
- Mỗi nhà cung cấp có công tắc đăng nhập, đăng ký, Brand và KOC riêng.
- Khi tắt, giao diện phải ẩn nút và backend phải từ chối URL gọi trực tiếp.
- Client secret được lưu dạng rõ theo yêu cầu, nhưng không xuất hiện trong bảng hoặc log.
- Thay đổi credential có thể yêu cầu khởi động lại dịch vụ; trạng thái bật/tắt phải có hiệu lực ngay.

## Nhật ký thực hiện

### 16/09/2026 — Lần 1

- Hoàn thành rà soát toàn bộ menu và 34 route Admin.
- Hoàn thành lớp CSS Admin độc lập, responsive và chuẩn phân trang.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 25

- Hoàn thành màn hình CRUD riêng `/Admin/Settings/Partners` cho logo đối tác trên trang chủ.
- Admin có thể thêm, xem danh sách, sửa, xóa, bật/tắt hiển thị và thay đổi thứ tự logo.
- Bổ sung upload JPG/PNG/WEBP tối đa 5 MB, xem trước logo và kiểm tra dữ liệu trước khi lưu.
- Dữ liệu dùng chung với mục “Đối tác tin cậy” hiện tại nên trang chủ cập nhật ngay, không thay đổi giao diện Brand/KOC.
- Bổ sung lối vào “Logo đối tác” trong nhóm Nội dung của trang Quản trị và menu phân quyền Admin.
- Áp dụng script `141_add_admin_partner_logo_menu.sql` thành công.
- Kiểm tra route Admin chuyển đúng về đăng nhập khi chưa xác thực; trang chủ phản hồi HTTP 200.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.
- Website đang chạy tại `http://localhost:5201` và callback HTTPS tại `https://localhost:7161`.

### 16/09/2026 — Lần 26

- Chuyển khu vực “Đối tác tin cậy” trên trang chủ từ lưới tĩnh sang Swiper 11.
- Swiper tự chạy, lặp khi đủ logo, hỗ trợ kéo chuột/cảm ứng, tạm dừng khi rê chuột và có nút trước/sau.
- Responsive số logo hiển thị: 2 mobile, 3 mobile lớn, 4 tablet, 5 desktop nhỏ và 6 desktop lớn.
- Tiếp tục đọc trực tiếp danh sách, trạng thái và thứ tự logo từ CRUD Admin.
- Kiểm tra HTML thực tế có 14 slide, thư viện Swiper, CSS và JavaScript đều được nạp thành công.
- Build 0 lỗi/0 cảnh báo; kiểm thử hồi quy đạt `313/313`; website tiếp tục chạy tại `http://localhost:5201`.

### 16/09/2026 — Lần 27

- Xác định dải logo trong ảnh thuộc trang `/danh-cho-brand`, trước đó còn hard-code 5 logo riêng.
- Loại bỏ danh sách hard-code và nạp trực tiếp logo đang bật, đúng thứ tự từ CRUD “Logo đối tác” trong Admin.
- Chuyển dải logo Brand public sang Swiper: tự chạy, lặp khi đủ dữ liệu, kéo chuột/cảm ứng và có nút trước/sau.
- Responsive 2 logo trên mobile, 3–4 trên tablet và 5–6 trên desktop.
- Xác minh HTML runtime của `/danh-cho-brand` nhận đủ `14` logo từ dữ liệu quản trị; CSS/JS có version chống cache.
- Build 0 lỗi/0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 28

- Bỏ nền thẻ trắng, bo góc, viền và bóng phía sau từng logo trong Swiper đối tác của trang `/danh-cho-brand`.
- Giữ logo chạy trực tiếp trên nền chung của dải đối tác; hiệu ứng hover chỉ nâng logo và trả màu thương hiệu.
- Giữ nguyên dữ liệu động từ CRUD Admin, điều hướng Swiper và responsive.
- Build thành công với 0 lỗi, 0 cảnh báo; website chạy lại tại `http://localhost:5201`.

### 16/09/2026 — Lần 29

- Bỏ toàn bộ nền, viền, bo góc và bóng của từng logo đối tác trên cả trang chủ và `/danh-cho-brand`.
- Xóa hai nút mũi tên khỏi HTML và gỡ cấu hình navigation của hai Swiper.
- Giữ Swiper tự chạy, lặp, hỗ trợ kéo chuột/cảm ứng và tiếp tục nạp 14 logo từ CRUD Admin.
- Xác minh runtime không còn markup nút trái/phải trên cả hai trang; CSS/JS đã đổi fingerprint chống cache.
- Build thành công với 0 lỗi, 0 cảnh báo; website chạy tại `http://localhost:5201`.

### 16/09/2026 — Lần 30

- Gỡ hoàn toàn ô tìm kiếm nhanh khỏi header Admin trên desktop/mobile.
- Gom thông báo, tài khoản Admin và đăng xuất vào cùng một cụm hành động để header cân đối sau khi bỏ tìm kiếm.
- Chuẩn hóa icon thông báo 38×38 px, căn giữa icon theo cả hai trục và giữ badge chưa đọc đúng góc.
- Chỉ thay đổi header Admin, không ảnh hưởng header hoặc giao diện Brand/KOC.
- Build thành công với 0 lỗi, 0 cảnh báo; website phản hồi HTTP 200 tại `http://localhost:5201`.

### 16/09/2026 — Lần 31

- Rà soát và phân tách rõ “kích hoạt tài khoản” (xác minh email) với “duyệt hồ sơ” KOC.
- Bổ sung số liệu “Chưa kích hoạt” trên trang Quản lý KOC; dữ liệu hiện tại có 2/2 KOC chưa xác minh email.
- Hiển thị nhãn ngắn “Đã kích hoạt/Chưa kích hoạt” ngay dưới trạng thái truy cập của từng KOC.
- Thêm nút icon `user-check` tại cột thao tác cho KOC chưa kích hoạt; icon có title và aria-label đầy đủ.
- Thêm modal xác nhận kích hoạt, bắt buộc lý do 10–1.000 ký tự; Admin xác minh email thay KOC và lưu AuditLog.
- Sau khi thành công, bảng và thống kê cập nhật tại chỗ; nút kích hoạt tự biến mất.
- Giữ các luồng khóa/mở khóa, duyệt hồ sơ, bảo mật, vi phạm và sửa thông tin độc lập.
- Build 0 lỗi/0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`; website phản hồi HTTP 200.

### 16/09/2026 — Lần 32

- Sửa sai nghĩa giữa trạng thái tài khoản và trạng thái xác minh email trên trang Quản lý KOC.
- Xác nhận `tamnt.mcc` đăng nhập được vì `Users.Status = Active`; luồng đăng nhập hiện không bắt buộc `EmailConfirmed`.
- Đổi “Chưa kích hoạt” thành “Email chưa xác minh”; đổi nhãn từng dòng thành “Email đã/chưa xác minh”.
- Đổi nút và modal “Kích hoạt KOC” thành “Xác minh email”, làm rõ thao tác không thay đổi quyền đăng nhập Active/Locked.
- Giữ công tắc trạng thái tài khoản là nơi khóa/mở quyền truy cập thực tế.
- Không tự động khóa hoặc làm gián đoạn hai tài khoản KOC đang hoạt động.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; website chạy lại tại `http://localhost:5201`.

### 16/09/2026 — Lần 33

- Sửa lỗ hổng nghiệp vụ cho phép Brand/KOC đăng nhập bằng mật khẩu khi `EmailConfirmed = false`.
- Luồng đăng nhập nay chặn tài khoản chưa xác minh email, ghi nhận sự kiện bị chặn và hướng người dùng tới trang kích hoạt/gửi lại mã.
- Giữ tài khoản Admin không bị khóa bởi quy tắc xác minh email để bảo toàn quyền quản trị hệ thống.
- Đăng nhập Google/Facebook hợp lệ tự xác nhận email vì danh tính đã được nhà cung cấp OAuth xác thực.
- Loại bỏ việc tự cấp phiên đăng nhập ở luồng đăng ký nhanh KOC và đăng ký Brand; tài khoản mới phải xác minh email trước.
- Không thay đổi trạng thái Active/Locked, trạng thái duyệt hồ sơ hoặc giao diện quản trị Brand/KOC.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 34

- Thiết kế lại trang Kích hoạt tài khoản đồng bộ với giao diện Đăng nhập và Quên/Đặt lại mật khẩu.
- Chia hai cột trên desktop: hướng dẫn bảo mật bên trái và form OTP bên phải; mobile tự chuyển thành một cột gọn.
- Rút nút chính thành `Xác minh OTP`, bổ sung icon và căn giữa icon/text cho toàn bộ thao tác.
- Thu gọn phần gửi lại mã bằng vùng mở rộng; tách rõ email nhận mã, giới hạn gửi và nút `Gửi lại mã`.
- Bổ sung giao diện riêng cho trạng thái cảnh báo, nhập sai và xác minh thành công.
- Chuẩn hóa ô OTP 6 chữ số, liên kết `Quay lại đăng nhập` có icon và màu tím đồng bộ hệ thống.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 35

- Bổ sung khóa học công khai thứ tư để khu `Các khóa học đang mở` cân thành lưới 2×2 trên desktop.
- Khóa mới `Đo lường hiệu quả và phát triển kênh`, cấp độ Chuyên sâu, thời lượng 4 tuần, trạng thái Published.
- Dữ liệu được thêm vào CRUD Academy thật và có lịch sử tạo tương ứng, không dùng thẻ giao diện giả.
- Bổ sung sắc thái nhận diện cho thẻ thứ tư; giữ responsive một cột trên màn hình nhỏ.
- Thêm script idempotent `141_seed_fourth_academy_course.sql` để có thể triển khai lặp lại an toàn.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 36

- Sửa lỗi tương phản chữ tại section tối `Chuẩn vận hành TopStar` trên trang Học viện KOC.
- Loại bỏ rule cũ ép tiêu đề và mô tả trong hai card về màu tối; chuyển sang trắng và tím nhạt phù hợp nền tím đậm.
- Giữ icon màu phân biệt, card kính mờ và bố cục responsive hiện tại.
- Tăng version query của `academy.css` và `landing-polish.css` để trình duyệt tải ngay CSS mới, không giữ cache lỗi.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 37

- Thiết kế lại menu public trên mobile theo dạng panel gọn, rõ vùng chạm và cân đối khoảng cách.
- Thu chiều cao header và logo; chuẩn hóa nút hamburger 42×42 px với trạng thái focus rõ ràng.
- Chuyển từng mục menu thành hàng 48 px gồm icon, nhãn và mũi tên điều hướng.
- Thay đường active kéo dài bằng nền tím nhạt bo góc đúng phạm vi mục đang chọn.
- Tách nhóm tài khoản bằng đường phân cách và chia đều hai nút Đăng ký/Đăng nhập; màn hình rất nhỏ tự xếp một cột.
- Tăng version `public-theme.css` để trình duyệt không giữ cache giao diện cũ.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 38

- Bổ sung quyền xóa chiến dịch cho Admin ngay tại cột thao tác bằng nút icon ngắn gọn.
- Thêm modal xác nhận nêu rõ hậu quả, bắt buộc nhập lý do 10–1.000 ký tự và cảnh báo thao tác không thể hoàn tác.
- Xóa theo transaction toàn bộ dữ liệu phụ thuộc của chiến dịch: ứng tuyển, shortlist, KOC tham gia, bài nộp, bài đăng/chỉ số, giao nhận, lịch hẹn, tranh chấp, đánh giá, funding, invoice, ví và lịch sử liên quan.
- Ghi AuditLog sau khi xóa thành công; nếu còn ràng buộc bất thường, toàn bộ thay đổi được rollback và giao diện báo lỗi.
- Không tự động xóa dữ liệu hiện có; Admin phải chủ động mở modal và xác nhận từng chiến dịch.
- Tối ưu layout trang: giới hạn chiều rộng nội dung, chuẩn hóa shortcut, tab cuộn ngang, nhóm nút xem/xóa và modal responsive.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 39

- Rà soát toàn bộ luồng gửi email: kích hoạt tài khoản, gửi lại OTP, quên/đặt lại mật khẩu, thao tác bảo mật Admin, thông báo ví KOC và email gửi thử SMTP.
- Sửa nguyên nhân logo hỏng do email trỏ tới URL `localhost`, máy người nhận không thể truy cập.
- Template email nay lấy logo đang cấu hình tại `Admin → Nhận diện`, ưu tiên logo active mới nhất.
- Nhúng logo trực tiếp vào MIME email bằng Content-ID (CID), không phụ thuộc website có public Internet hay không.
- Có fallback về logo mặc định trong `wwwroot/images/logo.png`; URL HTTPS bên ngoài vẫn được hỗ trợ khi không phải địa chỉ loopback.
- Chuẩn hóa kích thước logo email 148 px, giữ đúng tỷ lệ và không làm vỡ header trên Gmail/Outlook/mobile.
- Email `Gửi thử` cũng dùng đúng template và cơ chế logo giống email nghiệp vụ để Admin kiểm tra chính xác.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### 16/09/2026 — Lần 3

- Sửa lỗi tiếng Việt của menu `Đăng nhập Google & Facebook` do `sqlcmd` đọc sai code page.
- Quy định chạy script có tiếng Việt bằng `sqlcmd -f 65001`.
- Hoàn thành nạp Client ID/App ID và Secret từ database trước khi đăng ký OAuth.
- Giữ `appsettings.json` làm fallback nếu migration chưa được áp dụng hoặc database chưa có credential.

### 16/09/2026 — Lần 4

- Hoàn thành kiểm tra khả năng kết nối SMTP Host/Port từ danh sách tài khoản email.
- Hoàn thành thao tác gửi email thử về chính địa chỉ của tài khoản được chọn.
- Không tự động gửi email trong build hoặc kiểm thử; chỉ gửi khi Admin chủ động bấm nút.
- Bổ sung thông báo thành công/thất bại và không ghi mật khẩu SMTP vào log.
- Kiểm tra runtime trang Email Accounts và hai handler chẩn đoán; HTTP 200.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 5

- Tạo nhóm `Công việc` cho hồ sơ, chiến dịch, đánh giá, ngoại lệ và tranh chấp đang chờ xử lý.
- Đổi `Tài khoản` thành `Đối tác`; đổi `Phân tích` thành `Báo cáo`; đổi `Cài đặt hệ thống` thành `Cấu hình`.
- Gom phí chiến dịch, ký quỹ và phương thức thanh toán vào `Tài chính`.
- Tách Học viện, trang chủ, SEO, mẫu thông báo và lĩnh vực KOC thành nhóm `Nội dung`.
- Ẩn các menu Tài khoản/Phân quyền cũ bị trùng.
- Chạy migration `139` bằng UTF-8 và kiểm tra toàn bộ thứ tự menu trong database.

### 16/09/2026 — Lần 6

- Bổ sung khối `Hàng đợi vận hành` trên Dashboard từ dữ liệu escalation thật.
- Tổng hợp ba nguồn SLA: KOC nộp bài quá hạn, Brand chậm duyệt và tranh chấp quá hạn.
- Phân tách số lượng Critical, chưa tiếp nhận và đang theo dõi.
- Bổ sung số sự cố giao nhận và cảnh báo ưu tiên khi có công việc Critical.
- Giữ liên kết xử lý về Bàn vận hành hiện có, không tạo nghiệp vụ song song.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 7

- Bổ sung truy vấn đếm nhẹ dành riêng cho badge, không tải chi tiết Bàn vận hành trên mọi trang.
- Hiển thị tổng escalation chưa giải quyết trên menu `Công việc`.
- Badge chuyển sang trạng thái đỏ khi có escalation Critical.
- Tooltip hiển thị số chưa tiếp nhận và số Critical.
- Badge được căn lại theo menu ngang desktop và menu dọc mobile.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 8

- Bổ sung ô tìm kiếm toàn cục trên header Admin.
- Tìm theo Brand, KOC, chiến dịch và tranh chấp; giới hạn tối đa 25 kết quả.
- Cho phép tra cứu bằng tên, email, điện thoại hoặc nội dung liên quan; không tìm dữ liệu bí mật.
- Bổ sung trang kết quả responsive và liên kết trực tiếp đến màn hình xử lý phù hợp.
- Trên mobile, ô tìm kiếm nằm trong vùng menu mở rộng để không chiếm chiều cao header.
- Bổ sung ghi nhớ bộ lọc an toàn theo từng trang bằng `sessionStorage`.
- Không lưu password, file, hidden field; query string rõ ràng luôn được ưu tiên hơn trạng thái ghi nhớ.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 9

- Kết nối `EmailSender` với tài khoản `Mặc định gửi` đang hoạt động trong database.
- Giữ cấu hình `appsettings` làm fallback khi Admin chưa chọn tài khoản mặc định.
- Áp dụng cấu hình Admin cho email kích hoạt, quên mật khẩu, OTP và các email nghiệp vụ dùng `IEmailSender`.
- Chặn đặt tài khoản mặc định ở trạng thái tắt.
- Yêu cầu đầy đủ SMTP Host, Port, Username và Password khi chọn làm tài khoản gửi mặc định.
- Chặn xóa tài khoản đang là mặc định gửi hoặc nhận; Admin phải chuyển mặc định trước.
- Không gửi email thật trong quá trình kiểm thử.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 10

- Bổ sung khối `Tình trạng tích hợp` trên Dashboard Admin.
- Cảnh báo khi chưa có tài khoản email gửi mặc định đang hoạt động hoặc thiếu cấu hình SMTP cơ bản.
- Hiển thị riêng trạng thái Google/Facebook: đang tắt, đã bật đủ credential hoặc đã bật nhưng còn thiếu credential.
- Liên kết trực tiếp từng cảnh báo đến đúng màn hình cấu hình để Admin xử lý nhanh.
- Không hiển thị Client Secret, SMTP Password hoặc dữ liệu nhạy cảm trên Dashboard.
- Responsive một cột trên tablet/mobile; màu trạng thái giữ trong phạm vi CSS Admin.
- Rà lại mã nguồn UTF-8: các chuỗi Dashboard là tiếng Việt hợp lệ; hiện tượng ký tự sai trong lần đọc trước đến từ PowerShell đọc file không chỉ định UTF-8.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 11

- Thay cây menu Admin nhiều nhóm/dropdown bằng 7 mục phẳng theo cách tổ chức của portal Brand.
- Menu chính mới gồm: `Tổng quan`, `Công việc`, `Brand`, `KOC`, `Chiến dịch`, `Tài chính`, `Quản trị`.
- Giữ badge hàng đợi trên `Công việc` và trạng thái active cho toàn bộ trang con liên quan.
- Tạo trang `Quản trị` làm điểm vào duy nhất cho báo cáo, giám sát, nội dung và cấu hình hệ thống.
- Chia trang Quản trị thành ba nhóm dễ quét: Báo cáo và giám sát, Nội dung, Cấu hình hệ thống.
- Loại bỏ dropdown nhiều tầng khỏi header; giao diện mobile tiếp tục dùng cùng một danh sách 7 mục.
- Không thay đổi route, quyền Admin hoặc nghiệp vụ của các màn hình hiện có.
- Không thay đổi header/menu Brand và KOC.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 12

- Sửa liên kết mục `KOC` trên menu chính: từ hàng đợi `Hồ sơ chờ duyệt` sang danh sách đầy đủ `/Admin/Accounts/KOC`.
- Sửa mục `Brand` trỏ rõ ràng đến `/Admin/Accounts/Brand` và chuẩn hóa trạng thái active cho cả hai mục.
- Sửa thẻ KPI KOC trên Dashboard để mở danh sách KOC đầy đủ.
- Giữ màn `Hồ sơ chờ duyệt` trong nhóm nghiệp vụ `Công việc`, đúng vai trò hàng đợi cần xử lý.
- Không thay đổi dữ liệu KOC, nghiệp vụ duyệt hồ sơ hoặc portal KOC.
- Build thành công, 0 lỗi và 0 cảnh báo.

### 16/09/2026 — Lần 13

- Rà soát lại toàn bộ luồng truy cập Brand/KOC từ menu, Dashboard, tìm kiếm toàn cục và màn duyệt hồ sơ.
- Xác nhận database hiện có `2 KOC`, cả hai đã duyệt; hàng đợi duyệt bằng `0` là đúng dữ liệu nhưng không phải danh sách KOC.
- Sửa kết quả tìm kiếm KOC đã duyệt để mở `/Admin/Accounts/KOC` thay vì màn duyệt hồ sơ.
- Chuẩn hóa kết quả tìm kiếm Brand về `/Admin/Accounts/Brand`.
- Cho danh sách tài khoản nhận tham số `query` từ URL và tự điền từ khóa vào bộ lọc.
- Xác nhận menu `Công việc` active tại màn hồ sơ chờ duyệt; menu `Brand` và `KOC` active tại đúng danh bạ tương ứng.
- Build thành công, 0 lỗi và 0 cảnh báo.
- Chạy toàn bộ test ứng dụng: `313/313` test đạt.

### 16/09/2026 — Lần 14

- Kiểm kê toàn bộ route Admin và đối chiếu điểm vào từ 7 mục menu chính.
- Phát hiện mục `Tài chính` chỉ mở Phí dịch vụ, làm thiếu điểm vào cho Ký quỹ, Đối soát và các chính sách.
- Tạo trang hub `Quản trị tài chính` gồm Ký quỹ chiến dịch, Đối soát và chi trả, Phí dịch vụ, Chính sách phí, Chính sách rút tiền và Phương thức thanh toán.
- Bổ sung lối tắt tại `Công việc` cho Hồ sơ chờ duyệt, Duyệt đánh giá, Tranh chấp và Vi phạm.
- Bổ sung lối tắt tại `Chiến dịch` cho Ghép KOC, Lịch sử hợp tác và Lịch chiến dịch.
- Bổ sung các cấu hình bị thiếu trong trang `Quản trị`: Chính sách gói KOC, Quản lý menu và Dữ liệu mẫu.
- Quy về một nhóm sở hữu rõ ràng: Lịch thuộc Chiến dịch; Phương thức thanh toán thuộc Tài chính; không lặp lại trong Quản trị.
- Sửa trạng thái active cho các trang con của Chiến dịch, Tài chính và Quản trị.
- Xác nhận các trang create/edit/delete là luồng con từ trang danh sách tương ứng, không cần xuất hiện trên menu chính.
- Build thành công, 0 lỗi và 0 cảnh báo; `313/313` test đạt.

### 16/09/2026 — Lần 15

- Chạy kiểm kê tĩnh cho toàn bộ `58` Razor Page Admin và tất cả liên kết `asp-page`.
- Phát hiện và sửa liên kết Email hệ thống trên Dashboard từ route sai `/Admin/SystemSettings/EmailAccounts` sang `/Admin/Settings/EmailAccounts`.
- Kết quả quét sau sửa: `0` liên kết `asp-page` sai và `0` liên kết cũ `/Admin/SystemSettings` còn sót.
- Đưa Thông báo thành biểu tượng chuông trực tiếp trên header giống portal Brand, kèm badge số chưa đọc.
- Loại Thông báo khỏi trang Quản trị để tránh một chức năng thuộc hai nhóm.
- Chuẩn hóa trạng thái active: chi tiết duyệt hồ sơ thuộc `Công việc`; bảo mật tài khoản làm sáng đúng `Brand` hoặc `KOC` theo loại hồ sơ.
- Không thay đổi dữ liệu, quyền hạn hoặc nghiệp vụ gửi/đọc thông báo.
- Build thành công, 0 lỗi và 0 cảnh báo; `313/313` test đạt.

### 16/09/2026 — Lần 16

- Rà soát riêng các Razor route có tham số bắt buộc `{id:guid}` và `{userId:guid}`.
- Phát hiện thẻ `Bảo mật tài khoản / Mật khẩu Admin` trỏ nhầm vào trang bảo mật Brand/KOC nhưng không truyền `userId`.
- Tạo trang `/Admin/MyAccount` dành riêng cho Admin đang đăng nhập.
- Hoàn thiện form đổi mật khẩu Admin với kiểm tra mật khẩu hiện tại, độ dài, chữ hoa, chữ thường, chữ số và xác nhận mật khẩu.
- Dùng lại nghiệp vụ `IAuthService.ChangePasswordAsync`, giữ nguyên audit/auth event hiện có.
- Đổi tên thẻ thành `Tài khoản Admin` để không nhầm với chức năng quản trị bảo mật tài khoản Brand/KOC.
- Chuẩn hóa trạng thái active của trang tài khoản Admin về nhóm `Quản trị`.
- Không tự động thay đổi mật khẩu hoặc thực hiện thao tác tài khoản trong quá trình kiểm thử.
- Build thành công, 0 lỗi và 0 cảnh báo; `313/313` test đạt.

### 16/09/2026 — Lần 17

- Kiểm tra cấu hình gửi mặc định trong database: `mail.topstar.vn:465`, SSL bật, tài khoản `info@topstar.vn` đang hoạt động và có mật khẩu.
- Xác nhận DNS, TCP cổng 465 và bắt tay TLS 1.3 tới máy chủ email đều thành công; chứng thư khớp `topstar.vn`.
- Xác định luồng cũ dùng `System.Net.Mail.SmtpClient`, không phù hợp với implicit TLS của cổng 465.
- Chuyển luồng gửi email nghiệp vụ sang MailKit `4.18.0`; cổng 465 dùng `SslOnConnect`, cổng 587 dùng `StartTls`.
- Chuyển đồng bộ công cụ `Kiểm tra máy chủ` và `Gửi email thử` trong Admin sang cùng cơ chế TLS.
- Không gửi email thử tự động trong quá trình kiểm tra; Admin vẫn chủ động bấm gửi thử khi cần.
- Thiết kế lại trang Quên mật khẩu theo layout đăng nhập TopStar, đồng nhất desktop/mobile, màu tím và trạng thái lỗi/thành công.
- Build thành công, 0 lỗi và 0 cảnh báo; `313/313` test đạt.

### 16/09/2026 — Lần 18

- Rút gọn nhãn nút Quên mật khẩu từ `Gửi liên kết đặt lại mật khẩu` thành `Gửi liên kết`.
- Sau khi gửi thành công, nhãn chuyển thành `Gửi lại liên kết` để phản ánh đúng hành động tiếp theo.

### 16/09/2026 — Lần 19

- Sửa URL đặt lại mật khẩu từ `https://localhost:7161/dat-lai-mat-khau` sang `http://localhost:5201/dat-lai-mat-khau`.
- Sửa đồng bộ URL kích hoạt tài khoản, website và logo trong mẫu email sang host/port local đang chạy.
- Sửa đường dẫn logo email thành `/images/logo.png`, đúng vị trí static asset hiện có.
- Rà lại mã nguồn: không còn URL email cấu hình cũ `localhost:7161` ngoài profile HTTPS hợp lệ trong `launchSettings.json`.

### 16/09/2026 — Lần 20

- Đồng bộ trang Đặt lại mật khẩu với layout mới của Đăng nhập và Quên mật khẩu.
- Rút nhãn nút chính từ `Cập nhật mật khẩu` thành `Đổi mật khẩu` và bổ sung icon chìa khóa.
- Bổ sung icon mũi tên cho liên kết `Quay lại đăng nhập`.
- Sau khi đổi mật khẩu thành công, ẩn form đã dùng và hiển thị nút `Đăng nhập` thay vì cho gửi lại token.
- Bổ sung hướng dẫn mật khẩu ở cột thông tin và giữ responsive desktop/mobile.

### 16/09/2026 — Lần 21

- Xác định lỗi Google `400: redirect_uri_mismatch` do Google Cloud Console chưa cho phép callback của website đang chạy.
- Callback đăng nhập Google local hiện tại: `http://localhost:5201/signin-google`.
- Bổ sung trường callback chỉ đọc và nút sao chép ngay trong cấu hình Google/Facebook của Admin.
- Hiển thị hướng dẫn vị trí cần khai báo callback tương ứng trên Google Cloud Console và Facebook Login.
- Không thay đổi Client ID/Secret hoặc cấu hình OAuth bên ngoài của người dùng.

### 16/09/2026 — Lần 22

- Cho phép Admin chỉnh sửa Redirect URI của Google và Facebook thay vì trường chỉ đọc.
- Bổ sung cột `RedirectUri NVARCHAR(1000)` vào `SocialLoginConfigurations` bằng migration `140`.
- Client Secret/App Secret chuyển sang input text thường, hiển thị đầy đủ theo yêu cầu.
- Redirect URI, Client ID và Secret được lưu nguyên văn trong database, không mã hóa và không che giá trị.
- Nạp Redirect URI từ database khi khởi động; middleware OAuth dùng cả host/port và callback path do Admin cấu hình.
- Kiểm tra URL bắt buộc là địa chỉ đầy đủ `http://` hoặc `https://` trước khi lưu.
- Công tắc bật/tắt vẫn có hiệu lực ngay; thay đổi Redirect URI/ID/Secret cần khởi động lại dịch vụ.
- Áp dụng migration thành công cho Google và Facebook.
- Build thành công, 0 lỗi và 0 cảnh báo; `313/313` test đạt.

### 16/09/2026 — Lần 23

- Bắt trực tiếp HTTP 302 từ handler đăng nhập Google để xác minh tham số OAuth thực tế.
- Ứng dụng đang gửi đúng `redirect_uri=http://localhost:5201/signin-google`.
- Client ID thực tế khớp Client ID đang hiển thị trong màn cấu hình Admin.
- Database cũng đang lưu đúng callback `http://localhost:5201/signin-google` và Google Login đang bật.
- Kết luận lỗi `400: redirect_uri_mismatch` còn lại nằm ở danh sách Authorized redirect URIs của đúng OAuth Client trên Google Cloud Console, không nằm trong middleware hoặc database TopStar.
- Cần thêm URI chính xác, không có dấu `/` cuối, vào đúng OAuth 2.0 Client ID rồi chờ Google cập nhật cấu hình.

### 16/09/2026 — Lần 24

- Kiểm tra trực tiếp bốn callback với endpoint OAuth của đúng Google Client hiện tại.
- Google trả `redirect_uri_mismatch` cho callback `http://localhost:5201` và `http://localhost:7161`.
- Google chấp nhận callback HTTPS; chuyển Google về `https://localhost:7161/signin-google`.
- Chuyển Facebook về `https://localhost:7161/signin-facebook` để đồng nhất callback bảo mật local.
- Giữ website chính tại `http://localhost:5201` và chạy thêm endpoint HTTPS `https://localhost:7161` dành cho OAuth callback.

## Trạng thái bàn giao sang cuộc trò chuyện mới

- Giao diện Admin dùng Roboto, màu chủ đạo `#6f36c8`; phân trang 34px desktop và 32px mobile.
- Menu Admin đã rút gọn còn 7 mục: Tổng quan, Công việc, Brand, KOC, Chiến dịch, Tài chính và Quản trị.
- Brand mở `/Admin/Accounts/Brand`; KOC mở `/Admin/Accounts/KOC`; hồ sơ chờ duyệt thuộc Công việc.
- Công việc có lối vào hồ sơ chờ duyệt, duyệt đánh giá, tranh chấp và vi phạm.
- Chiến dịch có lối vào ghép KOC, lịch sử hợp tác và lịch chiến dịch.
- Tài chính có trang hub riêng cho ký quỹ, đối soát/chi trả, phí dịch vụ, chính sách phí, chính sách rút tiền và phương thức thanh toán.
- Quản trị gom báo cáo, nội dung, email, Google/Facebook, giao nhận, website, menu và các cấu hình hệ thống.
- Thông báo nằm trực tiếp trên header, có badge chưa đọc.
- Dashboard có hàng đợi SLA và tình trạng tích hợp email/Google/Facebook.
- Email mặc định do Admin cấu hình đã được nối vào luồng gửi email nghiệp vụ.
- Google/Facebook có công tắc đăng nhập/đăng ký và phạm vi Brand/KOC; backend chặn khi tắt.
- Admin có trang `/Admin/MyAccount` để đổi mật khẩu của chính tài khoản đang đăng nhập.
- Đã quét 58 Razor Page Admin: không còn `asp-page` sai và không còn route cũ `/Admin/SystemSettings`.
- Database kiểm tra gần nhất có 2 KOC đã duyệt và 0 KOC chờ duyệt.
- Kiểm tra gần nhất: build 0 lỗi/0 cảnh báo, test `313/313` đạt.
- Website cần được giữ chạy tại `http://localhost:5201`.
- Tiếp tục ghi checklist và nhật ký vào chính tài liệu này sau mỗi nhóm thay đổi.
- Không làm ảnh hưởng giao diện hoặc nghiệp vụ Brand/KOC.
- Chốt đề xuất kiến trúc vận hành và cấu hình tích hợp.

### 16/09/2026 — Lần 2

- Bắt đầu nhóm cấu hình đăng nhập Google/Facebook.
- Xác nhận hiện trạng: credential đang đọc từ `appsettings.json`; chưa có công tắc Admin.
- Xác nhận email SMTP/IMAP đã có CRUD trong Admin nhưng chưa có kiểm tra kết nối/gửi thử.
- Tạo bảng `SocialLoginConfigurations` và seed Google/Facebook.
- Tạo trang `/Admin/Settings/SocialLogin` cùng menu trong Cài đặt hệ thống.
- Hoàn thành công tắc đăng nhập, đăng ký và phạm vi Brand/KOC.
- Hoàn thành ẩn/hiện nút social trên trang đăng nhập và đăng ký.
- Hoàn thành chặn OAuth trực tiếp ở backend khi provider hoặc role bị tắt.
- Build thành công, 0 lỗi và 0 cảnh báo.
