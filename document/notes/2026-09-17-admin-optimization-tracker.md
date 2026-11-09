# Nhật ký tối ưu Admin — 17/09/2026

### Lần 1 — Làm rõ thao tác xác minh email thay KOC

- Đổi tiêu đề modal thành `Xác minh email thay KOC` để phân biệt với KOC tự nhập OTP.
- Hiển thị nút `Xác minh` kèm icon ngay trong cột thao tác trên desktop; mobile giữ icon gọn và tooltip đầy đủ.
- Bổ sung điều kiện sử dụng: Admin chỉ thực hiện sau khi đã đối chiếu đúng chủ tài khoản qua kênh hỗ trợ.
- Nêu rõ kết quả: KOC được phép đăng nhập bằng email/mật khẩu sau khi xác minh.
- Nêu rõ giới hạn: thao tác không duyệt hồ sơ và không thay đổi trạng thái khóa/mở tài khoản.
- Đổi `Lý do xác minh` thành `Căn cứ xác minh`, cung cấp ví dụ cụ thể và thông báo dữ liệu được lưu vào Nhật ký kiểm toán.
- Căn lại modal và hai nút hành động trên desktop/mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### Lần 2 — Tối ưu bảng quản trị KOC theo góc nhìn vận hành

- Bỏ nút `Hồ sơ chờ duyệt` khỏi header vì chức năng này đã có trong menu Công việc, tránh lặp luồng vận hành.
- Đổi ba cột trạng thái thành `Truy cập`, `Hồ sơ`, `Vi phạm`; chỉ hiển thị icon màu đồng kích thước, có tooltip và aria-label đầy đủ.
- Cột Truy cập giữ công tắc khóa/mở và icon email đã/chưa xác minh; bỏ toàn bộ nhãn dài trong từng dòng.
- Cột Hồ sơ dùng icon tương ứng Đã duyệt/Chờ duyệt/Cần bổ sung/Từ chối/Đã khóa; bấm icon để mở hồ sơ xử lý.
- Cột Vi phạm dùng icon khiên xanh nếu sạch và cảnh báo đỏ kèm số lượng nếu còn vi phạm hiệu lực.
- Cột cuối đổi từ `Thao tác` thành `Hỗ trợ`, chỉ giữ một nút ba chấm để bảng thoáng và nhất quán.
- Sắp lại menu hỗ trợ theo nghiệp vụ Admin: Tổng quan, Bảo mật & hỗ trợ, Hồ sơ & xét duyệt, Sửa thông tin, Xác minh email thay KOC, Vi phạm và khóa/khôi phục tài khoản.
- Giữ chức năng xác minh thay KOC trong menu có nhãn đầy đủ, không còn nút riêng bị ép/cắt chữ.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### Lần 3 — Sửa icon trạng thái bị trống

- Xác định hai glyph `fa-envelope-circle-exclamation` và `fa-shield-check` không được bộ Font Awesome hiện tại render ổn định.
- Đổi trạng thái email chưa xác minh sang icon phong bì `fa-envelope` màu vàng.
- Đổi trạng thái không có vi phạm sang icon khiên `fa-shield-halved` màu xanh.
- Đồng bộ icon khiên tại trang công khai Brand để không tái diễn ô icon trống ở khu vực khác.
- Giữ nguyên tooltip, aria-label và ý nghĩa màu trạng thái.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### Lần 4 — Gom thao tác KOC vào Trung tâm dạng tab

- Bỏ menu ba chấm dài tại từng dòng; thay bằng một nút `Trung tâm` có icon thống nhất.
- Gom nghiệp vụ vào một cửa sổ duy nhất với 5 tab: `Tổng quan`, `Hồ sơ`, `Bảo mật & hỗ trợ`, `Vi phạm`, `Lịch sử`.
- Tab Tổng quan chỉ giữ số liệu cần quét nhanh và nút sửa thông tin, không lặp lại liên kết của các tab khác.
- Đưa xác minh email thay KOC và khóa/khôi phục tài khoản vào tab Bảo mật & hỗ trợ.
- Giữ các thao tác nhạy cảm trong modal xác nhận riêng có căn cứ để tránh Admin bấm nhầm.
- Xử lý chuyển tiếp modal để không bị chồng lớp phủ khi mở xác minh hoặc khóa từ Trung tâm.
- Bổ sung responsive cho thanh tab, nhóm nút và nút Trung tâm trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### Lần 5 — Chuyển Trung tâm KOC từ modal sang nội tuyến

