# Academy - dữ liệu khóa học mẫu và quản trị Admin (14/08/2026)

## Phạm vi

- Bổ sung dữ liệu kiểm thử cho khối **Các khóa học đang mở** tại `/hoc-vien-koc`.
- Dữ liệu dùng chung với màn hình quản trị `/Admin/Academy`, không hard-code riêng ở trang public.

## Dữ liệu mẫu

- 3 khóa học ở trạng thái `Published`.
- 11 bài học thuộc các khóa học.
- Lịch sử audit tương ứng cho khóa học và bài học.

## Quản trị

Admin vào **Học viện KOC** (`/Admin/Academy`) để:

- Tạo và sửa khóa học.
- Xuất bản, ẩn hoặc lưu trữ khóa học.
- Tạo, sửa, sắp xếp và quản lý trạng thái bài học.
- Theo dõi lịch sử thay đổi.

Chỉ khóa học ở trạng thái `Published` mới xuất hiện trong mục **Các khóa học đang mở**.

## Database

File seed an toàn, có thể chạy lặp lại:

`database/sql/131_seed_academy_course_catalog_demo.sql`

Script chỉ thêm dữ liệu chưa tồn tại theo tiêu đề; không cập nhật hoặc ghi đè nội dung Admin đã chỉnh sửa. Script chỉ cho phép chạy trên database DEV/QA.

Kết quả đã chạy trên `TopstarDb`:

- `PublishedCourses=3`
- `ActiveLessons=11`
- `CourseAudits=3`
- `LessonAudits=11`

## Kiểm tra

- Build `backend/Topstar.sln`: thành công.
- 0 lỗi, 0 cảnh báo.
- Website đang chạy tại `http://localhost:5201`.
- `/hoc-vien-koc`: HTTP 200 và render 3 thẻ khóa học từ database (không dùng dữ liệu hard-code ở Razor).
- Chạy lại file seed không ghi đè khóa học hoặc bài học đã được Admin chỉnh sửa.
