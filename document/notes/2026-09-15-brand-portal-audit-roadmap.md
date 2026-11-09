# Brand Portal audit & delivery roadmap — 15/09/2026

## Bổ sung: native campaign workspace

Đã chuyển toàn bộ tab/subtab chiến dịch sang Razor fragment/AJAX native, không còn iframe. Chi tiết thay đổi, kiểm chứng và giới hạn production: [Brand native workspace](2026-09-15-brand-native-workspace.md).

## Góc nhìn nghiệm thu

Brand cần hoàn tất một chiến dịch nhanh, luôn biết việc tiếp theo, ai đang giữ việc, hạn xử lý và tổng tiền thực trả. Mọi số liệu hiển thị phải truy nguyên được từ database, bị giới hạn theo đúng Brand sở hữu và dùng tốt trên desktop lẫn mobile.

## Baseline đã tốt

- Solution build thành công, không có warning; 276 kiểm thử nghiệp vụ hiện hữu đều đạt.
- Portal đã có đầy đủ các miền chức năng chính: hồ sơ, gói dịch vụ, chiến dịch, tuyển KOC, ký quỹ, booking, vận chuyển, nội dung, báo cáo, đánh giá, thông báo và tranh chấp.
- Campaign Workspace đã gom phần lớn hành trình theo một chiến dịch và đã có quy tắc xác định việc tiếp theo từ dữ liệu vận hành.
- Luồng mua gói có checkout, sổ giao dịch, trạng thái thanh toán và chặn tạo chiến dịch khi chưa có gói hợp lệ.
- Các màn hình vận hành chính đã lấy dữ liệu qua service/database, có kiểm tra quyền sở hữu và nhiều quy tắc chuyển trạng thái có test tự động.

## Khoảng trống cần xác minh/khắc phục

### P0 — chặn Brand triển khai hoặc gây rủi ro tiền/dữ liệu

- Audit đăng nhập thật toàn bộ route để phát hiện redirect sai, lỗi 500, lỗi tài nguyên, mojibake và rò dữ liệu chéo Brand.
- Xác minh tổng chi phí xuyên suốt gồm giá gói, thù lao KOC, phí dịch vụ, tiền ký quỹ, đã giải ngân/hoàn tiền; thống nhất tên gọi giữa menu và màn hình.
- Xác minh mọi trạng thái đều trả lời được `Brand cần làm gì`, `đang chờ ai`, `hạn khi nào`, và CTA mở đúng đối tượng/chiến dịch.
- Xác minh hành động ghi dữ liệu có chống giả mạo, kiểm tra quyền sở hữu ở server và không cho chuyển trạng thái trái quy trình.

### P1 — tốc độ vận hành chiến dịch

- Chuẩn hóa thứ tự menu theo hành trình: Tổng quan → Hồ sơ → Gói & tài chính → Chiến dịch/tuyển KOC → Vận hành → Nội dung/nghiệm thu → Báo cáo/đánh giá → Hỗ trợ.
- Bổ sung/chuẩn hóa deadline, quá hạn, người đang giữ việc và hành động tiếp theo trên dashboard, danh sách và workspace.
- Chuẩn hóa empty/error/loading state để luôn có hướng xử lý; bộ lọc phải giữ ngữ cảnh chiến dịch.
- Kiểm tra responsive 390×844: không tràn ngang, menu dùng được, modal/bảng/action có tap target phù hợp.

### P2 — đo hiệu quả và khả năng production

- Đối chiếu KPI báo cáo với nguồn dữ liệu social sync và trạng thái nghiệm thu; nêu rõ thời điểm đồng bộ gần nhất.
- Bổ sung kiểm thử browser hồi quy cho desktop/mobile và ma trận route Brand.
- Tách rõ phần local đã nghiệm thu với tích hợp production: cổng thanh toán, vận chuyển, social API, email/SMS, lưu trữ, scheduler và monitoring.

## Thứ tự thực hiện

1. Baseline code/database, build và unit tests.
2. Runtime audit đăng nhập thật trên desktop/mobile; lập danh sách lỗi theo P0/P1/P2.
3. Sửa P0 theo hành trình hồ sơ → gói → chiến dịch → tuyển KOC → tài chính.
4. Sửa P1 theo hành trình vận hành → nội dung → nghiệm thu → báo cáo → đánh giá/hỗ trợ.
5. Chạy lại build, unit test, browser audit, kiểm tra quyền và dữ liệu.
6. Chạy website tại `http://localhost:5201`, chốt phạm vi local và danh sách phụ thuộc production.

