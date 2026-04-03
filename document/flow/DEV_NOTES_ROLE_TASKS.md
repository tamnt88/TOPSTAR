# Topstar - Công Việc Theo Vai Trò (Dev Notes)

Mục tiêu tài liệu:
- Liệt kê rõ các công việc nghiệp vụ theo 3 vai trò `Admin`, `Brand`, `KOC`.
- Dùng làm checklist cho team dev khi tách module, API, DB và UI.
- Chuẩn hoá thuật ngữ để tránh hiểu sai giữa FE, BE, QA.

Nguồn tham chiếu:
- `document/flow/Tổng quan luồng Admin.docx`
- `document/flow/Tổng quan luồng Brand.docx`
- `document/flow/Tổng quan luồng KOC.docx`

## 1) Công việc theo vai trò: Admin

### A. Thiết lập hệ thống (trước vận hành)
- Cấu hình form KOC CV (tick/chọn, field bắt buộc).
- Cấu hình form Brand onboarding.
- Cấu hình form Campaign (deliverables, timeline, budget/package).
- Cấu hình rules package:
- `<5tr`: brand không được chọn KOC.
- `>=20tr` (hoặc theo policy): brand được chọn KOC theo quota.
- Cấu hình RBAC (vai trò admin, quyền truy cập).
- Cấu hình notification templates.
- Bật audit logs cho hành động quan trọng.

### B. Duyệt onboarding
- Duyệt hồ sơ KOC (`PendingReview` -> `Approved/NeedUpdate/Rejected/Locked`).
- Duyệt hồ sơ Brand (`PendingReview` -> `Approved/NeedUpdate/Rejected/Locked`).
- Ghi reason/checklist khi NeedUpdate hoặc Reject.

### C. Duyệt Campaign và publish
- Xem queue campaign `Submitted`.
- Kiểm tra brief, deliverables, timeline, requirement KOC, logistics.
- Kiểm tra package rule có hợp lệ với quyền chọn KOC.
- Quyết định `Approved(Published)` hoặc `NeedEdit` hoặc `Rejected`.

### D. Matching và shortlist
- Theo dõi toàn bộ KOC apply theo campaign.
- Lọc theo score, followers, lĩnh vực, khu vực, lịch rảnh, completion history.
- Chạy shortlist tool:
- Top 3 theo điểm cao.
- Phần còn lại random theo quota + điều kiện tối thiểu.
- Override shortlist thủ công khi cần và lưu note lý do.
- Gửi shortlist cho Brand (`Applied` -> `Shortlisted`).

### E. Chốt tham gia campaign
- Nhận confirm/chọn từ Brand theo package.
- Chốt trạng thái:
- KOC được tham gia -> `Confirmed`.
- KOC không được chọn -> `NotSelected`.
- Khởi tạo task list execution (booking, shipment, deliverables).

### F. Điều phối vận hành
- Booking:
- Theo dõi calendar tổng.
- Can thiệp khi quá hạn confirm, đổi lịch nhiều, late cancel.
- Shipment:
- Theo dõi vận đơn theo campaign/KOC.
- Xử lý issue: lost/delay/return/sai trạng thái.
- Deliverables:
- Giám sát `Submitted/NeedRevision/Approved/Rejected`.
- Nhắc SLA duyệt nội dung.
- Can thiệp dispute nếu cần.

### G. Quality, Trust, Reporting
- Quản lý rating/review KOC <-> Brand.
- Xử lý khiếu nại review, ẩn review vi phạm.
- Vận hành scoring/ranking KOC (ảnh hưởng shortlist Top 3).
- Theo dõi KPI vận hành: completion rate, issue rate, top KOC/Brand.

### H. Finance/Wallet (phase sau)
- Tính phí dịch vụ theo campaign (15% nếu bật).
- Đối soát billing/invoice/receipt.
- Quản lý wallet KOC:
- `Pending` -> `Available`.
- `OnHold` khi dispute.
- Xử lý rút tiền: `WithdrawRequested` -> `PaidOut/Failed`.

## 2) Công việc theo vai trò: Brand

### A. Onboarding Brand
- Đăng ký tài khoản brand và gửi hồ sơ.
- Theo dõi phản hồi duyệt:
- `NeedUpdate`: sửa theo checklist và submit lại.
- `Approved`: mở tính năng tạo campaign.

### B. Tạo campaign
- Nhập thông tin campaign:
- Mục tiêu, ngành hàng, yêu cầu KOC.
- Deliverables (số lượng, định dạng, key message, hashtag).
- Timeline và deadlines.
- Budget và package.
- Cờ logistics:
- Có booking không.
- Có shipment không.
- Đặt policy revision (tối đa N lần) và tiêu chí approve/reject.
- Lưu Draft hoặc Submit duyệt.

### C. Làm việc với kết quả duyệt campaign
- Nhận trạng thái từ admin:
- `NeedEdit`: sửa đúng mục được yêu cầu.
- `Approved/Published`: campaign mở cho KOC apply.
- `Rejected`: xem lý do và xử lý theo chính sách.

### D. Nhận shortlist KOC (ẩn danh)
- Xem shortlist theo campaign:
- thông tin ẩn danh: kinh nghiệm, lĩnh vực, followers, ranking.
- Nắm rõ label hệ thống (Top 3 / Random picks) nếu hiển thị.

