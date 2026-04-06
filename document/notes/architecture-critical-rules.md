# QUY TẮC KIẾN TRÚC BẮT BUỘC (CỰC QUAN TRỌNG)

## Mục tiêu thống nhất
1. Website (Topstar.Web) KHÔNG gọi API nội bộ để render trang.
2. Website gọi trực tiếp tầng logic dùng chung (Application + Infrastructure).
3. Mobile (Flutter) gọi qua API.
4. Tuyệt đối không viết truy vấn DB/LINQ inline trong `.cshtml`.

## Quy tắc triển khai
- View `.cshtml` chỉ render UI và dữ liệu đã chuẩn bị.
- Mọi nghiệp vụ/truy vấn phải nằm ở Service/Use case thuộc tầng dùng chung.
- Razor PageModel/Controller chỉ điều phối request -> gọi service -> trả result.

## Đã refactor trong module Menu Admin
- Tạo contract dùng chung:
  - `Topstar.Application/NavigationContracts.cs`
  - `Topstar.Application/MenuAdminContracts.cs`
- Tạo implementation ở Infrastructure:
  - `Topstar.Infrastructure/NavigationService.cs`
  - `Topstar.Infrastructure/MenuAdminService.cs`
- Đăng ký DI:
  - `Topstar.Infrastructure/InfrastructureDependencyInjection.cs`
- Header admin đã bỏ truy vấn inline:
  - `Topstar.Web/Pages/Shared/_AdminHeader.cshtml` chỉ gọi `INavigationService`.
- CRUD Menu Admin đã chuyển gọi `IMenuAdminService`.

## Quy định code review
- PR sẽ bị reject nếu có:
  - `@inject TopstarDbContext` trong `.cshtml`
  - LINQ/EF query trực tiếp trong View
  - Business rule nằm trong View

## Ghi chú vận hành
- Khi app đang chạy trong Visual Studio, build CLI có thể fail do lock file DLL/EXE. Dừng process web trước khi build lại toàn bộ.
## Audit nhanh kiến trúc dùng chung (2026-04-05)

### Đã chuẩn hóa theo service dùng chung
- Admin Header: dùng `INavigationService`, không query DB trong `.cshtml`.
- Admin Menu CRUD: dùng `IMenuAdminService` cho list/create/update/delete.
- Tách model dùng chung cho form menu:
  - `Topstar.Web/Features/Admin/Menu/MenuFormModels.cs`
  - `MenuCreate` + `MenuEdit` không còn lặp class `InputModel`/`ParseCode`.

### Các điểm còn đang dùng TopstarDbContext trực tiếp trong Web (cần refactor tiếp)
1. `Pages/Public/RegisterKocStep1.cshtml.cs`
2. `Pages/Public/RegisterKocStep2.cshtml.cs`
3. `Pages/Public/RegisterKocStep3.cshtml.cs`
4. `Pages/Public/ActivateAccount.cshtml.cs`

### Kế hoạch refactor tiếp theo (bắt buộc)
- Tạo `IKocRegistrationService` (Application) + implementation Infrastructure để gom toàn bộ logic step 1/2/3.
- Tạo `IAccountActivationService` để tách logic kích hoạt tài khoản khỏi PageModel.
- PageModel chỉ giữ vai trò điều phối request/response (controller của Razor Pages), không chứa nghiệp vụ + EF query.

### Ghi chú quan trọng
- Dự án đang dùng Razor Pages nên không có MVC Controller cho các trang web là đúng chuẩn.
- Với kiến trúc hiện tại: `PageModel` đóng vai trò tương đương controller, nhưng vẫn phải gọi service tầng dùng chung.

### Component dùng chung mới
- Đã tách paging thành component dùng chung:
  - ackend/src/Topstar.Web/Features/Common/Paging/PagerViewModel.cs`n  - ackend/src/Topstar.Web/Pages/Shared/_Pager.cshtml`n- Trang MenuManagement đã dùng partial _Pager, không còn hardcode khối paging inline.
## Refactor triệt để (2026-04-05 - bổ sung)

### 1) Bỏ hardcode dùng chung
- Page size đã chuẩn hóa dùng chung tại:
  - `Topstar.Application/Common/Pagination/PaginationDefaults.cs`
