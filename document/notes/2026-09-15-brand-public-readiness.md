# Brand sau gộp menu: kiểm tra trước public

## Đã xử lý

- Menu chính rút còn Tổng quan, Chiến dịch, Tài chính, Hỗ trợ.
- Hồ sơ nằm ở tài khoản; thông báo dùng chuông.
- Chức năng vận hành vẫn truy cập từ trung tâm điều hành chiến dịch.
- Sửa icon không có trong Font Awesome Free: grid-2 và shield-check.
- Audit bổ sung phát hiện icon không có glyph, căn tâm header và mở menu thao tác desktop/mobile.
- Không sửa dữ liệu đăng ký thật hoặc phân bổ tài chính.

## Chưa được nghiệm thu production

- Kiểm thử end-to-end trên dữ liệu QA: mua gói, xác nhận ký quỹ, hoàn tiền, đối soát, nghiệm thu, tranh chấp; không dùng hồ sơ thật để thử.
- Xác minh cấu hình production: domain/HTTPS, secrets, quyền DB, backup/restore, logging và giám sát lỗi.
- Kiểm thử gateway thanh toán/callback và thông báo thực tế bằng môi trường sandbox.
- Kiểm tra phân quyền chéo Brand/KOC/Admin và truy cập chiến dịch không thuộc sở hữu.
- Kiểm tra trên thiết bị iOS/Android thật, không chỉ viewport trình duyệt.
- Chỉ public sau khi các kiểm tra trên có bằng chứng; audit giao diện không thay thế nghiệm thu nghiệp vụ.
