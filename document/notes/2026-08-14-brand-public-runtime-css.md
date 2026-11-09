# Khắc phục CSS trang Dành cho Brand

- Trang kiểm tra: `/danh-cho-brand`.
- Hiện tượng: header tải đúng nhưng nội dung landing page hiển thị như HTML thô.
- Cách xử lý: tạo entry CSS độc lập `brand-public-runtime.css` và nạp trực tiếp tại Razor Page, không còn phụ thuộc vào việc layout có render `section Styles` hay không.
- Entry nạp CSS chính của trang và CSS hiệu chỉnh hành trình, đồng thời đổi phiên bản URL để tránh cache cũ.
- Không thay đổi database; không có SQL cần chạy.
