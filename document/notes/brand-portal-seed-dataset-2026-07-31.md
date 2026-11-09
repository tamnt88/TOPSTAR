# Brand portal QA seed — 31/07/2026

## Tài khoản kiểm thử

- Email: `brand@topstar.local`
- Mật khẩu: `Topstar@123`
- Trạng thái tài khoản: `Active`
- Hồ sơ Brand: `Approved`, đủ checklist bắt buộc.

## Dữ liệu đã seed

- 12 chiến dịch QA mới, cộng với chiến dịch demo cũ thành 13 chiến dịch.
- Trạng thái phủ: `Draft`, `Submitted`, `NeedEdit`, `Published`, `InProgress`, `Completed`, `Rejected`.
- Mỗi chiến dịch QA có:
  - Package và ngân sách tương ứng.
  - Thời gian bắt đầu/kết thúc.
  - Brief tiếng Việt.
  - Deliverable.
  - Targeting cơ bản.
  - Lịch sử trạng thái.
- 48 lượt KOC ứng tuyển/xác nhận, lấy từ các KOC `Approved` và tài khoản `Active`.
- Sáu package public được bổ sung giá demo nếu trước đó giá bằng 0 để kiểm thử trang `/Brand/Packages`.

## SQL

- Seed: `database/sql/104_seed_brand_portal_test_dataset.sql`
- Verify: `database/sql/105_verify_brand_portal_test_dataset.sql`
- Hai file đều chạy lặp lại được; seed đã được chạy liên tiếp nhiều lần trên local mà không tăng số bản ghi.
- File chứa tiếng Việt UTF-8. Khi dùng `sqlcmd`, bắt buộc thêm `-f 65001`:

```powershell
sqlcmd -S "DESKTOP-L6JINQQ" -d TopstarDb -U sa -P 123456 -C -b -f 65001 -i database\sql\104_seed_brand_portal_test_dataset.sql
sqlcmd -S "DESKTOP-L6JINQQ" -d TopstarDb -U sa -P 123456 -C -b -f 65001 -i database\sql\105_verify_brand_portal_test_dataset.sql
```

## Kết quả QA

- Brand account: 1 tài khoản `Active`, hồ sơ `Approved` và `Complete`.
- Brand campaigns: 13.
- QA campaigns: 12.
- QA applications: 48.
- Endpoint `/Brand/Campaigns?handler=Data`: HTTP 200, `recordsTotal = 13`.
- Trang `/Brand/Packages`: HTTP 200, hiển thị 6 gói và CTA chọn gói.
- Tiếng Việt trong JSON DataTables và giao diện hiển thị đúng Unicode.
- Không thay đổi schema database.
