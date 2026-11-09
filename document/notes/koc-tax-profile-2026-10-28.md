# Thuế và CCCD KOC

Triển khai database/sql/165_add_koc_tax_profiles.sql trước binaries. Không tự chạy SQL trên dữ liệu sử dụng.

KOC: Hồ sơ & uy tín → Chỉnh sửa hồ sơ → Thuế & CCCD.
Admin: Quản lý KOC → Trung tâm tài khoản → Thuế & CCCD (chỉ xem).

- MST và CCCD lưu chuỗi mã hóa bằng Data Protection, không có trong hồ sơ Brand hoặc DTO public.
- MST và CCCD chỉ bắt buộc nhập, không ràng buộc độ dài; không xác minh cơ quan thuế.
- Ảnh JPG/PNG/WEBP, kiểm tra dấu hiệu tệp và giới hạn 5 MB/mặt. Cần cả hai ảnh ở lần lưu đầu; lần sau giữ ảnh cũ nếu không thay.
- Ảnh được mã hóa trước khi ghi App_Data/private-koc-tax ngoài wwwroot. Endpoint ảnh kiểm tra người sở hữu hoặc Admin, không cache. Ghi audit việc xem/cập nhật; không ghi số giấy tờ vào audit.
- Back up thư mục private-koc-tax cùng Data Protection key ring hiện hành và database. Giữ nguyên key ring khi deploy/chuyển máy, nếu mất khóa sẽ không đọc được thông tin cũ. Không đưa App_Data/private-koc-tax vào gói publish hoặc Git.
- Không dùng thông tin thuế làm điều kiện bắt buộc nhận việc/rút tiền ở thay đổi này. Không tự tính thuế hoặc đối chiếu danh tính nhà nước.
