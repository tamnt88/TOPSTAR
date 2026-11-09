# Điều chỉnh cụm giới thiệu nền tảng trên trang chủ

- Bỏ khối nền tím lớn phía sau ảnh; thay bằng khung ảnh editorial màu navy có điểm nhấn vàng kem.
- Thẻ số liệu `500+ Brand tin dùng` dùng nền kem để tách lớp rõ nhưng không cạnh tranh với tiêu đề.
- Giảm cỡ tiêu đề và tăng khoảng thở giữa ảnh, nội dung để cân bằng hai cột.
- Ba lợi ích được trình bày thành một tuyến dọc liên tục, phân biệt bằng tím, vàng và xanh; không dùng ba card giống hệt nhau.
- Có cấu hình responsive riêng: chuyển một cột từ tablet và không phát sinh cuộn ngang trên mobile.

## Kiểm tra

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- Trang chủ: HTTP 200.
- CSS giao diện public: HTTP 200.
- Không thay đổi database, không có SQL cần chạy.

## Tuyến quy trình trên trang chủ

- Thay bốn card rời bằng một timeline liên tục có bốn mốc nghiệp vụ.
- Đường tiến trình dùng chuyển động màu nhẹ để thể hiện hành trình đang vận hành; không dùng các mũi tên nền trắng rời rạc.
- Nội dung từng bước được đặt dưới mốc số, có vạch phân cách nhẹ để giữ khả năng đọc nhưng không tạo cảm giác bốn khối độc lập.
- Nút bắt đầu chiến dịch dùng màu vàng kem trên nền navy để rõ hành động và giảm mật độ màu tím.
- Trên tablet/mobile, timeline tự chuyển sang chiều dọc.