- Scope menu (ALL/ADMIN/KOC/BRAND) + formatter đã chuẩn hóa dùng chung tại:
  - `Topstar.Application/Menus/MenuSharedOptions.cs`
- UI trang list/create/edit đã đọc từ options dùng chung, không hardcode danh sách scope/page size trong view.

### 2) Tổ chức lại folder/class
- Application:
  - `Menus/MenuAdminContracts.cs`
  - `Menus/MenuSharedOptions.cs`
  - `Navigation/NavigationContracts.cs`
  - `Common/Pagination/PaginationDefaults.cs`
- Infrastructure:
  - `Menus/MenuAdminService.cs`
  - `Navigation/NavigationService.cs`
- Đã xóa file lẻ ở root gây rời rạc.

### 3) Reuse cho web + mobile API
- Toàn bộ rule paging/scope/format code menu nằm ở Application -> dùng chung cho:
  - Razor Pages (website)
  - API (mobile) khi triển khai endpoint.

### 4) Component paging dùng chung
- `Topstar.Web/Features/Common/Paging/PagerViewModel.cs`
- `Topstar.Web/Pages/Shared/_Pager.cshtml`
- `MenuManagement` dùng partial `_Pager`, không code paging inline.
## Bổ sung chuẩn hóa dùng chung cho API/Web (2026-04-05)

- Chuyển constants/rules dùng chung sang Topstar.Core:
  - `Topstar.Core/Common/Pagination/PaginationDefaults.cs`
  - `Topstar.Core/Menus/MenuOptions.cs` (scope options + formatter)
- Application giữ contracts/service interface theo module:
  - `Topstar.Application/Menus/MenuAdminContracts.cs`
  - `Topstar.Application/Navigation/NavigationContracts.cs`
- Infrastructure giữ implementation theo module:
  - `Topstar.Infrastructure/Menus/MenuAdminService.cs`
  - `Topstar.Infrastructure/Navigation/NavigationService.cs`
- Web dùng lại core options (không hardcode scope/page size trong view/page model).
- Đã xóa các file lẻ ở root Application/Infrastructure liên quan Menu/Navigation để tránh rời rạc.

## Refactor RegisterKoc + ActivateAccount (2026-04-05)

### Mục tiêu
- Loại bỏ hoàn toàn truy vấn DB trực tiếp trong Web ở cụm:
  - `RegisterKocStep1`
  - `RegisterKocStep2`
  - `RegisterKocStep3`
  - `ActivateAccount`

### Hạng mục đã triển khai
- Tạo module hợp đồng dùng chung cho luồng đăng ký KOC:
  - `backend/src/Topstar.Application/KocRegistration/KocRegistrationContracts.cs`
- Tạo service kích hoạt tài khoản dùng chung:
  - `backend/src/Topstar.Application/Auth/AccountActivationContracts.cs`
- Tạo implementation tầng Infrastructure:
  - `backend/src/Topstar.Infrastructure/Koc/KocRegistrationService.cs`
  - `backend/src/Topstar.Infrastructure/Auth/AccountActivationService.cs`
- Đăng ký DI:
  - `backend/src/Topstar.Infrastructure/InfrastructureDependencyInjection.cs`

### Kết quả trong Web
- `RegisterKocStep1.cshtml.cs`: dùng `IKocRegistrationService` để load tỉnh/phường + validate province/ward.
- `RegisterKocStep2.cshtml.cs`: dùng `IKocRegistrationService` để load danh mục ngành.
- `RegisterKocStep3.cshtml.cs`: dùng `IKocRegistrationService` để ghi profile/social/categories/children; dùng `IAccountActivationService` để tạo token kích hoạt.
- `ActivateAccount.cshtml.cs`: chỉ gọi `IAccountActivationService.ActivateByTokenAsync`.

### Nguyên tắc bắt buộc tiếp tục áp dụng
- Web PageModel/Controller không truy vấn `TopstarDbContext` trực tiếp.
- Toàn bộ nghiệp vụ và dữ liệu dùng chung phải đi qua service contract ở Application + implementation ở Infrastructure.