- Sửa lại đúng yêu cầu `toàn bộ trong một trang`: Trung tâm KOC không còn mở bằng modal phủ màn hình.
- Đưa khu vực chi tiết dạng tab xuống ngay dưới bảng tài khoản và tự cuộn tới vị trí sau khi chọn KOC.
- Giữ 5 tab nghiệp vụ trên cùng trang: Tổng quan, Hồ sơ, Bảo mật & hỗ trợ, Vi phạm và Lịch sử.
- Thêm nút đóng gọn cho panel nội tuyến; không tạo backdrop hoặc khóa thao tác trên trang.
- Chỉ giữ modal xác nhận cho các hành động nhạy cảm như xác minh email, khóa/khôi phục và sửa dữ liệu.
- Bổ sung bố cục responsive cho panel nội tuyến trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; JavaScript hợp lệ; kiểm thử hồi quy đạt `313/313`.

### Lần 6 — Tách Trung tâm KOC thành trang toàn màn hình

- Sửa đúng cách hiểu nghiệp vụ: nút Trung tâm chuyển sang một URL quản trị riêng cho từng KOC, không mở modal và không bung panel dưới danh sách.
- Trang chi tiết sử dụng toàn bộ vùng nội dung Admin, có nút quay lại danh sách rõ ràng.
- Giữ 5 tab Tổng quan, Hồ sơ, Bảo mật & hỗ trợ, Vi phạm và Lịch sử trên cùng một trang.
- Tải dữ liệu tài khoản trực tiếp phía máy chủ để URL có thể tải lại, đánh dấu và chia sẻ nội bộ.
- Giữ liên kết đến các màn hình chuyên sâu đối với xét duyệt hồ sơ, bảo mật và quản lý vi phạm.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 7 — Hoàn thiện nội dung vận hành Trung tâm KOC

- Bổ sung header hồ sơ có trạng thái tài khoản, trạng thái duyệt và xác minh email bằng tiếng Việt.
- Thêm các KPI người theo dõi, hợp tác hoàn thành, điểm hồ sơ và vi phạm hiệu lực.
- Thêm khu vực thông tin vận hành, danh sách việc cần chú ý và cảnh báo hỗ trợ KOC.
- Bổ sung kênh mạng xã hội, lịch sử hợp tác, phiên đăng nhập gần đây, vi phạm và audit theo từng tab.
- Đưa thao tác cập nhật hồ sơ và hỗ trợ tài khoản lên header để Admin xử lý nhanh.
- Hoàn thiện responsive cho KPI, thông tin, mạng xã hội và nhóm nút trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 8 — Hoàn thiện đầy đủ tab Hồ sơ KOC

- Bổ sung thông tin cá nhân, liên hệ, khu vực và địa chỉ giao nhận.
- Hiển thị danh mục nội dung và ghi chú xét duyệt của Admin.
- Bổ sung thông tin hình thể, tình trạng gia đình và con nhỏ phục vụ targeting chiến dịch.
- Bổ sung kinh nghiệm, Brand từng hợp tác và liên kết case study.
- Hiển thị năng lực viết kịch bản, quay, dựng, livestream và thiết bị sản xuất.
- Bổ sung mong muốn công việc, ngân sách tối thiểu, khu vực ưu tiên và khả dụng.
- Hiển thị portfolio với chỉ số nội dung, thư viện media, kênh mạng xã hội và lịch sử hợp tác.
- Các nhóm chưa có dữ liệu đều có trạng thái rỗng rõ ràng, không để khoảng trắng khó hiểu.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 9 — Nâng cấp trình bày hồ sơ KOC chuyên nghiệp

- Thiết kế lại header thành profile hero có avatar, nhãn loại hồ sơ và trạng thái vận hành.
- Thêm chỉ số phần trăm hoàn thiện hồ sơ cùng progress bar và bốn số liệu chất lượng.
- Việt hóa giá trị giới tính, tình trạng hôn nhân và các trạng thái nghiệp vụ.
- Tăng phân cấp tiêu đề bằng kicker/icon, giảm cảm giác form và các khung đồng cấp.
- Chuẩn hóa màu, bóng đổ, khoảng trắng và responsive cho desktop/mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 10 — Bổ sung hành động nhanh theo ngữ cảnh

