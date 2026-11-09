# Admin campaign funding - implementation 2026-08-04

## Đã triển khai

- Trang `Admin/Finance/CampaignFunding` dùng mô hình trung tâm điều hành trên một dashboard.
- Hàng đợi gồm: Chờ đối soát, Đã ký quỹ, Nghiệm thu & giải ngân, Ngoại lệ và Tất cả.
- Bảng đối soát dùng jQuery DataTables: tìm kiếm, lọc trạng thái, chọn số dòng, sắp xếp và phân trang.
- Mỗi hàng đợi lọc đúng trạng thái trên cùng dataset; tab và bộ lọc hiện tại được giữ trong `sessionStorage` sau POST/redirect.
- Mỗi yêu cầu có hồ sơ tài chính dạng modal với các tab Tổng quan, Phân bổ KOC, Thanh toán và Chứng từ.
- Xác nhận chuyển khoản được thực hiện ngay trong hồ sơ. OnePAY chỉ đọc trạng thái IPN, Admin không xác nhận thủ công.
- Validation theo chuẩn chung: `novalidate`, viền đỏ, icon lỗi và thông báo ngay dưới trường; service tiếp tục kiểm tra nghiệp vụ và ghi ledger/audit.
- CSS/JavaScript tách riêng tại `wwwroot/css/admin/admin.css` và `wwwroot/js/admin/campaign-funding.js`.

## Giới hạn đã ghi nhận, không tạo thao tác giả

- Sai lệch thiếu/thừa tiền và sai nội dung chuyển khoản.
- Luồng yêu cầu bổ sung hoặc từ chối từng phiên bản chứng từ.
- Maker-checker cho giao dịch vượt hạn mức.
- Tài khoản nhận hoàn, chứng từ hoàn tiền và đối soát ngân hàng tự động.

Đợt hoàn thiện giao diện này không thay đổi database và không có file SQL mới cần chạy.

## Bổ sung cùng ngày

- Cột thao tác được rút gọn còn một nút `Xem hồ sơ`; các liên kết chuyển sang trang chiến dịch và trang matching đã được loại khỏi luồng xử lý tài chính chính.
- Modal hồ sơ bổ sung tab `Nghiệm thu & hoàn tiền` và `Lịch sử`.
- Nghiệm thu từng KOC chỉ mở cho hồ sơ đã ký quỹ và chiến dịch hủy có kết luận tranh chấp; tránh giải ngân thủ công sai luồng đối với chiến dịch bình thường.
- Hoàn tiền chỉ mở trong phạm vi số tiền đã được duyệt hoàn. Mã giao dịch, lý do và số tiền tiếp tục được kiểm tra tại client và service.
- Lịch sử đọc trực tiếp từ `CampaignFundLedgers`, hiển thị thời gian, loại biến động, ghi chú và số tiền; không phát sinh bảng dữ liệu mới.
- Chuẩn DataTables của bảng quản trị dùng tối thiểu 10 dòng/trang; chỉ cho chọn 10, 20 hoặc 50 dòng. Cấu hình 5 dòng đã lưu trước đó trong trình duyệt được tự động nâng lên 10.
