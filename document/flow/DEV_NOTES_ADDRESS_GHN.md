# Topstar - Quy tac dia chi GHN va validate realtime

## 1) Validate realtime Email/SĐT (AJAX)
- Ap dung cho form dang ky KOC buoc 1 va dang ky Brand.
- Trigger: khi user roi o Email/SĐT (blur).
- Endpoint Razor Page handler: `?handler=CheckIdentity`.
- Neu ton tai:
- Email: hien loi `Email da ton tai trong he thong.`
- SĐT: hien loi `So dien thoai da ton tai trong he thong.`
- Van giu server-side validate khi submit de dam bao an toan.

## 2) Chuan dia chi de tuong thich GHN
- Chuan UI dang ky su dung 3 truong bat buoc:
- `StreetAddress`: So nha + ten duong
- `WardName`: Phuong/Xa
- `CityName`: Tinh/Thanh pho

## 3) Dinh huong mapping GHN (giai doan tiep theo)
- Khi tich hop API GHN shipping/order, bo sung them cac field sau trong DB:
- `WardIdV2` (so nguyen theo GHN)
- `AddressV2` (chuoi dia chi day du theo quy dinh GHN)
- `IsNewAddress` (bool)
- Theo tai lieu GHN trong `document/GHN/...docx`, GHN chap nhan cau truc dia chi moi va field v2.

## 4) Quy tac luu tru de theo doi van don
- Profile KOC/Brand nen co dia chi mac dinh theo format tren.
- Don hang/van don phai luu snapshot dia chi tai thoi diem tao don (khong chi reference profile).
- Co trang thai xac minh dia chi (chua xac minh/da xac minh GHN).

## 5) Danh sach file da ap dung
- `backend/src/Topstar.Web/Pages/Public/RegisterKocStep1.cshtml`
- `backend/src/Topstar.Web/Pages/Public/RegisterKocStep1.cshtml.cs`
- `backend/src/Topstar.Web/Pages/Public/RegisterBrand.cshtml`
- `backend/src/Topstar.Web/Pages/Public/RegisterBrand.cshtml.cs`