- Thêm thanh hành động nhanh ngay dưới header hồ sơ để giảm số lần tìm tab/menu.
- Nút hồ sơ tự đổi giữa `Xét duyệt hồ sơ` và `Cập nhật hồ sơ` theo trạng thái.
- Nút bảo mật ưu tiên `Xác minh email` khi KOC chưa xác minh, nếu đã xác minh thì hiển thị `Bảo mật`.
- Thêm đường tắt đến Vi phạm, Lịch sử và sao chép email; vi phạm hiệu lực có badge số lượng.
- Giữ bước xác nhận ở màn hình chuyên sâu cho thao tác nhạy cảm, tránh khóa/xác minh nhầm chỉ bằng một lần bấm.
- Responsive nhóm hành động thành lưới hai cột trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 11 — Hoàn thiện Bảo mật & hỗ trợ ngay trong Trung tâm KOC

- Loại bỏ nút điều hướng sang trang bảo mật khác; toàn bộ nghiệp vụ chính được xử lý ngay trong tab.
- Bổ sung tổng quan bảo mật bốn màu: email, điện thoại, lần đăng nhập gần nhất và quyền truy cập.
- Thêm biểu mẫu xác minh email thay KOC, khóa/khôi phục tài khoản và thu hồi toàn bộ phiên đăng nhập.
- Thêm ghi chú hỗ trợ nội bộ và danh sách ghi chú để nhiều Admin phối hợp liên tục.
- Mọi thao tác nhạy cảm đều yêu cầu lý do/căn cứ và sử dụng service audit hiện có.
- Bổ sung thông báo kết quả sau thao tác và responsive cho desktop/mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 12 — Tối ưu trải nghiệm biểu mẫu thao tác nhạy cảm

- Không hiển thị trạng thái lỗi `Trường này là bắt buộc` ngay khi form vừa được mở.
- Nút xác nhận mặc định bị vô hiệu hóa và chỉ bật khi nội dung đạt độ dài tối thiểu.
- Thêm bộ đếm ký tự, chỉ dẫn số ký tự tối thiểu và dấu bắt buộc rõ ràng.
- Dùng trạng thái viền/đổ bóng nhẹ khi form đã đủ điều kiện gửi.
- Giữ kiểm tra nghiệp vụ phía máy chủ để không làm giảm an toàn thao tác.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 13 — Sửa nguồn dữ liệu lịch sử đăng nhập

- Xác định website dùng cookie authentication nên lượt đăng nhập được ghi vào `AuthEvents` và `LastLoginAtUtc`, không tạo `AuthSessions` token.
- Đổi danh sách thành `Lịch sử đăng nhập gần đây` và đọc các sự kiện Login thành công thực tế.
- Hiển thị IP, thời điểm và user-agent của từng lượt đăng nhập.
- Phân biệt rõ `phiên token có thể thu hồi` với lượt đăng nhập cookie trên website để tránh hiểu sai số liệu.
- Giữ thao tác thu hồi cho các phiên token thực sự tồn tại và tự khóa nút khi không có phiên.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 14 — Thêm lọc và phân trang lịch sử đăng nhập/ghi chú

- Lịch sử đăng nhập hỗ trợ tìm theo IP hoặc trình duyệt và lọc Thành công/Thất bại/Bị chặn.
- Ghi chú hỗ trợ tìm được theo nội dung hoặc email Admin.
- Hai danh sách có phân trang độc lập và lựa chọn 5/10/20 dòng.
- Bộ lọc và phân trang chạy tại chỗ, không tải lại toàn bộ trang hoặc làm mất tab hiện tại.
- Chuẩn hóa nút phân trang 34px trên desktop và 32px trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 15 — Tăng typography và sửa mũi tên select

- Tăng cỡ chữ nền riêng cho toàn bộ trang Trung tâm KOC lên mức dễ đọc hơn.
- Tăng cỡ chữ tiêu đề hồ sơ, tab, bộ lọc và nội dung điều khiển.
- Tăng chiều cao input/select của bộ lọc lên 40px.
- Dùng mũi tên select tùy chỉnh, cách mép phải 12px và thêm padding để chữ không chồng icon.
- Giới hạn CSS trong Trung tâm KOC, không ảnh hưởng giao diện Brand và KOC công khai.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 16 — Hợp nhất file hồ sơ và xét duyệt KOC

