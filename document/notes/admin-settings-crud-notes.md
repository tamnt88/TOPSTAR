## Admin Settings CRUD - cập nhật (2026-04-05)

Đã triển khai CRUD theo page cho các mục Cài đặt còn lại, dùng service chung:

1) Tài khoản gửi & nhận email
- List + Create/Update + Delete ngay trên page.
- File: `Pages/Admin/Settings/EmailAccounts.cshtml(.cs)`

2) Cấu hình giao nhận
- List + Create/Update + Delete ngay trên page.
- File: `Pages/Admin/Settings/Shipping.cshtml(.cs)`

3) Cấu hình thông tin website
- Form cập nhật cấu hình website dùng chung.
- File: `Pages/Admin/Settings/WebsiteInfo.cshtml(.cs)`

4) Cấu hình logo & fav
- Đổi sang upload file thật (logo/favicon/default share image), lưu vào `wwwroot/uploads/branding`.
- Không nhập URL tay cho các trường ảnh/logo/fav.
- File: `Pages/Admin/Settings/Branding.cshtml(.cs)`

5) Cấu hình SEO & Social trang public
- List + Create/Update + Delete cấu hình theo từng page key/path.
- Ảnh OG và ảnh Twitter dùng upload file, lưu vào `wwwroot/uploads/seo`.
- File: `Pages/Admin/Settings/SeoSocial.cshtml(.cs)`

Service dùng chung:
- Contract: `Topstar.Application/SystemSettings/SystemSettingsContracts.cs`
- Implementation: `Topstar.Infrastructure/SystemSettings/SystemSettingsAdminService.cs`
- DI: `Topstar.Infrastructure/InfrastructureDependencyInjection.cs`

Nguyên tắc quan trọng:
- Web PageModel không query DbContext trực tiếp.
- Toàn bộ xử lý data qua `ISystemSettingsAdminService`.
- Chuỗi tiếng Việt trong UI/Service phải lưu UTF-8 chuẩn (không dấu lỗi `?`, `Ã`, `�`).
- Các cấu hình ảnh/logo/fav bắt buộc đi theo luồng upload file.
