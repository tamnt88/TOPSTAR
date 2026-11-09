# Rà soát trải nghiệm website KOC

## Mục tiêu

Giúp KOC nhìn thấy việc cần làm, mở đúng đối tượng và hoàn thành thao tác với ít bước nhất. Dữ liệu tiền, deadline và số việc phải đúng trên toàn phạm vi, không phụ thuộc trang danh sách.

## Phát hiện

- P0: Thu nhập ước tính trên Dashboard đang cộng ngân sách toàn campaign, không phải phần phân bổ của KOC.
- P0: Một số KPI booking/deadline được tính từ lát dữ liệu đang hiển thị.
- P0: Việc cần xử lý trên Dashboard dẫn tới danh sách chung, chưa mở đúng submission/application/booking.
- P1: Chi tiết campaign chưa phân biệt ngân sách campaign với thu nhập KOC; trạng thái còn hiển thị mã tiếng Anh và quay lại làm mất bộ lọc.
- P1: Hồ sơ ứng tuyển nằm cuối trang Jobs trong khối thu gọn, khó theo dõi trên mobile.
- P1: Mobile cần lối tắt cố định tới Tổng quan, Công việc, Lịch, Ví và Thông báo.
- P1: Shipment dựng nhiều modal/form theo số dòng; nên dùng modal hành động dùng chung và chỉ dựng chi tiết khi mở dòng.
- P2: Thông báo có id bộ lọc trùng, thiếu lựa chọn 100 dòng và cần deep link theo loại nghiệp vụ.
- P2: Hồ sơ 5 tab cần lưu từng phần, báo trạng thái đã lưu và preview góc nhìn Brand.
- P2: Layout KOC đọc nội dung website lặp lại trong cùng request; nên cache theo request.

## Thứ tự triển khai

1. Sửa số tiền/KPI và tạo trung tâm “Việc cần làm”.
2. Deep link đúng việc, giữ trạng thái quay lại và nâng cấp chi tiết campaign/ứng tuyển.
3. Bottom navigation mobile, thông báo và giảm DOM shipment.
4. Wizard hồ sơ, cache layout và đo hiệu năng trên dữ liệu lớn.

## Tiêu chí nghiệm thu

- Không gọi tổng ngân sách campaign là thu nhập KOC.
- KPI không đổi khi đổi trang hoặc page size.
- Từ Dashboard mở đúng việc cần xử lý.
- Các màn chính không tràn ngang ở viewport 390 px.
- Build sạch, test ứng dụng và browser smoke KOC đều đạt.

## Đã hoàn thành trong đợt đầu

- Thu nhập dự kiến lấy từ phần phân bổ ký quỹ của đúng participation KOC, loại funding cũ/hủy/hoàn; không cộng ngân sách toàn campaign.
- Dashboard ưu tiên submission cần sửa/chưa nộp rồi mới tới hồ sơ chờ Brand; mỗi dòng có Brand, deadline, hành động và deep link.
- Deep link đầu việc tự lọc và mở modal đúng submission; deep link ứng tuyển tự mở khu vực “Đăng ký của tôi” và đánh dấu đúng hồ sơ.
- Booking sắp tới được đếm trên toàn phạm vi; deadline 7 ngày chỉ tính đầu việc chưa nộp hoặc cần sửa.
- Chi tiết campaign giải thích rõ tổng ngân sách không phải thu nhập KOC, Việt hóa trạng thái và giữ URL khi quay lại.
- Mobile có bottom navigation cho Tổng quan, Công việc, Lịch, Ví và Thông báo.
- Thông báo chỉ còn một `notificationFilter` và có lựa chọn 100 dòng.
- Layout chỉ đọc cấu hình website một lần trong mỗi request.

## Đã hoàn thành trong đợt hoàn thiện

- “Đăng ký của tôi” đã thành khu vực riêng trên trang Cơ hội, có tìm kiếm, lọc trạng thái, page size và phân trang tại DB; lịch sử chỉ lấy cho các hồ sơ trong trang hiện tại.
- Shipment chỉ còn ba modal hành động dùng chung cho toàn trang (xác nhận nhận, báo sự cố, thêm ảnh); chi tiết dòng được dựng khi KOC mở và modal nhận đúng shipment từ nút thao tác.
- Hồ sơ lưu độc lập theo 5 phần: cơ bản, mạng xã hội, cá nhân, thư viện ảnh và video; mỗi phần giữ đúng tab sau khi lưu hoặc có lỗi.
- Có chế độ “Xem như Brand” từ trang sửa/hồ sơ; email, điện thoại, ngày sinh và địa chỉ chi tiết được che trước khi xác nhận cộng tác.
- Bottom navigation dẫn thẳng tới Thông báo và hiện badge số chưa đọc; URL thông báo KOC được chuẩn hóa theo submission, booking, shipment, application và review, kể cả bản ghi cũ chưa có TargetUrl tường minh.
- Kiểm thử ngày 08/09/2026: build 0 warning/0 error, 271/271 application tests đạt; browser smoke ở viewport 390 px đạt cho khu vực đăng ký, deep link, shipment modal dùng chung, preview Brand và không có lỗi JavaScript/tràn ngang.

## Theo dõi sau phát hành

- Đo thời gian truy vấn và tỷ lệ thao tác hoàn tất bằng telemetry thực tế; chỉ bổ sung cache liên request khi số liệu cho thấy cấu hình/menu hoặc badge tạo tải đáng kể.
- Khi có thiết kế hồ sơ KOC dành riêng cho Brand, tái sử dụng cùng view model với chế độ preview để đảm bảo KOC thấy đúng nội dung Brand thật sự thấy.

## Vòng rà soát hoàn thiện

- Đã smoke test toàn bộ 15 màn KOC ở viewport 390 × 844: tất cả HTTP 200, có tiêu đề trang, không tràn ngang và không có JavaScript page error.
- Chuẩn hóa redirect sau thao tác trên Booking, Shipment, Ví và rút đăng ký: giữ nguyên tìm kiếm, trạng thái, page size và trang đang xem của từng danh sách.
- Loại bỏ bộ lọc Shipment cũ bị ẩn; trang chỉ còn một bộ lọc/phân trang `_ListWindow` làm nguồn trạng thái duy nhất.
- `ProfileAccessService` lưu kết quả kiểm tra Brand/KOC trong phạm vi request. Page guard và layout dùng chung kết quả, tránh chạy lại chuỗi truy vấn hoàn thiện hồ sơ trong cùng request.
- Đã khai báo precision Entity Framework khớp schema SQL cho tiền chiến dịch/KOC, số đo cơ thể, điểm hồ sơ và tỷ lệ social; loại bỏ cảnh báo có thể cắt hoặc làm tròn dữ liệu khi ghi.
- Môi trường Development chạy HTTP không còn gọi HTTPS redirect khi chưa cấu hình cổng TLS; Production vẫn giữ HSTS và HTTPS redirect.