- Bỏ chỉ số `100% chất lượng` dễ gây hiểu nhầm; thay bằng trạng thái duyệt và số nhóm dữ liệu đã khai báo.
- Xác định tab Hồ sơ là file hồ sơ KOC đầy đủ, không còn nút mở thêm trang xét duyệt khác.
- Đưa ba thao tác Duyệt hồ sơ/Yêu cầu bổ sung/Từ chối vào cuối file hồ sơ.
- Mỗi quyết định yêu cầu căn cứ tối thiểu 10 ký tự và sử dụng quy trình audit hiện có.
- Nút header và hành động nhanh chuyển trực tiếp đến tab Hồ sơ trên cùng trang.
- Đa dạng màu theo nhóm dữ liệu: định danh, danh mục, hình thể, kinh nghiệm, năng lực, mong muốn và KPI.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 17 — Đưa xét duyệt lên thanh Hành động nhanh

- Đưa nút `Xét duyệt` lên thanh Hành động nhanh để Admin truy cập ngay từ đầu trang.
- Bỏ khối biểu mẫu xét duyệt dài ở cuối file hồ sơ.
- Nút mở modal tập trung với trạng thái hiện tại, độ phủ dữ liệu, điểm hồ sơ và số kênh xã hội.
- Modal giữ ba quyết định Duyệt/Yêu cầu bổ sung/Từ chối và bắt buộc nhập căn cứ.
- Dùng màu xanh, cam và đỏ để phân biệt rõ mức độ của từng quyết định.
- Responsive modal và nhóm nút cho mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 18 — Hiển thị hành động xét duyệt theo trạng thái

- Ẩn quyết định trùng với trạng thái hồ sơ hiện tại để tránh thao tác dư thừa.
- Hồ sơ đã duyệt chỉ cho phép chuyển sang yêu cầu bổ sung; không hiển thị lại nút duyệt hoặc từ chối trực tiếp.
- Hồ sơ cần bổ sung chỉ hiển thị duyệt hoặc từ chối; hồ sơ đã từ chối cho phép yêu cầu bổ sung hoặc duyệt lại.
- Hồ sơ chờ duyệt giữ đủ ba lựa chọn duyệt, yêu cầu bổ sung và từ chối.
- Bổ sung hướng dẫn ngữ cảnh và màu trạng thái trong modal.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 19 — Làm rõ khóa tài khoản và hủy duyệt

- Đổi nút nhanh `Bảo mật` thành hành động đúng trạng thái: `Khóa tài khoản` hoặc `Mở khóa`.
- Nút khóa/mở khóa tự chuyển đến tab Bảo mật, mở đúng biểu mẫu và focus vào ô lý do.
- Đổi `Cập nhật xét duyệt` thành `Hủy duyệt` khi hồ sơ đang Approved.
- Hủy duyệt yêu cầu lý do và chuyển hồ sơ sang `NeedUpdate` để KOC bổ sung, không xóa dữ liệu hồ sơ.
- Cập nhật tiêu đề, giải thích, placeholder và nút xác nhận theo đúng ngữ cảnh hủy duyệt.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 20 — Sửa màu trạng thái tài khoản đã khóa

- Badge `Đã khóa` tại header chuyển sang nền đỏ nhạt, chữ đỏ và icon ổ khóa.
- Badge `Đang hoạt động` tiếp tục dùng màu xanh và icon xác nhận.
- Nút `Mở khóa` giữ màu xanh vì đây là hành động khôi phục tài khoản.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 21 — Avatar và tài liệu hồ sơ KOC

- Thiết kế lại avatar thành khung riêng 72px, bo góc, nền và đổ bóng nhẹ; cho phép mở ảnh gốc.
- Thêm chấm trạng thái xanh/đỏ trên avatar theo quyền truy cập tài khoản.
- Đọc riêng media loại `ProfileDocument` mới nhất và không trộn tài liệu PDF/Word vào thư viện ảnh.
- Thêm thẻ hồ sơ năng lực với định dạng file, trạng thái chưa upload, nút xem file và tải xuống.
- Tối ưu bố cục avatar và tài liệu trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 22 — Trang xem profile KOC