## Nhật ký nghiệm thu

- [x] Baseline: build 0 warning/0 error.
- [x] Baseline: 276/276 unit tests đạt.
- [x] Runtime desktop có đăng nhập thật: 17/17 route đạt.
- [x] Runtime mobile 390×844 có đăng nhập thật: 17/17 route đạt.
- [x] P0 local hoàn tất: không có lỗi route/server/JS, luồng chi phí không thu trùng phí nền tảng, service ghi dữ liệu kiểm tra Brand sở hữu.
- [x] P1 local hoàn tất: menu theo hành trình, CTA mobile tối thiểu 44×44 px, không tràn ngang/ảnh hỏng/mojibake/action không tên.
- [x] Đối chiếu dữ liệu và phân quyền local hoàn tất; dùng riêng dataset QA idempotent, không cập nhật/xóa tài khoản đăng ký thật.
- [x] Website cuối cùng chạy tại cổng 5201.

## Thay đổi sau audit

- Gom mua gói, quản lý chiến dịch, tuyển KOC, ký quỹ và hóa đơn vào nhóm `Chiến dịch & ngân sách`; đổi `Phí dịch vụ` thành `Tài chính & hóa đơn` để Brand nhìn đúng toàn bộ khoản tiền.
- Đưa duyệt nội dung vào nhóm `Nghiệm thu & hiệu quả`, trước báo cáo và đánh giá KOC.
- Tăng vùng chạm CTA/nút điều hướng nội dung lên tối thiểu 44×44 px trên mobile và bump cache CSS.
- Thêm browser regression `backend/tests/brand-portal-audit.browser.cjs`; kết quả cuối 34/34 lượt đạt, lưu tại `.runlogs/brand-portal-audit-final.json`.

## Hoàn thiện theo phản hồi Brand

- [x] **Hộp việc cần xử lý:** Dashboard gom tối đa 8 việc theo mức khẩn cấp/deadline, ghi rõ chiến dịch, hành động, đang chờ Brand hay Topstar và CTA mở đúng ngữ cảnh.
- [x] **Tổng chi phí chiến dịch:** Workspace hiển thị giá gói, ngân sách KOC, phí nền tảng, phí thanh toán, đã giải ngân, còn ký quỹ, tiền hoàn và tổng chi phí đã ghi nhận. Chi phí sản phẩm ngoài hệ thống được chú thích là chưa tính.
- [x] **Timeline theo KOC:** Mỗi cộng tác hiển thị chuỗi chốt KOC → giao sản phẩm → booking → nội dung → nghiệm thu, kèm người đang giữ việc, hành động tiếp theo và deadline gần nhất.
- [x] Quy tắc chi phí/escrow có 4 unit test; toàn bộ test tăng từ 276 lên 280 và đều đạt.
- [x] Browser audit cuối gồm 17 route chung và một Workspace có campaign thật trên cả hai viewport: 36/36 lượt đạt.

## Phần phụ thuộc production

- OnePAY: callback/chữ ký, tài khoản merchant, retry và đối soát tiền thật.
- Ngân hàng/chứng từ: quy trình đối soát vận hành và SLA xác nhận của Admin.
- GHN/đơn vị vận chuyển: credential, webhook, mapping địa chỉ và trạng thái giao hàng thật.
- TikTok/YouTube/social sync: OAuth production, quota, độ trễ API và tính đầy đủ metrics/last-sync.
- Email/SMS/OTP/thông báo: sender domain, template thật, deliverability và retry/dead-letter.
- Object storage/CDN/virus scan cho giấy phép, nội dung và chứng từ.
- Background jobs nhiều instance, distributed lock, monitoring/alerting, backup/restore và retention audit log.
- Kiểm thử tải, bảo mật xâm nhập, secrets/HTTPS/CORS/CSP và kiểm tra phân quyền với nhiều Brand thật trên bản sao dữ liệu production đã ẩn danh.

## Mobile-first Brand & KOC

- [x] Bottom navigation theo tác vụ hằng ngày cho Brand.
- [x] KOC ưu tiên `Việc cần làm` trên bottom navigation hiện hữu.
- [x] Badge thông báo Brand lấy từ database.
- [x] Safe-area, khoảng tránh che nội dung và vùng chạm tối thiểu 44px.
- [x] Audit cuối Brand 36/36 và KOC 24/24 lượt desktop/mobile đạt.
