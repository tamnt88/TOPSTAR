# TOPSTAR - Ghi chú triển khai cho team dev

## 1) Quy tắc URL đẹp (Rewrite URL)
- Trang Học viện KOC dùng route đẹp: `/hoc-vien-koc`.
- Không dùng URL kỹ thuật cho public page khi đã có route thân thiện.
- Thiết lập toàn hệ thống: `LowercaseUrls = true` trong `Program.cs`.
- Khi tạo trang public mới, ưu tiên dùng `@page "/duong-dan-than-thien"`.

## 2) Quy tắc SEO + Social Share (áp dụng toàn site)
- SEO mặc định đặt tại `Pages/Shared/_Layout.cshtml`.
- Các thẻ đã chuẩn hóa dùng chung:
  - `title`, `description`, `keywords`, `robots`, `canonical`
  - Open Graph: `og:type`, `og:locale`, `og:site_name`, `og:title`, `og:description`, `og:url`, `og:image`
  - Twitter Card: `twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`
- Mỗi trang có thể override bằng:
  - `ViewData["Title"]`
  - `ViewData["Description"]`
  - `ViewData["Keywords"]`
  - `ViewData["ShareImage"]`

## 3) Quy tắc giao diện/animation trang public
- Dùng tông màu thương hiệu vàng/cam/nâu đồng nhất.
- Mọi section chính nên là block tách biệt, có nhịp điệu thị giác rõ.
- Dùng class `reveal-up` + `section-flow` để tạo hiệu ứng vào khung nhìn.
- Counter số liệu dùng class `counter-value` + `data-target`, `data-suffix`.
- Hiệu ứng phải mượt, không dùng pattern gây nhức mắt.

## 4) Trang Học viện KOC (đã triển khai)
- File: `Pages/Public/Academy.cshtml`
- CSS: `wwwroot/css/site.css` (khối `/* ===== Academy Page ===== */`)
- Cấu trúc gồm:
  - Hero + KPI
  - Các khóa học chuyên sâu
  - Lộ trình phát triển (timeline)
  - Case Studies
  - Thư viện tài liệu
  - CTA cuối trang

## 5) Trang Brand + Gói KOC
- Trang public Brand:
  - File: `Pages/Public/BrandPublic.cshtml`
  - Route đẹp: `/danh-cho-brand`
- Trang gói KOC cho Brand:
  - File: `Pages/Public/BrandPackages.cshtml`
  - Route đẹp: `/goi-koc`
- Điều hướng CTA bắt buộc:
  - Nút `Xem các gói KOC` -> `/goi-koc`
  - Nút `Mua gói KOC ngay` -> `/goi-koc`
- Active menu `Dành cho Brand` phải sáng khi vào cả `/danh-cho-brand` và `/goi-koc`.

## 6) Checklist khi thêm trang public mới
- Có route đẹp.
- Có `ViewData` cho SEO cơ bản.
- Có phiên bản mobile responsive.
- Có animation nhẹ, không làm giảm hiệu năng.
- Kiểm tra hiển thị tiếng Việt có dấu và số liệu định dạng `vi-VN`.

## 7) Quy tắc auth/account (mới)
- Website xử lý auth trực tiếp qua `Application + Infrastructure`, KHÔNG gọi API.\n- Persistence dùng EF Core (DbContext + Entity mapping), không dùng SqlCommand thủ công cho luồng auth.
- Luồng chuẩn hiện tại:
  - Đăng nhập: `/dang-nhap`
  - Đăng ký: `/dang-ky`
  - Quên mật khẩu: `/quen-mat-khau`
  - Đặt lại mật khẩu: `/dat-lai-mat-khau?token=...`
  - Đăng xuất: `/dang-xuat`
- Dashboard theo role sau khi đăng nhập:
  - `Admin` -> `/Admin/Dashboard`
  - `Brand` -> `/Brand/Dashboard`
  - `Koc` -> `/Koc/Dashboard`
- Các folder dashboard đã bật authorize trong `Program.cs`:
  - `/Admin`, `/Brand`, `/Koc`

## 8) Quy tắc lưu bảo mật và tracking thiết bị
- Hash mật khẩu: PBKDF2 SHA256 (`100000` iterations).
- Mọi sự kiện auth phải ghi vào `AuthEvents` (`Register/Login/ForgotPassword/ResetPassword`).
- Thiết bị đăng ký/đăng nhập được upsert vào `UserDevices` bằng fingerprint.
- Quên mật khẩu tạo token hash trong `PasswordResetTokens` (hết hạn 30 phút).
- Với local/dev chưa có email service: link reset được hiển thị trực tiếp ở UI quên mật khẩu để test end-to-end.