- Thêm route `/Admin/KOC/Profile/{profileId}` dành cho Admin xem hồ sơ KOC theo góc nhìn Brand.
- Bổ sung link `Xem profile KOC` ngay trên header Trung tâm tài khoản.
- Trang profile trình bày hero, KPI, danh mục, mạng xã hội, portfolio, thư viện ảnh, năng lực, kinh nghiệm và tài liệu hồ sơ.
- Ẩn email, điện thoại, địa chỉ chi tiết, ghi chú nội bộ và công cụ quản trị khỏi bản profile giới thiệu.
- Có link quay lại đúng Trung tâm tài khoản của KOC hiện tại.
- Responsive cho tablet và mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 23 — Giữ trạng thái active menu KOC

- Gán `AdminActivePath=/Admin/Accounts/KOC` cho Trung tâm KOC và trang xem profile KOC.
- Trung tâm Brand dùng `AdminActivePath=/Admin/Accounts/Brand` tương ứng.
- Menu KOC trên thanh điều hướng tiếp tục active khi Admin đi từ danh sách sang trung tâm hoặc profile preview.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 24 — Làm rõ trạng thái truy cập trong danh sách KOC

- Bỏ công tắc bật/tắt gây hiểu nhầm là thao tác khóa nhanh.
- Dùng icon khóa mở màu xanh cho tài khoản hoạt động và icon khóa màu đỏ cho tài khoản đã khóa.
- Giữ icon email riêng để phân biệt trạng thái xác minh; mỗi icon có tooltip và aria-label đầy đủ.
- Khóa/mở khóa chỉ thực hiện trong Trung tâm tài khoản, có lý do và xác nhận.
- JavaScript syntax hợp lệ; build sạch và kiểm thử hồi quy đạt `313/313`.

### Lần 25 — Bỏ tạo tài khoản thủ công khỏi danh sách

- Bỏ nút `Thêm KOC/Brand` khỏi header trang quản lý tài khoản.
- Bỏ modal tạo tài khoản không còn được sử dụng.
- Giữ luồng đăng ký và xác minh chính thức làm nguồn tạo tài khoản.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 26 — Loại bỏ link sang trang xét duyệt cũ

- Icon trạng thái hồ sơ trong danh sách chuyển sang Trung tâm tài khoản và mở trực tiếp tab `#profile`.
- Link `Mở hồ sơ đầy đủ` trong phần chi tiết cũng dùng Trung tâm mới.
- Không còn tham chiếu `/Admin/ProfileReview` trong luồng danh sách KOC/Brand.
- JavaScript syntax hợp lệ; build sạch và kiểm thử hồi quy đạt `313/313`.

### Lần 27 — Hoàn thiện Trung tâm quản trị Brand

- Mở rộng Account Center dùng dữ liệu Brand thật: doanh nghiệp, đầu mối, website, pháp lý, thuế và giấy phép kinh doanh.
- Tổng quan Brand dùng KPI chiến dịch, chiến dịch đang vận hành, tổng ngân sách và vi phạm.
- Tab Hồ sơ Brand có độ đầy đủ 10 nhóm, file giấy phép xem/tải, thông tin pháp nhân và danh sách chiến dịch.
- Modal xét duyệt Brand hiển thị số chiến dịch và trạng thái giấy phép thay cho chỉ số KOC.
- Bảo mật, xác minh email, khóa/mở khóa, phiên đăng nhập và ghi chú dùng nhãn đúng Brand.
- Danh sách Brand bổ sung KPI email chưa xác minh giống KOC.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 28 — Phục hồi tài khoản Brand demo

- Tạo lại `brand@topstar.local` trong database hiện hành với role `BRAND`.
- Mật khẩu demo dùng chuẩn seed `Topstar@123`.
- Tài khoản Active, email/điện thoại đã xác minh và hồ sơ Brand Approved.
- Bổ sung hồ sơ doanh nghiệp, đầu mối, ngành hàng, website, địa chỉ và thông tin thuế để kiểm thử portal.
- Sửa hash bằng script SQL để tránh PowerShell diễn giải ký tự `$`; đặt lại bộ đếm đăng nhập sai.
- Bổ sung tỉnh/phường và giấy phép kinh doanh để hồ sơ đạt điều kiện sử dụng tính năng.
- Kiểm tra đăng nhập thật thành công và được chuyển tới `/Brand/Dashboard`.

### Lần 29 — Làm rõ nơi quản lý đơn Brand mua gói KOC

