# Trang quản trị số dư Brand

Đường dẫn: `/Admin/Finance/BrandBalances`. Không cần script SQL mới cho thay đổi giao diện này; vẫn cần SQL161–162 của nghiệp vụ số dư và chiến dịch.

- Thống kê toàn bộ số dư khả dụng, tiền giữ để rút, số yêu cầu chờ duyệt/chờ chuyển, số tiền có thể đối soát hoàn.
- Ba tab: Yêu cầu rút tiền, Hoàn vào số dư, Chính sách.
- Hai bảng dùng jQuery DataTables và TopstarLists; lọc, đếm, phân trang tại database. Mặc định 20 dòng, chọn 10/20/50/100; trạng thái và số trang được lưu theo namespace URL của từng bảng. Tắt toolbar Lọc/Xóa lọc tự sinh để không trùng bộ lọc.
- Modal xử lý yêu cầu chỉ tải chi tiết khi mở. Tách rõ số tiền giữ, phí, thực nhận và tài khoản đã chốt. Người đã duyệt lần đầu không có nút tự duyệt lần hai.
- Hoàn vào số dư mở modal từ đúng dòng quỹ; chốt operation key cho lần gửi. Backend tiếp tục kiểm tra quyền, hạn mức và điều kiện tài chính. Hàng đợi duyệt lần hai cập nhật qua AJAX.
- Form dùng novalidate, thông báo chung, lỗi từng trường bằng tiếng Việt; không đóng modal hoặc xóa dữ liệu khi backend từ chối. Có khóa gửi lặp và loading. Sau thành công tải lại bảng/thống kê, giữ bộ lọc và trang.
- Select2 và file upload tiếp tục dùng bộ xử lý chung. Các ô tiền ở chính sách có phân cách hàng nghìn; gửi số nguyên về backend. Modal có backdrop tĩnh.
- Mobile chuyển bảng thành thẻ có nhãn cột; thống kê giữ một hàng theo chuẩn chung.

Kiểm tra: Web build; Chrome headless dùng asset thật với API giả lập để kiểm tra bảng, bộ lọc, tab, modal, lỗi nhập liệu, gửi AJAX và không tràn ngang ở 390px. Harness SQL LocalDB riêng qua 61 kiểm tra, gồm các truy vấn summary/lọc/phân trang và endpoint đọc chi tiết mới. Không gửi yêu cầu duyệt/chuyển tiền thật trong quá trình kiểm tra.
