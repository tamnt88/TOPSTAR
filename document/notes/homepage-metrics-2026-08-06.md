# Homepage - dải số liệu vận hành (06/08/2026)

## Phạm vi hoàn thiện

- Thay bốn card số liệu rời rạc bằng một dải dữ liệu vận hành có phân cấp thị giác rõ ràng.
- Giữ nền tổng thể sáng, dùng navy làm điểm neo; tím và vàng chỉ đóng vai trò màu nhấn.
- Bổ sung icon riêng cho từng chỉ số, đường phân tách và ba cam kết: hồ sơ xác thực, hiệu quả theo dõi, quy trình lưu vết.
- Bổ sung diễn giải ngắn để số liệu không đứng độc lập, tránh cảm giác số liệu trang trí.
- Tận dụng cơ chế `counter-value` hiện có để số đếm khi section xuất hiện; không tạo JavaScript trùng lặp.
- Responsive: 4 cột desktop, 2 cột tablet và 1 cột mobile; không phát sinh cuộn ngang.

## Kiểm tra

- `dotnet build Topstar.sln --no-restore`: thành công, 0 warning, 0 error.
- Website đã chạy lại tại `http://localhost:5201`.
- Trang chủ trả HTTP 200 và đã render `home-proof__heading`, `counter-value`.

## Sua loi mat CSS khi tai trang

- Chuyen `footer.css`, CSS rieng cua trang va `public-theme.css` vao `<head>` cua layout public.
- Khong con nap stylesheet tu partial footer; tranh cac section quy trinh/so lieu hien nhu HTML tho va phat sinh thanh cuon ngang trong luc tai trang.
- Thu tu nap: CSS nen -> footer -> CSS rieng trang -> public theme, de lop giao dien hoan thien duoc ap dung sau cung.
- Da build thanh cong va xac nhan trang chu HTTP 200; tat ca stylesheet nam truoc `</head>`.

## Tach khoi cac the so lieu (bo sung)

- Sua lop giao dien public ghi de `gap: 0`; bon the so lieu tren desktop hien co khoang cach 14px va khong con dinh vien.
- Moi the co vien, bo goc, nen trong suot nhe va hover rieng de van thuoc cung mot cum nhung de nhan dien tung chi so.
- Responsive giu 2 cot tren tablet, 1 cot va khoang cach 12px tren mobile.
- Build thanh cong 0 warning, 0 error; trang chu va stylesheet fingerprint tren `http://localhost:5201` tra HTTP 200.
- Khong thay doi database; khong co SQL can chay.

## Database

- Không thay đổi schema hoặc dữ liệu; không có file SQL cần chạy.