- Đổi thẻ `Phí dịch vụ` trên trang Tài chính thành `Đơn mua gói KOC` để đúng nghiệp vụ Admin cần tìm.
- Thiết kế lại trang Billing với hai khu vực tách bạch: đơn mua gói KOC và hóa đơn phí chiến dịch.
- Bổ sung KPI tổng đơn, đơn chờ thanh toán, gói còn khả dụng và doanh thu đã thu từ gói.
- Danh sách đơn hiển thị Brand, mã đơn, tên gói, giá trị, phương thức, trạng thái và chiến dịch đã sử dụng.
- Đơn chờ thanh toán có thao tác nhập mã giao dịch và xác nhận đã thu tiền ngay trên dòng.
- Trạng thái được Việt hóa, mã màu rõ ràng và bố cục responsive cho tablet/mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 30 — Chuẩn hóa bố cục quản lý đơn mua gói

- Chuyển hai nghiệp vụ thành tab thật; mỗi lần chỉ hiển thị `Đơn mua gói KOC` hoặc `Hóa đơn phí chiến dịch`.
- Bổ sung hàng tiêu đề cột cho danh sách đơn để Admin đọc đúng Brand, gói, giá trị, trạng thái và thao tác.
- Chuẩn hóa lưới cột và chiều cao input/nút xác nhận để các thao tác thẳng hàng.
- Gộp các dòng đơn vào một khung bảng thống nhất, bỏ cảm giác các card rời rạc.
- Giữ bố cục responsive: tablet chuyển sang thẻ 3 cột, mobile chuyển sang 1–2 cột.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 31 — Bổ sung chứng từ mua gói và căn chỉnh thao tác

- Hiển thị trực tiếp ảnh bằng chứng chuyển khoản Brand đã tải lên trong từng đơn mua gói.
- Hiển thị thời điểm gửi và toàn bộ ghi chú thanh toán của Brand; có link mở ảnh gốc.
- Đơn chưa có chứng từ hiển thị cảnh báo rõ để Admin kiểm tra trước khi xác nhận thu tiền.
- Chuẩn hóa bộ lọc thành grid, đồng nhất chiều cao input, select và hai nút `Áp dụng`/`Đặt lại`.
- Chuẩn hóa chiều cao ô mã giao dịch và nút xác nhận thanh toán, giữ thẳng hàng trên desktop và tự xếp dọc trên mobile.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 32 — Danh sách đơn gọn và modal xét duyệt

- Chuyển danh sách đơn mua gói về dạng bảng gọn, mỗi đơn chỉ chiếm một dòng.
- Đưa phần tổng kết kết quả và bộ phân trang xuống đúng bên dưới danh sách.
- Thay ô nhập/nút xác nhận trên từng dòng bằng một nút `Xem & xét duyệt` thống nhất.
- Modal xét duyệt hiển thị đầy đủ Brand, mã đơn, gói, giá trị, trạng thái, chứng từ, thời gian gửi và ghi chú.
- Admin nhập mã giao dịch và xác nhận đã thu tiền trong footer modal; đơn đã xử lý chỉ hiển thị thông tin đối soát.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 33 — Ghi chú đối soát dạng textarea

- Đổi ô mã giao dịch một dòng thành textarea `Ghi chú đối soát` trong modal xét duyệt đơn mua gói.
- Placeholder hướng dẫn nhập cả mã giao dịch và nội dung xác nhận thanh toán.
- Bổ sung mô tả lưu nhật ký kiểm toán và giới hạn 100 ký tự.
- Cho phép kéo tăng chiều cao textarea khi cần xem nội dung dài.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 34 — Sửa lỗi tạo chiến dịch do mã loại nội dung

- Xác định form mới mặc định gửi `Video` trong khi danh mục vận hành lưu mã chuẩn `video`.
- Đổi giá trị mặc định của nội dung bàn giao sang mã chuẩn `video`.
- Validation danh mục chuyển sang so sánh không phân biệt chữ hoa/thường để tương thích dữ liệu chiến dịch cũ.
- Khi loại nội dung thực sự không hợp lệ, lỗi được gắn vào đúng trường nội dung bàn giao thay vì chỉ hiện cảnh báo chung.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 35 — Chuẩn hóa form tạo chiến dịch theo gói đã mua