### E. Xác nhận/chọn KOC theo gói
- Nếu package không cho chọn: xác nhận danh sách được gửi.
- Nếu package cho chọn: chọn KOC theo quota được phép.
- Confirm để admin chốt participation.

### F. Booking và Shipment (nếu bật)
- Booking:
- Đề xuất/confirm/reschedule/cancel lịch với KOC.
- Shipment:
- Tạo shipment cho KOC.
- Theo dõi tracking timeline đến `Delivered`.
- Tạo issue/support khi phát sinh lỗi.

### G. Duyệt deliverables và đóng campaign
- Review submission theo từng deliverable.
- Quyết định:
- `Approved`.
- `NeedRevision` + comment rõ.
- `Rejected` + lý do.
- Duyệt lại vòng revision theo giới hạn N lần.
- Chốt campaign `Completed` khi đạt điều kiện.

### H. Review và tài chính (phase sau)
- Đánh giá KOC sau hoàn tất campaign.
- Theo dõi phí dịch vụ, hóa đơn, biên nhận, trạng thái thanh toán.

## 3) Công việc theo vai trò: KOC

### A. Onboarding và CV
- Đăng ký tài khoản, xác thực OTP.
- Điền CV theo wizard:
- Cá nhân.
- Kênh social + followers + lĩnh vực.
- Thông tin bổ sung liên quan campaign (body profile, kinh nghiệm, thiết bị, availability...).
- Submit hồ sơ để admin duyệt.
- Xử lý phản hồi:
- `NeedUpdate`: cập nhật theo checklist.
- `Approved`: mở module campaign/wallet.

### B. Khám phá và apply campaign
- Duyệt list campaign, lọc theo lĩnh vực/budget/khu vực/thời gian.
- Xem chi tiết campaign và điều kiện tham gia.
- Submit apply (`Applied`) kèm ghi chú/lịch rảnh/địa chỉ nhận hàng (nếu cần).
- Có quyền rút apply trước khi admin chốt (`Withdrawn`).

### C. Theo dõi shortlist và confirm tham gia
- Theo dõi trạng thái:
- `Applied` -> `Shortlisted` -> `Confirmed/NotSelected`.
- Khi `Confirmed`, vào task list execution theo campaign.

### D. Booking và nhận hàng (nếu có)
- Booking:
- Accept/đề xuất lại lịch.
- Tuân thủ giới hạn reschedule/late cancel.
- Shipment:
- Xác nhận địa chỉ nhận hàng.
- Theo dõi tracking.
- Xác nhận đã nhận hàng hoặc mở issue nếu bất thường.

### E. Thực thi và nộp deliverables
- Thực hiện nội dung theo brief và deadline.
- Nộp bài/link/proof/metrics theo từng deliverable (`Submitted`).
- Nếu `NeedRevision`, sửa và nộp lại trong giới hạn N lần.
- Theo dõi kết quả cuối: `Approved/Rejected`.

### F. Review và Wallet
- Đánh giá Brand sau khi campaign hoàn tất.
- Theo dõi điểm/ranking của bản thân.
- Quản lý wallet:
- Theo dõi `Pending/Available/OnHold/PaidOut`.
- Gửi yêu cầu rút tiền (`WithdrawRequested`).
- Theo dõi kết quả payout (`PaidOut`/`Failed`).

## 4) Note bắt buộc cho team dev

### A. Chuẩn hóa trạng thái (state machine)
- Mỗi domain cần enum + transition rule rõ ràng:
- `KOCProfile`, `BrandProfile`, `Campaign`, `Application`, `Schedule`, `Shipment`, `Submission`, `Review`, `WalletTransaction`.
- Không cho FE/BE tự suy diễn trạng thái ngoài transition hợp lệ.

### B. Rule engine / policy config
- Tách policy khỏi UI:
- Package rule (`<5tr`, `>=20tr`, quota).
- Số lần revision tối đa.
- SLA duyệt.
- Hold period wallet.
- Late cancel penalty.

### C. Audit + Notification + Escalation
- Bắt buộc log cho hành động duyệt/chốt/hủy/override shortlist/trạng thái payout.
- Notification theo event:
- NeedUpdate, Approved, Confirmed, lịch hẹn, shipment issue, revision required.
- Escalation cho quá hạn SLA hoặc dispute.

### D. Ưu tiên triển khai MVP
- Phase 1:
- Onboarding KOC/Brand.
- Create + approve + publish campaign.
- Apply + shortlist + confirm.
- Booking + shipment basic tracking.
- Submission + revision + completion.
- Review cơ bản.
- Phase 2:
- Ranking nâng cao.
- Wallet payout đầy đủ.
- Billing/invoice/fee 15%.

## 5) Gợi ý phân chia module kỹ thuật
- `auth-identity`: đăng ký/đăng nhập/OTP/RBAC.
- `profiles`: KOC profile + Brand profile.
- `campaigns`: campaign CRUD + approval workflow.
- `matching`: applications + shortlist tool + selection confirm.
- `operations-booking`: calendar + schedule states.
- `operations-shipment`: shipment + tracking + issue.
- `deliverables`: submission/revision/approval.
- `reviews-quality`: rating/review/dispute.
- `wallet-finance`: earnings/payout/billing (phase sau).
- `notifications`: templates + triggers.
- `audit-reporting`: audit trails + dashboard KPI.

