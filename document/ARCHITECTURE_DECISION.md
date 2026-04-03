# Topstar - Architecture Decision Note

Ngày ghi nhận: 2026-04-03

## Quyết định kiến trúc

Topstar sẽ triển khai theo mô hình:
- `Shared Business Logic` trong các project dùng chung (`Core/Application`).
- `Website` dùng trực tiếp logic dùng chung (không gọi HTTP API nội bộ).
- `API` cũng dùng cùng logic dùng chung đó.
- `Mobile (Flutter)` gọi `API`.

## Cấu trúc backend đề xuất

```txt
backend/
  Topstar.Core/            # Domain model + business rules
  Topstar.Application/     # Use cases / services / DTO / validation
  Topstar.Infrastructure/  # EF Core, SQL Server 2022, integrations
  Topstar.Web/             # Website host (Admin/Brand), gọi trực tiếp Application
  Topstar.Api/             # REST API host cho Mobile/đối tác
```

## Nguyên tắc triển khai

- Không đặt business logic trong `Topstar.Web` hoặc `Topstar.Api`.
- `Topstar.Web` và `Topstar.Api` chỉ là lớp trình bày (presentation/transport).
- Luồng nghiệp vụ phải nằm trong `Topstar.Application` và `Topstar.Core`.
- Cùng một use case phải cho ra hành vi giống nhau dù chạy từ Web hay API.

## Vì sao chọn cách này

- Tránh duplication logic giữa Web và API.
- Website không có overhead gọi API nội bộ qua HTTP.
- Vẫn giữ được API riêng cho mobile.
- Phù hợp giai đoạn MVP: nhanh triển khai, dễ kiểm soát hơn microservices.

## Rủi ro cần kiểm soát

- Dễ "lệch kiến trúc" nếu dev đưa logic vào Controller/PageModel.
- Cần test integration/use-case để đảm bảo nhất quán giữa Web và API.
- Cần quy ước rõ dependency direction:
- `Web`/`Api` phụ thuộc `Application`.
- `Application` phụ thuộc abstraction.
- `Infrastructure` hiện thực abstraction.

## Kết luận

Đây là kiến trúc chính thức cho giai đoạn hiện tại của Topstar.
Nếu quy mô tăng mạnh, có thể tách dần module ra service độc lập sau.