- Đổi loại nội dung bàn giao từ ô nhập tự do thành select lấy trực tiếp từ danh mục đang hoạt động.
- Dòng nội dung thêm mới bằng JavaScript cũng dùng cùng danh mục, tránh gửi mã không hợp lệ.
- Khi loại nội dung lỗi, trang tự mở đúng tab `Nội dung bàn giao` và đánh dấu trường cần sửa.
- Ngân sách lấy phạm vi snapshot của chính đơn mua gói, không lấy nhầm chính sách hiện tại.
- Hiển thị rõ khoảng ngân sách được phép ngay dưới ô nhập và kiểm tra tức thời phía trình duyệt.
- Lỗi ngân sách từ backend được gắn đúng vào ô ngân sách thay vì cảnh báo chung khó hiểu.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 36 — Gói giới hạn số KOC, không giới hạn ngân sách

- Loại bỏ kiểm tra khoảng ngân sách của gói khi tạo và chỉnh sửa chiến dịch.
- Giữ kiểm tra `SelectionQuota`: số KOC cần tuyển không được vượt quyền lợi của gói đã mua.
- Ô ngân sách trở thành số tiền Brand chủ động thiết lập và không còn tự điền theo khoảng giá trị gói.
- Bổ sung chú thích rõ ngay dưới ô ngân sách để tránh hiểu nhầm giữa ngân sách chiến dịch và quyền lợi gói.
- Trang danh sách gói đổi mô tả từ phạm vi ngân sách sang quyền chủ động thiết lập ngân sách.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 37 — Nhấn mạnh quyền lợi gói và lối vào quản trị

- Bổ sung khối màu `Quyền lợi chính` trên từng thẻ gói, phân biệt rõ Topstar phân công và Brand tự chọn KOC.
- Tên gói Topstar không còn được tạo theo khoảng ngân sách; dùng nhãn nghiệp vụ `Topstar tuyển chọn và phân công KOC phù hợp`.
- Lọc bỏ các bullet quyền lợi cũ có nội dung giới hạn/phù hợp ngân sách trên trang Brand.
- Bổ sung lối vào `Cấu hình gói KOC` ngay tại trang Admin Tài chính.
- Đổi nhãn trong Công cụ quản trị thành `Cấu hình gói KOC`, mô tả rõ quản lý giá bán, số KOC và quyền lựa chọn.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 38 — Dọn giao diện danh sách quản lý gói

- Tắt toolbar `Lọc/Xóa lọc` tự sinh của bộ danh sách để không trùng với bộ lọc riêng của trang.
- Ẩn hàng DataTables rỗng phía trên bảng.
- Sửa lưới bộ lọc thành đủ năm cột: tìm kiếm, trạng thái, số dòng, lọc và đặt lại.
- Đồng nhất chiều cao và căn giữa hai nút thao tác bộ lọc.
- Thiết lập lại độ rộng các cột bảng, cho tên/mô tả xuống dòng hợp lý và căn giữa cột thao tác.
- Tắt tự động co giãn cột của DataTables để bố cục ổn định.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 39 — Chuẩn hóa validation và nút quay lại form gói

- Thay link quay lại dạng chữ bằng nút viền chuẩn, đặt thống nhất bên phải header trên desktop và phía trên trên mobile.
- Bổ sung khối tổng hợp lỗi ở đầu form với tiêu đề và hướng dẫn rõ ràng.
- Tab chứa lỗi có màu cảnh báo và badge chấm than; sau khi submit tự mở tab có lỗi đầu tiên.
- Kích hoạt client validation chuẩn để bắt lỗi trước khi gửi lên server.
- Các trường bắt buộc quan trọng được đánh dấu `*` nhất quán.
- Khi người dùng sửa dữ liệu, trạng thái lỗi và badge tab được cập nhật lại ngay.
- Đổi nhãn tab `Campaign & Matching` thành `Quyền chọn KOC` dễ hiểu hơn.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 40 — Sửa menu active cho quản lý gói KOC

- Nhóm trang danh sách, thêm và sửa gói KOC được xác định là nghiệp vụ Tài chính dù URL kỹ thuật nằm dưới `Admin/Settings`.
- Menu `Tài chính` được active trên toàn bộ nhóm trang gói KOC; menu `Quản trị` không còn active đồng thời.
- Các trang cấu hình khác dưới `Admin/Settings` vẫn giữ menu `Quản trị` như trước.
- Build thành công với 0 lỗi, 0 cảnh báo.

### Lần 41 — Sửa quy tắc ngân sách và giới hạn KOC của gói

- Bỏ điều kiện cấm các gói có khoảng ngân sách giao nhau; đây là mức tư vấn, không phải dải phân tuyến độc quyền.
- Đổi nhãn thành `Ngân sách đề xuất` và giải thích rõ khoảng này có thể trùng giữa nhiều gói.
- Khi tắt quyền Brand tự chọn, trường giới hạn KOC được khóa và thay bằng trạng thái `Không áp dụng`.
- Làm rõ giới hạn bắt buộc của gói Brand tự chọn là số KOC tối đa.
- Chỉ tab thực sự có thông báo lỗi mới hiển thị badge cảnh báo.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 42 — Chuẩn hóa giới hạn KOC cho toàn bộ gói

- Mọi gói đều bắt buộc khai báo số KOC được xác nhận tối đa, không phụ thuộc hình thức tuyển chọn.
- Nhóm Topstar phân công được chuẩn hóa thành tối đa `1/3/5 KOC`; nhóm Brand tự chọn giữ mức `3/5/10 KOC`.
- Cập nhật dữ liệu sáu gói đang bán, tên gói và mô tả để quyền lợi được thể hiện trực tiếp.
- Trang bán gói, thanh toán và danh sách Admin đều hiển thị hình thức tuyển chọn kèm quota KOC.
- Luồng tạo và sửa chiến dịch dùng quota snapshot của đơn mua để chặn vượt quyền lợi.
- Các đơn mua đã phát sinh không bị thay đổi snapshot quyền lợi.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 43 — Trình bày lại trang công khai gói KOC

- Chia sáu gói thành hai nhóm rõ ràng: `Topstar tuyển chọn và phân công` và `Brand chủ động lựa chọn`.
- Bổ sung hướng dẫn chọn phương án trước khi so sánh gói.
- Đưa quota KOC thành khối thông tin nổi bật trên từng thẻ.
- Hiển thị rõ mỗi gói dùng cho một chiến dịch và thời hạn sử dụng.
- Ghi chú ngân sách trả KOC do Brand thiết lập riêng, gói chỉ giới hạn số KOC được xác nhận.
- Tối ưu bố cục responsive cho phần hướng dẫn và hai nhóm gói.
- Build thành công với 0 lỗi, 0 cảnh báo; trang `/goi-koc` phản hồi đầy đủ nội dung mới.

### Lần 44 — Hoàn chỉnh rule quota khi Brand tạo chiến dịch

- Đưa quota và hình thức tuyển chọn từ snapshot đơn mua vào màn hình tạo/sửa chiến dịch.
- Ô số KOC dùng quota thật làm giá trị `max`, hiển thị tên gói và giới hạn ngay cạnh trường nhập.
- Bắt lỗi tức thời trên trình duyệt và gắn lỗi backend đúng vào trường số KOC.
- Backend bắt buộc tìm quota dự phòng từ chính sách gói nếu snapshot cũ bị thiếu; không còn cho qua khi quota rỗng.
- Backfill quota cho `2` đơn mua cũ còn thiếu snapshot; sau cập nhật không còn đơn thiếu quota.
- Gói đã mua vẫn sử dụng được nếu Admin ngừng bán gói sau thời điểm mua.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.

### Lần 45 — Sửa Google OAuth production trả về localhost

- Xác định database đang ghi đè callback Google thành `https://localhost:7161/signin-google` khi ứng dụng khởi động.
- Cập nhật callback production Google thành `https://topstar.vn/signin-google` và Facebook thành `https://topstar.vn/signin-facebook` trong source lẫn database.
- Tách callback localhost sang `appsettings.Development.json` để môi trường local vẫn phát triển độc lập.
- Chọn callback theo hostname request để `topstar.vn` vẫn dùng domain thật ngay cả khi tiến trình IIS đang chạy với environment Development.
- Production từ chối dùng redirect HTTPS trỏ tới loopback nếu database bị cấu hình nhầm lần nữa.
- Chuẩn hóa URL kích hoạt, đặt lại mật khẩu, website và logo trong cấu hình mặc định sang `https://topstar.vn`.
- Kiểm tra trực tiếp luồng Google trên `https://topstar.vn`: authorization URL trả `redirect_uri=https://topstar.vn/signin-google`.
- Build thành công với 0 lỗi, 0 cảnh báo; kiểm thử hồi quy đạt `313/313`.
