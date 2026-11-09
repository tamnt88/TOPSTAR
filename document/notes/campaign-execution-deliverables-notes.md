# Campaign MVP - matching và thực thi deliverable

## Trạng thái cập nhật

Ngày review: 2026-07-27

Luồng MVP hiện đã đi xuyên suốt từ tạo campaign đến Brand duyệt nội dung:

1. Brand tạo/sửa draft và gửi Admin duyệt.
2. Admin duyệt profile Brand/KOC và campaign.
3. KOC đăng ký/rút đăng ký campaign đã publish.
4. Admin lập shortlist theo hồ sơ thực.
5. Brand chọn KOC theo package hoặc chốt toàn bộ danh sách Admin phân công.
6. Khi chốt shortlist, campaign chuyển từ `Published` sang `InProgress`.
7. Hệ thống tạo một submission task cho từng cặp participation + deliverable.
8. KOC nộp link; Brand duyệt, yêu cầu sửa hoặc từ chối; hai phía nhận notification và thao tác được ghi audit log.
9. Khi mọi submission có kết quả cuối (`Approved` hoặc `Rejected`), Brand xác nhận hoàn tất campaign; campaign và participation chuyển sang `Completed`.
10. Sau completion, Brand đánh giá từng KOC và KOC đánh giá Brand một lần trong mỗi campaign.
11. Admin đối soát khoản thu cho participation có submission `Approved`; KOC theo dõi số dư thật trong Wallet.

## Package policy đã review và sửa

- 5 dải ngân sách chuẩn: dưới 5 triệu, 5-dưới 20 triệu, 20-dưới 50 triệu, 50-dưới 100 triệu và từ 100 triệu.
- Migration `19_create_campaign_package_policies.sql` tạo trực tiếp cấu hình mới; migration `21_refine_campaign_package_policies.sql` giữ vai trò nâng cấp database cũ.
- Admin không thể lưu policy nếu dải active bị hở, chồng lấn, không bắt đầu từ 0 hoặc không bao phủ đến vô hạn.
- Shortlist đã finalize không thể thêm/chọn lại.
- Dashboard đã dùng trạng thái chuẩn `InProgress`, bỏ trạng thái cũ `Running` ở thống kê campaign.
- KOC Dashboard đã sửa nguồn dữ liệu: trạng thái apply/shortlist lấy từ `Applications`, còn job đang chạy lấy từ `Participations`; không còn truy vấn các trạng thái không hợp lệ trong bảng participation.

## Quy tắc submission

- `NotSubmitted -> Submitted`: KOC nộp URL http/https hợp lệ.
- `NeedRevision -> Submitted`: KOC nộp lại và tăng `RevisionCount`.
- `Submitted -> Approved | NeedRevision | Rejected`: chỉ Brand sở hữu campaign được duyệt.
- `NeedRevision` và `Rejected` bắt buộc có phản hồi.
- Submission bị khóa khi không ở đúng trạng thái chuyển tiếp; kiểm tra ownership luôn nằm trong service.
- Submit/review chỉ mở khi campaign là `InProgress`; task chưa finalize không xuất hiện sớm và campaign đã hoàn tất chỉ còn chế độ xem.
- Unique index `(ParticipationId, DeliverableId)` ngăn tạo task trùng.

## Quy tắc nghiệm thu và hoàn tất campaign

- `Approved` và `Rejected` là hai kết quả cuối của một submission.
- Campaign chỉ có thể chuyển `InProgress -> Completed` khi có ít nhất một submission và toàn bộ submission đã có kết quả cuối.
- Brand phải bấm xác nhận **Hoàn tất campaign**; hệ thống không tự đóng âm thầm sau lần review cuối.
- Khi hoàn tất, toàn bộ participation của campaign chuyển sang `Completed`, KOC nhận notification và các form submit/review bị khóa.
- Thao tác đóng campaign chạy trong transaction mức `Serializable` và trả thông báo tải lại nếu có cập nhật đồng thời.
- `Rejected` được tính là đã xử lý để campaign không bị kẹt vĩnh viễn; phase tài chính sau này phải chỉ dựa trên kết quả đủ điều kiện thanh toán, không suy diễn từ trạng thái `Completed`.

## Layout/UX đã triển khai

- KOC `/Koc/Tasks`: 4 thẻ tổng quan, đầu việc xếp theo hạn, hiện feedback ngay tại card, nộp/nộp lại bằng một ô URL.
- Brand `/Brand/Deliverables`: 4 thẻ tổng quan, bài `Submitted` được ưu tiên đầu danh sách, mở link bằng tab mới có `noopener noreferrer`, duyệt ngay trên card.
- Trang chi tiết execution có panel nghiệm thu, thanh tiến độ, số lượng duyệt/từ chối, lý do chưa thể hoàn tất và nút hoàn tất chỉ xuất hiện khi đủ điều kiện.
- Có đường tắt từ trang campaign/job và menu riêng cho cả Brand/KOC.
- Brand `/Brand/Reviews` và KOC `/Koc/Reviews`: card ưu tiên đánh giá còn thiếu, hai thẻ tổng quan, nút chọn 1–5 sao dễ bấm trên mobile, nhận xét tùy chọn và bản ghi đã gửi ở chế độ chỉ xem.
- View chỉ render dữ liệu; query và business rule nằm ở Application/Infrastructure đúng quy chuẩn kiến trúc.

## Quy tắc review hai chiều

- Chỉ participation thuộc campaign `Completed` mới tạo được review.
- Brand được đánh giá từng KOC đã tham gia; KOC được đánh giá Brand của campaign đó.
- Điểm hợp lệ từ 1 đến 5, comment tùy chọn và tối đa 2.000 ký tự.
- Mỗi bộ `(CampaignId, ReviewerUserId, RevieweeUserId)` chỉ có một review; service kiểm tra trước và database unique index chặn race condition.
- Review đã gửi dùng trạng thái `Completed`, hiển thị công khai theo `IsVisible`, tạo notification cho người nhận và audit log cho người gửi.
- Điểm trung bình KOC Dashboard đã sửa từ trạng thái không tồn tại `Approved` sang `Completed` và chỉ tính review visible.

## Earning settlement và Wallet MVP

- Không tự chia `Campaign.BudgetAmount` vì tài liệu chưa định nghĩa số tiền cho từng KOC hoặc từng deliverable.
- Admin nhập số tiền đối soát thực tế cho participation đã `Completed` và có ít nhất một submission `Approved`.
- Mỗi participation chỉ có một earning transaction; unique constraint bảo vệ khỏi tạo trùng.
- Tổng earning còn hiệu lực trong một campaign không được vượt `Campaign.BudgetAmount`; thao tác tạo chạy transaction `Serializable`.
- State machine MVP: `Pending -> Available | OnHold`, `OnHold -> Available`, `Available -> OnHold` khi có dispute.
- Chưa cho chuyển `PaidOut/Failed` vì payout, tài khoản ngân hàng và quy trình rút tiền chưa được thiết kế.
- Mọi thao tác tạo/chuyển trạng thái đều có audit và notification cho KOC.
- KOC `/Koc/Wallet` đã bỏ toàn bộ số dư/giao dịch hardcode; hiển thị Available, Pending, OnHold, tổng thu nhập và ledger thật.
- Admin `/Admin/Finance/Settlements` ưu tiên participation chưa đối soát, cho nhập tiền, ghi chú, release hoặc hold ngay trên card responsive.

## Database patches

- `16_patch_campaign_mvp_menu.sql`: menu campaign MVP.
- `17_patch_profile_review_menu.sql`: menu duyệt profile.
- `18_patch_matching_menu.sql`: menu matching.
- `19_create_campaign_package_policies.sql`: bảng package policy và finalize metadata.
- `20_patch_package_policy_menu.sql`: menu cấu hình package.
- `21_refine_campaign_package_policies.sql`: nâng cấp policy database cũ.
- `22_create_submission_workflow.sql`: backfill task cũ, unique index và menu execution.
- `23_allow_completed_participation.sql`: mở rộng CHECK constraint participation cho trạng thái `Completed`.
- `24_create_campaign_reviews_workflow.sql`: unique index review, menu Brand/KOC và role mapping. Migration này cần người vận hành chạy sau migration 23.
- `25_create_wallet_earning_settlements.sql`: bảng ledger earning, constraints/indexes, menu đối soát Admin và Wallet KOC. Migration đã parse-check nhưng chưa chạy.

## QA hiện tại

- Web build: thành công, 0 lỗi. Còn 3 nullable warning có sẵn trong module SocialSync, không thuộc workflow này.
- Unit tests: bao phủ campaign/profile/matching/package/submission, campaign completion và review rules.
- `git diff --check`: không có whitespace error.
- Database local: patch 22 đã chạy; có đủ 2 menu workflow và unique index submission.
- Database local: patch 23 đã chạy; CHECK constraint participation cho phép `Confirmed`, `Completed`, `Cancelled` và đang trusted.
- Review workflow QA: migration 24 hiện đã có trong database; unique index và 2 menu đã được xác nhận.
- Wallet MVP QA: 56/56 unit tests pass, Web build 0 error, migration 25 parse-check thành công và database hiện chưa có bảng `WalletTransactions`.

## Chưa làm / bước hợp lý tiếp theo

- Chưa có upload file trực tiếp; MVP dùng URL bài đăng.
- Lịch sử từng phiên bản URL/comment đã có trong `SubmissionHistories`; mỗi lần submit/review append snapshot URL, comment, revision, actor và thời điểm. Dòng “chưa có” trước đây đã lỗi thời.
- Chưa có API mobile cho workflow mới; service hiện đã tách dùng chung để API gọi lại.
- Chưa có nhánh Admin buộc đóng/hủy campaign khi có tranh chấp; hiện Brand chỉ đóng khi mọi đầu việc đã có kết quả cuối.
- Chưa có moderation/ẩn review từ Admin; schema đã có trạng thái `Hidden` để triển khai khi cần.
- Bước tiếp theo đề xuất sau khi migration 25 được xác nhận: payout request và thông tin tài khoản nhận tiền; cần chốt yêu cầu bảo mật/duyệt trước khi triển khai chuyển `Available -> PaidOut`.

## Payout request MVP

- Migration 25 đã được xác nhận trong database; bảng ledger và 2 menu tồn tại, chưa có giao dịch cũ cần backfill.
- Không đổi earning transaction thành `PaidOut`: payout là aggregate độc lập để hỗ trợ rút một phần hoặc gộp nhiều earning.
- Số dư khả dụng = tổng earning `Available` trừ payout `WithdrawRequested` và `PaidOut`; payout `Failed` tự hoàn số dư về khả dụng.
- KOC lưu một tài khoản nhận tiền. Số tài khoản được mã hóa bằng ASP.NET Data Protection; dữ liệu thường chỉ giữ/hiển thị 4 số cuối.
- Mỗi payout lưu snapshot tài khoản đã mã hóa tại thời điểm request, tránh việc cập nhật tài khoản làm thay đổi lệnh cũ.
- KOC nhập số tiền không vượt số dư khả dụng; tạo request trong transaction `Serializable` để tránh double-spend.
- Admin xem hàng đợi payout, xác nhận `PaidOut` kèm mã tham chiếu hoặc `Failed` kèm lý do. Kết quả cuối không thể đảo ngược trong MVP.
- Số tài khoản không được ghi vào audit log. Admin được giải mã tạm thời trong trang payout có role `Admin` để thực hiện chuyển khoản.
- Wallet có layout thống nhất: số dư + form rút, 4 KPI, tài khoản nhận tiền, lịch sử payout và earning ledger; responsive trên mobile.
- `26_create_payout_requests.sql`: tạo `KocPayoutAccounts`, `PayoutRequests`, constraints và indexes. File đã tạo, chưa chạy.
- QA: 61/61 unit tests pass, Web build 0 error.

## Lịch sử nghiệp vụ bất biến

- `AuditLogs` vẫn dùng để truy vết kỹ thuật, nhưng không còn là nguồn lịch sử duy nhất cho các domain quan trọng.
- `SubmissionHistories` lưu từng sự kiện `Submitted/Reviewed`, revision number, URL của phiên bản, phản hồi, trạng thái, actor và thời điểm.
- `WalletTransactionStatusHistories` lưu đầy đủ `FromStatus -> ToStatus`, lý do, actor và thời điểm cho earning settlement.
- `PayoutRequestStatusHistories` lưu đầy đủ vòng đời payout, note/mã tham chiếu hoặc lý do thất bại, actor và thời điểm.
- Bản ghi chính vẫn giữ snapshot hiện tại để query nhanh; bảng history là append-only để xem lại toàn bộ diễn biến.
- Foreign key không cascade delete: không thể xóa bản ghi chính rồi vô tình mất lịch sử.
- Database trigger chặn trực tiếp `UPDATE/DELETE` trên ba bảng history; chỉ cho phép append. Khi cần correction phải ghi sự kiện bù mới, không sửa quá khứ.
- Migration backfill một `MigratedSnapshot` cho mỗi submission/earning/payout hiện hữu chưa có history, nên dữ liệu cũ không bị bỏ trống timeline.
- KOC/Brand xem timeline submission ngay trong card; KOC xem timeline earning và payout bằng khối `<details>` gọn, không làm màn hình chính bị nặng.
- `27_create_domain_status_histories.sql`: tạo ba bảng history, indexes, FKs không cascade, append-only triggers và backfill. File chưa chạy.

## Review moderation và lịch sử kiểm duyệt

- Migration 27 đã được xác nhận: đủ ba bảng, ba trigger append-only hoạt động và submission cũ đã có `MigratedSnapshot`.
- Admin `/Admin/ReviewModeration` có bộ lọc Tất cả/Đang hiển thị/Đã ẩn, KPI, thông tin hai phía, rating, comment và timeline.
- State machine moderation chỉ cho `Completed -> Hidden` và `Hidden -> Completed`; không xóa review và không cho chuyển vòng sai.
- Ẩn review bắt buộc lý do; restore giữ lại toàn bộ lịch sử cũ.
- `ReviewStatusHistories` lưu `FromStatus`, `ToStatus`, lý do, actor và thời điểm; FK không cascade và trigger chặn update/delete.
- Tạo review mới ghi trạng thái `Completed` ban đầu vào history trong cùng transaction.
- Khi moderation, cập nhật review hiện tại + append history + audit + notification trong cùng transaction và conditional update chống hai Admin xử lý đồng thời.
- Rating dashboard chỉ tính review `Completed` và `IsVisible`, nên review Hidden lập tức không ảnh hưởng điểm.
- `28_create_review_moderation_history.sql`: tạo history, index, append-only trigger, backfill và menu Admin. File chưa chạy.
- QA: 62/62 unit tests pass, Web build 0 error.

## Campaign lifecycle histories

- Migration 28 đã được xác nhận: `ReviewStatusHistories`, append-only trigger và menu moderation đều tồn tại.
- `CampaignStatusHistories` lưu toàn bộ vòng `Draft -> Submitted -> Published/NeedEdit/Rejected -> InProgress -> Completed` cùng note, actor và thời điểm.
- `ApplicationStatusHistories` lưu `Applied/Withdrawn/Reapply/Shortlisted/Confirmed/NotSelected` thay vì chỉ giữ kết quả cuối.
- `ParticipationStatusHistories` lưu thời điểm participation được tạo `Confirmed` và chuyển `Completed/Cancelled`.
- Mọi transition trong CampaignService, CampaignApplicationService, MatchingService và DeliverableWorkflowService append history trong cùng lần SaveChanges/transaction với snapshot hiện tại.
- Ba bảng history có FK không cascade, index theo aggregate + thời điểm, trigger append-only và backfill `Migrated snapshot` cho dữ liệu cũ.
- Brand xem timeline campaign ngay tại `/Brand/Campaigns`; KOC xem timeline apply/matching ngay tại `/Koc/Jobs` bằng `<details>` responsive.
- Review schema phát hiện CHECK constraint `Campaigns.Status` thiếu `Rejected` dù code hỗ trợ quyết định này. Migration 29 thay constraint bằng bộ trạng thái canonical có `Rejected`.
- `29_create_campaign_lifecycle_histories.sql`: sửa Campaign status constraint, tạo ba bảng lifecycle history, indexes, triggers và backfill. File chưa chạy.

## Profile status histories

- Migration 29 đã được xác nhận trong database: đủ ba bảng lifecycle history, dữ liệu hiện hữu đã được backfill, các trigger append-only đang bật và CHECK constraint Campaign đã hỗ trợ `Rejected`.
- `BrandProfileStatusHistories` và `KocProfileStatusHistories` lưu rõ `FromStatus -> ToStatus`, review note và thời điểm UTC cho toàn bộ vòng đời hồ sơ.
- Lịch sử được bắt tại database bằng trigger trên `BrandProfiles`/`KocProfiles`, nên không bỏ sót thay đổi trạng thái từ Web hiện tại, API hoặc background job trong tương lai.
- Hai bảng history là append-only: trigger chặn `UPDATE/DELETE`, foreign key không cascade và index theo profile + thời gian. Correction phải được ghi bằng sự kiện mới, không sửa quá khứ.
- Migration backfill một snapshot cho từng hồ sơ cũ chưa có history; chạy lại migration không tạo snapshot trùng.
- Admin xem timeline ngay trong trang duyệt hồ sơ, hiển thị rõ trạng thái nguồn, trạng thái đích, thời gian và ghi chú. Actor của quyết định Admin tiếp tục được lưu trong `AuditLogs`; history hồ sơ ưu tiên bảo đảm không bỏ sót transition ở mọi đường ghi dữ liệu.
- `30_create_profile_status_histories.sql`: tạo hai bảng history, constraints/indexes, backfill, trigger append-only và trigger tự động capture. File đã tạo, chưa chạy.

## Shipment workflow MVP

- Migration 30 đã được xác nhận trong database: Brand có 1 snapshot, KOC có 2 snapshot và đủ bốn trigger capture/append-only đang hoạt động.
- Chọn hoàn thiện Shipment trước Booking để mỗi workflow được khép kín. Chỉ participation `Confirmed` thuộc campaign bật `RequiresShipment` mới được tạo vận đơn.
- Mỗi participation tối đa một shipment; service kiểm tra trước và unique index tại database chống tạo trùng do hai request đồng thời.
- Khi tạo shipment, tên, số điện thoại và địa chỉ được copy thành snapshot trên vận đơn. KOC đổi hồ sơ sau đó không làm sai địa chỉ của đơn hàng cũ.
- State machine MVP: `Created -> InTransit -> Delivered`; KOC có thể chuyển `Created/InTransit -> Issue`; Brand xử lý `Issue -> InTransit/Returned`. Không cho sửa ngược trạng thái cuối.
- Brand có màn hình card responsive để tạo vận đơn, nhập carrier/tracking, ưu tiên sự cố và xử lý tiếp tục giao/hoàn hàng. KOC có màn hình theo dõi mã vận đơn, xác nhận đã nhận hoặc mở form báo sự cố gọn bằng collapse.
- `ShipmentStatusHistories` lưu `FromStatus -> ToStatus`, note, actor và thời điểm trong cùng lần lưu với shipment snapshot, notification và audit log.
- `Shipments.Status` là concurrency token; hai thao tác đồng thời từ cùng trạng thái không thể cùng ghi thành công và tạo timeline phân nhánh sai.
- History có FK không cascade, index theo shipment + thời gian và trigger chặn `UPDATE/DELETE`. Migration backfill snapshot cho shipment cũ; correction phải append sự kiện mới.
- `31_create_shipment_workflow_history.sql`: tạo unique index, bảng history, backfill, append-only trigger và menu cho Brand/KOC. File đã tạo, chưa chạy.

## Booking / Schedule workflow MVP

- Migration 31 đã được xác nhận: unique shipment theo participation, 1 history snapshot, append-only trigger và menu Brand/KOC đều hoạt động đúng.
- Mỗi participation `Confirmed` thuộc campaign bật `RequiresBooking` có tối đa một schedule; unique index database chống tạo trùng do request đồng thời.
- State machine: `Proposed -> Confirmed`, đổi lịch tạo `Rescheduled`, hủy tạo `Cancelled` terminal. `Proposed/Confirmed/Rescheduled` được đổi lịch tối đa 3 lần.
- Bên tạo đề xuất gần nhất không được tự xác nhận. Brand đề xuất thì KOC xác nhận; KOC counter-propose thì Brand xác nhận, dựa trên actor của event gần nhất.
- Thời gian nhập theo múi giờ nghiệp vụ Việt Nam, lưu UTC; bắt đầu phải sau hiện tại ít nhất 15 phút, kết thúc sau bắt đầu, thời lượng tối đa 24 giờ và nằm trong khung ngày campaign.
- `ScheduleStatusHistories` lưu snapshot thời gian của từng phiên bản cùng `FromStatus -> ToStatus`, lý do, actor và timestamp độ chính xác cao. Vì vậy đổi lịch không làm mất khung giờ cũ.
- History có FK không cascade, index theo schedule + thời gian và trigger append-only. `Schedules.Status` là concurrency token để hai phía không thể đồng thời tạo timeline phân nhánh.
- Layout Brand/KOC dùng card responsive, hành động chính hiển thị trực tiếp; form đổi/hủy và timeline được collapse/details để màn hình gọn trên mobile.
- `32_create_booking_workflow_history.sql`: tạo unique index, history/backfill/trigger và hai menu Booking. File đã tạo, chưa chạy.
- Campaign completion đã được siết lại: campaign yêu cầu booking chỉ hoàn tất khi mọi participation có schedule `Confirmed`; campaign yêu cầu shipment chỉ hoàn tất khi mọi shipment là `Delivered`.

## Admin Operations exception desk

- Migration 32 đã được xác nhận: schedule cũ có history snapshot, unique index, append-only trigger và menu Booking cho Brand/KOC đều hoạt động.
- Admin `/Admin/Operations` gom hai hàng đợi cần can thiệp: shipment `Issue` và booking đã quá giờ chưa xác nhận hoặc đã đổi lịch từ 2 lần.
- Shipment issue có thể được Admin quyết định `Issue -> InTransit/Returned`; booking có thể bị Admin buộc `-> Cancelled`. Mọi can thiệp bắt buộc ghi rõ kết quả/lý do.
- Admin không sửa hoặc xóa event cũ. Transition mới được append vào `ShipmentStatusHistories`/`ScheduleStatusHistories` với actor Admin, snapshot thời gian hiện tại và tiền tố note `Admin:` để timeline dễ đọc.
- Cập nhật aggregate, append history, audit log và notification cho cả Brand/KOC nằm trong cùng `SaveChanges`; concurrency token chặn xử lý trùng khi Brand/KOC vừa thao tác.
- Layout ưu tiên ngoại lệ khẩn cấp, có KPI số lượng, card responsive và timeline thu gọn. Hành động buộc hủy được đặt trong `<details>` để tránh bấm nhầm.
- `33_patch_admin_operations_menu.sql`: chỉ thêm menu và role mapping Admin; không tạo history mới vì tái sử dụng hai bảng history chuẩn đã có. File đã tạo, chưa chạy.

## Dispute / Support workflow MVP

- Migration 33 đã được xác nhận: menu Admin Operations có đúng role mapping và hàng đợi hiện nhận diện 1 booking quá hạn chưa xác nhận.
- `DisputeCases` là aggregate riêng gắn với participation, lưu người mở, category, tiêu đề/mô tả gốc, priority, Admin phụ trách, trạng thái hiện tại và kết luận.
- State machine: `Open -> InReview -> Resolved/Rejected`; Admin có thể quyết định trực tiếp từ `Open`; người tạo chỉ được `Open -> Cancelled` trước khi Admin tiếp nhận.
- Cột `IsActive` có CHECK constraint đồng bộ với status và filtered unique index `(ParticipationId, Category, IsActive) WHERE IsActive=1`, chặn hai dispute hoạt động cùng loại cho một participation ngay tại database.
- `DisputeCaseStatusHistories` append từng `FromStatus -> ToStatus`, note, actor và timestamp `datetime2(7)`; FK không cascade và trigger chặn `UPDATE/DELETE`.
- Brand/KOC dùng portal card responsive chung: form mở được collapse, danh sách case ưu tiên active, kết luận và timeline rõ ràng. Admin có filter trạng thái, priority badge, tiếp nhận và ghi kết luận bắt buộc.
- Mọi transition lưu aggregate + history + audit + notification trong cùng lần; notification gửi cả hai phía khi Admin tiếp nhận/kết luận hoặc case bị hủy.
- Category `Payment`/`Shipment` được ưu tiên `High`, nhưng MVP không tự động giữ tiền khi mới mở dispute; Admin vẫn dùng Finance Settlements để tránh khóa tiền sai khi chưa xác minh.
- `34_create_dispute_workflow.sql`: tạo hai bảng, constraints/indexes, append-only trigger và ba menu theo role. File đã tạo, chưa chạy.

## Dispute messages và evidence journal

- Migration 34 đã được xác nhận: schema/index/trigger và ba menu dispute đúng; database hiện chưa có case cũ cần backfill.
- Mỗi case có trang detail riêng cho Brand/KOC/Admin, tách khỏi list để màn hình hàng đợi vẫn nhẹ và hồ sơ điều tra có đủ không gian.
- `DisputeMessages` lưu từng phản hồi, author, role snapshot, nội dung, evidence URL, visibility và timestamp `datetime2(7)`; không sửa message cũ khi có bổ sung.
- `Visibility=Parties` hiển thị cho cả hai bên; `AdminOnly` chỉ Admin đọc được và CHECK constraint đảm bảo chỉ author role `ADMIN` mới tạo được.
- Evidence chỉ nhận URL tuyệt đối `http/https`, tối đa 1.000 ký tự; link mở tab mới với `noopener noreferrer`.
- Trigger append-only chặn `UPDATE/DELETE`. Trigger `ActiveCaseOnly` chặn INSERT vào case đã đóng, bảo vệ cả race condition và đường ghi ngoài Web.
- Case `Resolved/Rejected/Cancelled` chuyển trang detail sang chỉ đọc; nội dung trao đổi và bằng chứng cũ vẫn được giữ nguyên.
- Phản hồi public tạo notification đúng đối tượng; ghi chú nội bộ không phát notification và audit chỉ lưu metadata `HasEvidence/Visibility`, không copy nội dung nhạy cảm.
- `35_create_dispute_messages.sql`: tạo journal, constraints/index, append-only trigger và active-case trigger. File đã tạo, chưa chạy.

## Dispute guard và Admin campaign cancellation

- Migration 35 đã được xác nhận: `DisputeMessages` tồn tại, journal hiện rỗng, append-only và active-case trigger đều bật, đủ ba CHECK constraint role/visibility.
- Brand không thể hoàn tất campaign khi bất kỳ participation nào còn `DisputeCase.IsActive=1`; summary hiển thị đúng lý do thay vì chỉ vô hiệu hóa nút.
- Admin chỉ được hủy campaign `InProgress` từ một dispute đang hoạt động. Form nằm trong vùng cảnh báo thu gọn, bắt buộc lý do 10–900 ký tự và có xác nhận lần cuối.
- `Campaigns` lưu liên kết cấu trúc `CancellationDisputeCaseId`, `CancelledByUserId`, `CancelledAtUtc`, `CancellationReason`; không phụ thuộc việc đọc chuỗi note để truy ra nguyên nhân.
- Hủy campaign chạy transaction `Serializable`: `Campaign -> Cancelled`, participation `Confirmed -> Cancelled`, schedule active `-> Cancelled`, earning `Pending/Available -> OnHold`; mỗi aggregate append history với actor Admin.
- Dispute `Open` được chuyển `InReview` khi Admin dùng nó để hủy campaign. Audit chỉ ghi case/campaign/count và lý do; Brand cùng toàn bộ KOC liên quan nhận notification.
- Shipment không tự chuyển trạng thái khi hủy campaign vì hàng có thể đang vận chuyển; Admin/Brand tiếp tục xử lý giao hoặc hoàn trong Shipment workflow để history phản ánh đúng vật lý.
- CHECK constraint cancellation metadata yêu cầu campaign `Cancelled` có đủ dispute, Admin, thời điểm và lý do. Hai FK không cascade và filtered index hỗ trợ truy ngược campaign theo dispute.
- `36_add_campaign_dispute_cancellation.sql`: thêm cancellation metadata, FKs, index và CHECK constraint. File đã tạo, chưa chạy.
- Hotfix migration 36: SQL Server báo `Invalid column name` khi ADD column và tạo FK/index/CHECK trong cùng batch. Các DDL phụ thuộc cột mới đã chuyển sang dynamic SQL, có prerequisite migration 34 và `TRY/CATCH` rollback; lần chạy lỗi được xác nhận chưa để lại thay đổi dở dang.

## Structured dispute wallet holds

- Migration 36 đã được xác nhận: đủ 4 cancellation columns, 2 FK, filtered index và trusted CHECK; `CancellationReason` có `COL_LENGTH=2000` byte đúng với `NVARCHAR(1000)`.
- `WalletTransactions.HoldDisputeCaseId` biểu diễn dispute đang giữ tiền hiện tại; khi earning được release thì cột này trở về null.
- `WalletTransactionStatusHistories.DisputeCaseId` giữ liên kết vĩnh viễn trên cả event `-> OnHold` và `OnHold -> Available`, nên không phải parse GUID từ `HoldReason`.
- Khi Admin hủy campaign do dispute, mọi earning `Pending/Available` chuyển `OnHold` cùng FK case. Manual hold từ Finance không giả mạo liên kết dispute.
- Admin Dispute Detail hiển thị số khoản và tổng tiền đang giữ. Chỉ case `Resolved/Rejected` mới mở form giải phóng; lý do 10–900 ký tự và confirm bắt buộc.
- Release chạy transaction `Serializable`, chuyển đúng các wallet còn `OnHold` bởi case đó về `Available`, append history có dispute FK, audit tổng tiền/count và notification cho KOC.
- CHECK constraint đảm bảo wallet có `HoldDisputeCaseId` chỉ khi status `OnHold`; hai FK không cascade và filtered indexes hỗ trợ điều tra theo case.
- `37_link_wallet_holds_to_disputes.sql`: thêm 2 FK columns, indexes và CHECK; yêu cầu migration 27 và 34. File đã tạo, chưa chạy.

## Notification Center và read history

- Migration 37 đã được xác nhận: đủ 2 FK columns, 2 filtered indexes và trusted CHECK; database không có wallet transaction cũ cần backfill.
- Trước bước này, nhiều workflow đã tạo `Notifications` nhưng Admin/Brand/KOC chưa có inbox để đọc; dữ liệu tồn tại nhưng không đến được người dùng qua UI.
- Ba trang `/Admin/Notifications`, `/Brand/Notifications`, `/Koc/Notifications` dùng chung layout responsive: unread KPI, lọc Chưa đọc/Tất cả, đánh dấu từng item hoặc đọc tất cả.
- Inbox giới hạn 100 item mới nhất theo bộ lọc, ưu tiên unread trước để tránh màn hình dài và query không giới hạn.
- `NotificationEvents` append `Created/Read/Hidden`, actor và timestamp `datetime2(7)`; FK không cascade, index theo notification + thời gian và trigger chặn `UPDATE/DELETE`.
- Migration backfill event `Created` cho mọi notification cũ và `Read` cho các item đã đọc; chạy lại không tạo trùng các event backfill.
- Trigger trên `Notifications AFTER INSERT` tự append `Created`, bảo đảm notification từ mọi module hiện tại và job/API tương lai đều có lịch sử khởi tạo.
- Mark read dùng conditional update theo đúng `UserId`; read-all chạy transaction `Serializable`, chỉ append event cho tập unread đã khóa, tránh hai request tạo timeline đọc trùng.
- Notification đã đọc vẫn được giữ nguyên; UI hiển thị thời điểm tạo/đọc và chỉ mở timeline khi có nhiều hơn một event.
- `38_create_notification_inbox_history.sql`: đã chạy và xác nhận thành công. Database có 2 notification cũ và đúng 2 event `Created`; hai trigger append-only/capture-created đều đang bật; ba menu và role mapping đúng.

## Notification archive, restore và actionable routing

- Inbox có ba bộ lọc `Chưa đọc / Tất cả / Đã lưu trữ`; layout card responsive ưu tiên nút `Mở công việc`, còn đọc/lưu trữ/khôi phục là hành động phụ để thao tác nhanh cả trên mobile.
- Lưu trữ không xóa notification. Snapshot hiện tại dùng `IsVisible` + `HiddenAtUtc`; từng lần chuyển trạng thái append event `Hidden` hoặc `Restored` vào `NotificationEvents`, giữ được đầy đủ các vòng lưu trữ/khôi phục.
- CHECK constraint buộc `IsVisible` và `HiddenAtUtc` đồng nhất. History vẫn có FK không cascade và trigger append-only nên không thể sửa/xóa quá khứ.
- `TargetUrl` là đường dẫn nội bộ tối đa 500 ký tự. Cả database và service đều chặn URL ngoài hệ thống hoặc protocol-relative (`//...`) để không tạo open redirect.
- Notification cũ chưa có `TargetUrl` được định tuyến an toàn theo `Role + Type` tới Jobs, Tasks, Shipment, Booking, Dispute, Wallet hoặc Operations; không ghi ngược một liên kết suy đoán vào dữ liệu lịch sử.
- Index `(UserId, IsVisible, CreatedAtUtc DESC)` kèm các cột hiển thị hỗ trợ ba inbox tải tối đa 100 item mà không quét toàn bảng.
- `39_add_notification_archive_routing.sql`: thêm hai cột, backfill mốc lưu trữ cho dữ liệu cũ, mở rộng event type `Restored`, thêm constraints và index. File đã tạo, chưa chạy.
- QA sau thay đổi: build 0 error (còn 3 nullable warning có sẵn trong SocialSync); 72/72 unit tests pass.

## Notification source correlation và immutable identity

- Migration 39 đã được xác nhận trong database: `HiddenAtUtc` 8 byte, `TargetUrl` 1.000 byte đúng `NVARCHAR(500)`, index inbox tồn tại và hai CHECK constraint đều trusted; chưa có event `Restored` vì chưa phát sinh thao tác khôi phục.
- Phát hiện và sửa góc open redirect: service giờ từ chối URL quá 500 ký tự, backslash và mọi control character; database từ chối backslash, tab, CR, LF ngoài quy tắc đường dẫn nội bộ bắt đầu bằng đúng một `/`.
- Notification mới có cặp `SourceEntityType + SourceEntityId`, trỏ có cấu trúc về aggregate phát sinh thay vì parse tiêu đề/body. Đã chuẩn hóa nơi tạo notification qua `NotificationFactory` cho matching, submission, campaign completion, review, shipment, schedule, operations, dispute, wallet và payout.
- CHECK `SourcePair` bắt buộc hai cột source cùng null hoặc cùng có giá trị. Dữ liệu cũ không đủ căn cứ liên kết được giữ null, không đoán và không làm sai lịch sử.
- Filtered index theo source hỗ trợ điều tra toàn bộ notification của một aggregate. Inbox hiển thị loại nguồn và 8 ký tự đầu ID để support đối chiếu nhanh mà không làm card nặng.
- Trigger `TR_Notifications_ImmutableIdentity` chặn sửa người nhận, loại, tiêu đề, nội dung, target và source sau khi tạo. Read/archive vẫn cập nhật snapshot hiện tại; diễn biến tiếp tục append trong `NotificationEvents`.
- `40_add_notification_source_correlation.sql`: đã chạy và xác nhận thành công. Source columns đúng kích thước, filtered index tồn tại, hai CHECK constraint trusted, immutable trigger đang bật và không có source pair sai.

## Notification open history

- Nút `Mở công việc` không còn điều hướng GET trực tiếp. UI gửi POST có anti-forgery tới PageModel, service kiểm tra notification thuộc đúng user, còn visible và target vẫn là đường dẫn nội bộ an toàn trước khi redirect.
- Khi mở notification chưa đọc, service cập nhật snapshot read và append `Read`; sau đó luôn append `Opened`. Cả hai nằm trong transaction `Serializable`, nên lịch sử không bị phân nhánh hoặc ghi mở cho notification vừa bị archive ở request khác.
- `Opened` được ghi cho từng lần mở, không chỉ lần đầu. Timeline vì vậy phản ánh được mức độ người dùng quay lại xử lý công việc mà không sửa/xóa event trước.
- Notification không có target phù hợp ở lại inbox và trả thông báo rõ ràng; không tạo event mở giả và không redirect về một trang đoán sai.
- Layout vẫn giữ một nút chính `Mở công việc`; read/archive là hành động phụ. Timeline đổi nhãn kỹ thuật `Opened` thành `Đã mở công việc`, dễ hiểu trên cả desktop và mobile.
- `41_add_notification_open_history.sql`: đã chạy và xác nhận thành công. CHECK constraint trusted đã cho phép `Opened`; database hiện chưa có event mở vì chưa phát sinh thao tác sau triển khai.
- QA: build 0 error, còn 3 nullable warning có sẵn trong SocialSync; 72/72 unit tests pass.

## Notification role-safe deep link và target snapshot

- Sửa lỗi user có nhiều role: inbox không còn lấy role đầu tiên không xác định từ `UserRoles`. Mỗi portal truyền role cố định `ADMIN/BRAND/KOC`, service xác minh user thật sự có role đó trước khi trả inbox hoặc mở notification.
- Deep-link ưu tiên source có cấu trúc: dispute mở thẳng hồ sơ; campaign mở đúng trang Admin review, Brand edit hoặc KOC detail; shortlist, submission, participation và application resolve campaign thật từ database trước khi tạo URL.
- Không query N+1 khi render inbox: màn hình chỉ chuẩn bị preview target nhẹ; lookup quan hệ chi tiết chỉ chạy cho đúng một notification khi user bấm mở.
- `NotificationEvents.TargetUrlSnapshot` lưu chính xác URL nội bộ đã mở tại thời điểm event `Opened`. Timeline hiển thị đích dưới event, phục vụ support/audit ngay cả khi quy tắc routing thay đổi sau này.
- CHECK constraint bắt buộc chỉ event `Opened` mới có target snapshot và target phải là đường dẫn nội bộ an toàn; mọi event khác bắt buộc snapshot null. History tiếp tục append-only, không update/backfill bằng suy đoán.
- `42_add_notification_open_target_snapshot.sql`: đã chạy và xác nhận thành công. Cột đúng `NVARCHAR(500)`, CHECK trusted và không có event vi phạm quy tắc target snapshot.

## Notification audience role và actor role history

- Sửa lỗi multi-role còn lại: notification mới lưu `AudienceRole=ADMIN/BRAND/KOC`; mỗi inbox chỉ lấy notification legacy chưa có role hoặc notification đúng role của portal hiện tại. Notification của role này không còn lẫn vào portal role khác cùng user.
- Toàn bộ producer hiện tại đã gắn audience rõ ràng: matching, deliverable, review/moderation, shipment, booking, operations, dispute, wallet và payout. `NotificationFactory` chuẩn hóa role thành uppercase.
- Trigger database từ chối mọi notification mới thiếu `AudienceRole`. Dữ liệu cũ giữ null để tương thích và không suy đoán sai; CHECK chỉ cho null legacy hoặc ba role canonical.
- Mọi event thao tác `Read/Opened/Hidden/Restored` lưu `ActorRoleSnapshot`; event `Created` do database sinh không giả actor/role và bắt buộc role snapshot null.
- Timeline hiển thị badge role cạnh thao tác, nên support phân biệt cùng một user đã đọc/mở/lưu trữ ở portal Admin, Brand hay KOC.
- `AudienceRole` được thêm vào tập thuộc tính identity bất biến. Không thể chuyển một notification đã tạo từ role này sang role khác; correction phải tạo notification mới và giữ bản cũ trong lịch sử.
- Index `(UserId,AudienceRole,IsVisible,CreatedAtUtc DESC)` phục vụ inbox role-scoped. CHECK event buộc non-created event phải có actor role canonical.
- `43_add_notification_role_snapshots.sql`: thêm hai role snapshots, constraints/index và hai trigger bảo vệ. File đã tạo, chưa chạy.
- QA sau role scoping tại thời điểm triển khai: build 0 error/0 warning; 72/72 unit tests pass.

## Submission overdue operational escalation

- Migration 43 đã được xác nhận trong database: role columns đúng `NVARCHAR(10)`, index và hai trusted CHECK tồn tại, hai trigger đang bật; 2 notification/event cũ vẫn giữ role null theo chính sách legacy.
- Admin Operations có khu vực `Đầu việc quá hạn` riêng, dùng partial responsive để không làm hai queue Shipment/Booking cũ bị rối. Admin chủ động bấm `Quét deadline hiện tại`; GET dashboard không âm thầm ghi database.
- Rule MVP phát hiện submission thuộc campaign `InProgress`, deadline đã qua và còn `NotSubmitted/NeedRevision/Submitted`. Quá hạn dưới 48 giờ là `High`, từ 48 giờ là `Critical`.
- `OperationalEscalations` giữ snapshot hiện tại `Open/Acknowledged/Resolved`, thời điểm phát hiện đầu/gần nhất, severity và actor/timestamp tiếp nhận hoặc đóng.
- `OperationalEscalationHistories` append mọi lifecycle: mở mới, Admin tiếp nhận, tăng severity, tự đóng khi nguồn hết quá hạn và mở lại nếu quá hạn tái diễn. Vì vậy card có thể biến khỏi active queue nhưng hồ sơ quá khứ không mất.
- Một rule/source chỉ có một aggregate nhờ unique index; scan chạy `Serializable`, không tạo escalation/notification trùng khi hai Admin quét đồng thời.
- Khi tạo escalation lần đầu, Brand và KOC nhận notification role-scoped gắn source `Submission`. Scan lại chỉ cập nhật `LastDetectedAtUtc`, không spam notification.
- Aggregate có FK nguồn tới `Submissions` không cascade; history có FK không cascade, index theo escalation + timestamp và trigger append-only. Trigger source validation bổ sung bảo vệ rule/type, nên không thể tạo hồ sơ mồ côi hoặc xóa nguồn làm mất khả năng điều tra.
- Layout card ưu tiên Critical, hiển thị campaign/KOC/Brand/deadline/trạng thái, form tiếp nhận ngay trên card và timeline thu gọn; ghi chú tiếp nhận bắt buộc 5–1.000 ký tự.
- `44_create_operational_escalation_history.sql`: đã chạy và xác nhận thành công. Có 2 bảng, 5 FK không cascade, 7 trusted CHECK, 5 named index và 2 trigger đang bật; chưa có escalation/history vì chưa thực hiện lần quét đầu tiên.
- Hotfix UX: notification `SubmissionOverdue` đã có target cho cả hai portal; Brand resolve campaign thật rồi mở Deliverables, KOC mở Tasks. Không còn cảnh báo quá hạn không có nút hành động.

## Operational scan-run journal

- Mỗi lần Admin quét submission quá hạn tạo một `OperationalScanRuns` trong cùng transaction với escalation/history/notification. Bản ghi lưu rule, actor, thời điểm bắt đầu/kết thúc và các số `Detected/Created/Resolved/Critical`.
- Scan run là append-only và có FK actor không cascade; không thể sửa số liệu một lần quét sau khi transaction đã commit. CHECK constraint buộc thời gian/count hợp lệ và chỉ nhận rule canonical `SUBMISSION_OVERDUE`.
- Index theo `RuleCode + CompletedAtUtc DESC` hỗ trợ lấy 10 lần quét gần nhất mà không quét toàn journal.
- Admin Operations hiển thị rõ lần quét gần nhất, người thực hiện và kết quả ngay dưới tiêu đề queue. Khi chưa có journal, UI cảnh báo `Chưa có lịch sử quét deadline` thay vì khiến Admin hiểu nhầm queue rỗng là dữ liệu vừa được kiểm tra.
- `45_create_operational_scan_run_journal.sql`: đã chạy và xác nhận thành công. Có FK actor không cascade, 3 trusted CHECK, index theo rule/time và append-only trigger; chưa có run vì chưa bấm quét.

## Dispute SLA escalation và typed sources

- Refactor nguồn escalation trước khi mở rộng: thêm `SubmissionId` và `DisputeCaseId` có FK không cascade. CHECK buộc `SUBMISSION_OVERDUE` chỉ dùng SubmissionId, `DISPUTE_SLA` chỉ dùng DisputeCaseId và typed ID phải bằng `SourceEntityId` correlation.
- Không dùng một GUID polymorphic không FK làm nguồn thật. `SourceEntityId` chỉ phục vụ contract/query chung; FK typed mới bảo đảm không có history mồ côi và không xóa aggregate nguồn khi escalation còn tồn tại.
- Rule `DISPUTE_SLA` phát hiện case active `Open/InReview` không có hoạt động từ 24 giờ; từ 48 giờ chuyển `Critical`. Hoạt động được tính từ mốc case gần nhất hoặc message mới nhất.
- Scan dispute chạy `Serializable`, tạo/tăng severity/mở lại/tự đóng bằng cùng lifecycle và `OperationalEscalationHistories` append-only. Case có message mới hoặc kết thúc được tự chuyển escalation sang `Resolved`, không biến mất âm thầm.
- `OperationalScanRuns` mở rộng rule canonical `DISPUTE_SLA`; lưu riêng detected/created/resolved/critical và actor cho từng lần quét.
- Admin Operations có section responsive riêng: thời điểm/kết quả scan gần nhất, badge SLA, nút mở thẳng Dispute Detail, tiếp nhận ngay trên card và timeline thu gọn.
- Escalation mới gửi notification `DisputeSla` role Admin cho assigned Admin; nếu chưa phân công thì gửi Admin đang quét. Notification gắn source `DisputeCase`, nên deep-link và open history hoạt động đầy đủ.
- `46_add_dispute_sla_escalations.sql`: backfill typed SubmissionId, thêm typed FKs/check/index, mở rộng scan rule và source validation trigger. File đã tạo, chưa chạy.
- QA: build 0 error, còn 3 nullable warning có sẵn trong SocialSync; 72/72 unit tests pass; database xác nhận migration 46 chưa chạy hoặc chạy dở.

## Đối chiếu lại tài liệu Word theo vai trò

- Đã đọc trực tiếp ba tài liệu `Tổng quan luồng Admin.docx`, `Tổng quan luồng Brand.docx`, `Tổng quan luồng KOC.docx`; từ đây không dùng số trang hiện có để kết luận một module đã hoàn thiện.
- Luồng marketplace cốt lõi đã khép kín ở mức MVP: onboarding → campaign → matching → booking/shipment → submission → review/dispute → wallet. Toàn bộ phạm vi Word chưa hoàn thiện 100%.
- KOC còn thiếu các phần chính: OTP onboarding/rút tiền, đủ 7 bước CV có dữ liệu thật, filter/sort campaign nâng cao, reminder/late cancel, carrier tracking chi tiết, upload evidence, review reply/appeal và giới hạn/phí payout.
- Brand còn thiếu: campaign form nhiều deliverable/deadline và targeting đầy đủ, checklist NeedEdit có cấu trúc, calendar, carrier integration, KOC collaboration history riêng, billing/phí/hóa đơn và báo cáo vận hành đầy đủ.
- Admin còn thiếu: form builder, notification template, user detail/ranking/violation, calendar tổng, reminder automation, carrier mapping/API hoàn chỉnh, scoring/ranking, operational reports và billing 15%.
- Quy tắc review từ nay: chỉ đánh dấu hoàn thiện khi có UI usable, authorization, validation, transaction/concurrency phù hợp, database constraint/index, lịch sử cho dữ liệu quan trọng và kiểm thử tương xứng.

## Operational SLA policy và lịch sử cấu hình

- Thay ngưỡng SLA hard-code bằng `OperationalSlaPolicies`: mỗi rule có High/Critical hours, trạng thái bật/tạm dừng, actor cập nhật và mốc hiệu lực.
- `OperationalSlaPolicyHistories` lưu snapshot trước/sau, lý do, actor và thời điểm. History append-only; policy identity không được đổi/xóa để correlation giữa các lần quét luôn ổn định.
- Seed giữ nguyên hành vi cũ: submission High ngay khi quá deadline/Critical 48h; dispute High 24h/Critical 48h.
- Admin Operations hiển thị policy trước các queue, có form responsive, validation rõ, trạng thái hiệu lực và timeline thu gọn. Thay đổi bắt buộc lý do 5–1.000 ký tự.
- Scan từ chối chạy nếu policy thiếu hoặc đang tạm dừng; audit của scan lưu snapshot ngưỡng đã sử dụng, tránh không giải thích được kết quả sau khi policy đổi.
- `47_create_operational_sla_policy_history.sql`: tạo hai bảng, FK không cascade, CHECK/index, hai trigger bảo vệ và seed hai policy. Cần chạy sau khi migration 46 hoàn tất.
- Review sau triển khai xác nhận migration 46 thực tế đã hoàn tất: FK submission mang tên `FK_OperationalEscalations_SubmissionTyped`, index dispute mang tên `IX_OperationalEscalations_DisputeCase`; câu verify cũ dùng tên dự kiến nên báo thiếu sai. Migration 47 cũng đã có đủ 2 policy và 2 trigger đang bật.
- Rule biên SLA được tách thành `OperationalSlaRules` và có unit test riêng: High/Critical phải hợp lệ, đúng thời điểm chạm ngưỡng được tính inclusive, trước ngưỡng một phút chưa bị nâng mức.
- Hạng mục ưu tiên kế tiếp: background scan có database lease chống nhiều instance chạy trùng, system actor minh bạch và hiển thị tình trạng lần chạy tự động trên Operations. Không dùng một tài khoản Admin giả làm actor hệ thống.

## KOC campaign discovery filters

- Đối chiếu tài liệu Word cho thấy trang cơ hội trước đây chỉ tải toàn bộ campaign Published theo ngày tạo, chưa có filter/sort như mô tả.
- Bổ sung query filter chạy tại SQL cho từ khóa tên campaign/Brand, khoảng ngân sách, yêu cầu booking, yêu cầu shipment và loại deliverable; không tải toàn bộ dữ liệu về Web để lọc trong bộ nhớ.
- Sort canonical gồm mới nhất, ngân sách cao, ngân sách thấp và sắp hết hạn; mọi sort có tie-break theo thời điểm tạo để kết quả ổn định.
- Query string được chuẩn hóa: cắt khoảng trắng/độ dài, loại giá trị âm, tự đổi hai đầu khoảng ngân sách nếu nhập ngược và đưa logistics/sort lạ về mặc định.
- Layout filter dùng `<details>` gọn trên mobile, tự mở khi đang có điều kiện; hiển thị số kết quả, badge logistics/deliverable và empty state có nút xóa bộ lọc.
- Form rút đăng ký giữ nguyên filter hiện tại khi quay lại danh sách, tránh KOC mất ngữ cảnh đang tìm kiếm.
- Không cần migration SQL cho hạng mục này vì dùng các cột/index campaign hiện hữu.

## Booking reminders và late cancellation

- Booking `Confirmed` được worker kiểm tra mỗi 5 phút để gửi reminder cho cả Brand/KOC ở cửa sổ 24 giờ và 2 giờ trước lịch.
- `ScheduleReminderEvents` là journal append-only. Unique key gồm schedule + reminder code + snapshot giờ bắt đầu: không gửi trùng khi nhiều instance chạy, nhưng một lịch đã reschedule vẫn được nhắc cho mốc giờ mới.
- Reminder event và hai notification được ghi trong cùng `SaveChanges`; request GET của trang Booking không ghi database.
- Hủy lịch được xem là late cancellation khi lịch đang `Confirmed` và thời điểm hủy nằm trong 24 giờ trước giờ bắt đầu hoặc đã qua giờ. Proposal chưa được hai bên chốt không bị gắn late.
- `Schedules` lưu snapshot `IsLateCancellation`, ngưỡng 24 giờ, actor và thời điểm hủy. Timeline status đồng thời ghi tiền tố rõ ràng trong note; thay đổi policy tương lai không làm đổi cách hiểu booking cũ.
- Brand/KOC thấy cảnh báo late cancellation ngay trên card. Dữ liệu cancelled cũ chỉ backfill thời điểm từ snapshot sẵn có, không đoán actor và không tự gắn late.
- Migration `48_add_booking_reminders_late_cancellation.sql` thêm metadata, FK/check, reminder journal/index và append-only trigger. Cần chạy trước khi khởi động phiên bản Web có worker mới.
- Hotfix migration 48: SQL Server compile-time không nhận cột vừa thêm khi câu `UPDATE`/constraint nằm cùng batch. Các DDL/DML phụ thuộc cột mới đã chuyển sang dynamic SQL và toàn script được bọc `TRY/CATCH` rollback; chạy lại an toàn sau lần lỗi.

## Booking calendar cho KOC, Brand và Admin

- Calendar dùng chung một query/service nhưng scope theo role: KOC chỉ lịch của mình, Brand chỉ campaign của mình, Admin xem toàn hệ thống; không dựa vào filter UI để bảo vệ dữ liệu.
- Ranh giới tháng được tính ở múi giờ Việt Nam rồi đổi sang UTC để query, sau đó event đổi lại giờ Việt Nam khi hiển thị; tránh lịch sát 00:00 rơi sai ngày/tháng.
- Desktop dùng lưới tháng thứ Hai–Chủ nhật; mobile tự chuyển sang danh sách theo ngày để card không bị quá hẹp. Có điều hướng tháng trước/sau và trạng thái Cancelled được giảm nhấn thị giác.
- Brand/KOC xem calendar ngay trên trang Booking hiện tại, vẫn giữ list/action bên dưới. Admin có trang `/Admin/Calendar` và CTA sang Operations, tách quan sát lịch khỏi thao tác can thiệp.
- Migration `49_add_admin_booking_calendar_menu.sql` chỉ thêm menu và role mapping Admin; không thay đổi dữ liệu booking hoặc history.

## Brand review submission SLA

- Rule `BRAND_REVIEW_SLA` phát hiện submission còn `Submitted` trong campaign `InProgress`, dùng `SubmittedAtUtc` của lần nộp mới nhất làm snapshot query; event `Submitted` append-only vẫn là bằng chứng đối chiếu.
- Policy mặc định High 48 giờ, Critical 72 giờ và chỉnh được trong cùng SLA policy UI. Scan từ chối chạy khi policy thiếu hoặc tạm dừng.
- Escalation dùng typed `SubmissionId` có FK, lifecycle Open/Acknowledged/Resolved, severity history và scan-run journal như các rule Operations khác.
- Brand chỉ nhận notification khi escalation được tạo lần đầu; scan lặp không spam. Khi Brand duyệt/yêu cầu sửa/từ chối, lần scan sau tự đóng hồ sơ nhưng không xóa timeline.
- Admin Operations có section riêng, kết quả lần quét gần nhất, badge severity, form tiếp nhận và timeline responsive.
- Migration `50_add_brand_review_sla_escalation.sql` mở rộng trusted CHECK cho escalation/scan/policy, seed policy + history, thêm filtered queue index và cập nhật trigger typed-source.
## Nhiều deliverable và deadline riêng cho chiến dịch

- Brand có thể tạo từ 1 đến 10 deliverable trong cùng chiến dịch; mỗi dòng có loại nội dung, số lượng và deadline riêng. Form tự đánh lại index khi thêm/xóa để Razor binding không làm rơi dữ liệu.
- Admin thấy toàn bộ danh sách đầu việc khi duyệt; KOC thấy đầy đủ cam kết và hạn nộp trước khi đăng ký. Layout chuyển thành card hai cột trên màn rộng và một cột trên mobile.
- Khi shortlist được chốt, luồng matching hiện hữu tạo đúng một submission cho từng cặp participation/deliverable; unique key `(ParticipationId, DeliverableId)` ngăn tạo trùng.
- Bộ lọc loại deliverable của KOC dùng `EXISTS` trên toàn bộ danh sách, không còn chỉ xét deliverable đầu tiên.
- Brand chỉ sửa khi campaign ở `Draft/NeedEdit`. Hệ thống từ chối xóa deliverable đã có submission để không phá FK và không làm mất hồ sơ công việc.
- `CampaignDeliverableHistories` lưu snapshot trước/sau cho `Created/Updated/Deleted`, actor và thời điểm. Bảng không FK tới deliverable vì phải giữ được lịch sử sau khi draft xóa dòng, nhưng có FK campaign/actor và trigger append-only.
- Migration `51_create_campaign_deliverable_history.sql` cần chạy trước khi dùng phiên bản Web này. Script idempotent ở mức tạo bảng và có transaction/rollback khi lỗi.

## Checklist yêu cầu chỉnh sửa chiến dịch

- Khi trả campaign về `NeedEdit`, Admin phải chọn ít nhất một nhóm trường và ghi chỉ dẫn cụ thể; service không tin dữ liệu form và chỉ chấp nhận tám mã nhóm canonical.
- Brand thấy checklist gần đầu trang sửa campaign theo card responsive, thay vì phải suy đoán từ một ghi chú chung. Ghi chú tổng quan vẫn được giữ để mô tả quyết định duyệt.
- Mỗi checklist gắn trực tiếp với đúng `CampaignStatusHistory` của lần review. Lần gửi lại/review sau tạo journal mới, không ghi đè phản hồi cũ.
- Database kiểm tra campaign, trạng thái `NeedEdit` và actor của checklist khớp status history. Unique key chặn trùng nhóm trong cùng lần review; trigger append-only chặn update/delete.
- Migration `52_create_campaign_review_feedback_checklist.sql` cần chạy sau migration 51.

## Targeting KOC có cấu trúc

- Brand cấu hình giới tính, khoảng follower, khoảng engagement, tối đa 20 tỉnh/thành và tối đa 20 nhóm nội dung; tỉnh và chuyên mục dùng FK tới danh mục hệ thống, không lưu text tự do.
- Service kiểm tra lại khoảng min/max, phần trăm 0–100, mã giới tính canonical, trạng thái tỉnh đang active và category có thật trước khi ghi.
- Admin thấy toàn bộ tiêu chí trong màn duyệt. KOC thấy tiêu chí và kết quả đối chiếu hồ sơ hiện tại; targeting là tín hiệu phù hợp, không tự động cấm đăng ký để tránh loại nhầm hồ sơ thiếu metric.
- Đối chiếu KOC dùng giới tính/tỉnh/follower hồ sơ, engagement cao nhất trong social account visible và category hồ sơ. UI nói rõ từng nhóm chưa khớp để KOC có thể cập nhật CV.
- `CampaignTargetingHistories` giữ JSON snapshot trước/sau, actor và timestamp cho mỗi lần Brand tạo/cập nhật targeting; trigger append-only bảo vệ hồ sơ quá khứ.
- Migration `53_create_campaign_targeting_history.sql` tạo targeting, hai bảng liên kết, index tra cứu, history constraints và trigger. Chạy sau migration 52.

## Ranking và shortlist theo targeting

- Admin matching xếp KOC theo độ phù hợp targeting giảm dần, sau đó mới dùng điểm hồ sơ, follower và thời điểm apply làm tie-break ổn định.
- Mỗi nhóm targeting đang được cấu hình có trọng số bằng nhau; điểm là tỷ lệ nhóm khớp trên tổng nhóm áp dụng, làm tròn hai chữ số. Không có targeting được xem là 100% để không phạt campaign legacy.
- Admin thấy điểm phù hợp và nhóm chưa khớp nhưng vẫn tự quyết định shortlist; thuật toán hỗ trợ quyết định, không thay quyền kiểm soát vận hành.
- Khi Admin gửi shortlist, `ShortlistItems` đóng băng `TargetingMatchScoreSnapshot` và JSON matched/unmatched. Brand xem snapshot này, không xem điểm tính lại từ hồ sơ KOC đã thay đổi sau đó.
- Snapshot mới có CHECK 0–100, JSON hợp lệ và trigger immutable. Dữ liệu shortlist cũ giữ null thay vì backfill suy đoán từ hồ sơ hiện tại.
- Migration `54_add_shortlist_targeting_match_snapshot.sql` cần chạy sau migration 53.

## Hồ sơ hợp tác Brand–KOC tổng hợp

- Ba portal có trang lịch sử hợp tác riêng nhưng dùng chung một service. Brand chỉ query participation thuộc campaign của mình; KOC chỉ query participation của mình; Admin được xem toàn hệ thống. Scope nằm trong SQL query, không dựa vào ẩn card trên UI.
- Timeline đọc trực tiếp từ các journal participation, booking, shipment, submission và dispute hiện hữu; không sao chép sự kiện sang bảng tổng hợp nên không tạo hai nguồn sự thật.
- Mỗi trang lấy tối đa 50 participation gần nhất và load history theo batch, không chạy một query cho từng card. Event được sắp mới nhất trước trong từng hồ sơ.
- Giao diện responsive hai cột desktop/một cột mobile, timeline thu gọn bằng `details` để danh sách vẫn dễ quét.
- Actor chỉ hiển thị role snapshot suy ra từ role hiện hành (`Admin/Brand/KOC`), không lộ email đăng nhập Admin sang portal Brand/KOC.
- Review code phát hiện và sửa đăng ký trùng `ScheduleReminderBackgroundService`; worker reminder hiện chỉ được đăng ký một lần.
- Migration `55_add_collaboration_history_menus.sql` chỉ thêm ba menu và role mapping, không tạo thêm bảng history.

## Phí dịch vụ 15% và invoice Brand

- Đối chiếu lại Word Admin/Brand xác nhận phí dịch vụ 15% tính theo campaign và Brand cần theo dõi `Pending/Paid/Overdue`, hóa đơn/biên nhận. Wallet KOC giữ nguyên ý nghĩa là thu nhập KOC; không trộn fee vào transaction ví.
- Admin chỉ phát hành invoice khi campaign `Completed`. Invoice chụp ngân sách, tỷ lệ 15%, số phí, ngày phát hành và hạn 15 ngày; các điều khoản bị trigger khóa bất biến sau khi phát hành.
- Lifecycle canonical: `Pending -> Paid/Void/Overdue`, `Overdue -> Paid/Void`; Paid/Void là trạng thái cuối. Paid bắt buộc mã tham chiếu, Void bắt buộc lý do.
- `CampaignBillingInvoiceHistories` append-only giữ mọi chuyển trạng thái, actor, note và thời điểm. Database có unique campaign/invoice number, CHECK amount/status/result/dates và index queue theo status + due date.
- Brand có trang read-only xem invoice, phí, deadline, trạng thái, mã thanh toán và timeline. Admin có dashboard số chưa phát hành/chờ/đã trả/doanh thu, cùng action phát hành, ghi nhận trả, quá hạn hoặc hủy.
- Không backfill invoice cho campaign cũ vì phát hành hóa đơn là quyết định tài chính, không được suy đoán. Admin chủ động phát hành từ queue campaign Completed.
- Migration `56_create_campaign_billing_invoice_history.sql` tạo ledger, history, triggers, indexes và menu Admin/Brand. Chạy sau migration 55.

## Báo cáo thật và biên nhận in/PDF

- Trang Brand Reports đã bỏ toàn bộ KPI/tên KOC hard-code. `IReportingService` lấy campaign, daily metric, submission thật; PageModel chỉ điều phối và không còn query `TopstarDbContext` trực tiếp.
- KPI Brand gồm campaign hoàn tất, impressions, views, engagement/ER, spend, revenue, ROI khi đủ spend và bảng hiệu quả KOC tối đa 20 dòng. Khi chưa có metric, UI nói rõ thay vì hiển thị số mẫu.
- Admin Reports tổng hợp completion rate, participation đang chạy, shipment issue, late cancellation, dispute active, phí đã thu/chờ và payout chờ xử lý.
- Brand mở biên nhận bằng invoice ID nhưng service luôn lọc thêm `BrandUserId`; GUID không cấp quyền đọc invoice của Brand khác. Trang biên nhận dùng layout in riêng và nút `In / Lưu PDF` của trình duyệt.
- Biên nhận đọc trực tiếp snapshot invoice bất biến, không tính lại từ campaign hiện tại. Vì vậy ngân sách/phí trên file luôn khớp ledger đã phát hành.
- Migration `57_add_admin_operational_report_menu.sql` chỉ thêm menu báo cáo Admin; Brand Reports dùng menu hiện hữu.

## Chính sách rút tiền KOC và snapshot tài chính

- Chính sách mặc định quy định số tiền tối thiểu, số yêu cầu tối đa mỗi KOC theo ngày Việt Nam (UTC+7), phí cố định và phí theo tỷ lệ. Server kiểm tra lại toàn bộ giới hạn; không tin giá trị từ giao diện.
- Khi KOC tạo yêu cầu, hệ thống giữ nguyên số tiền yêu cầu, phí và số thực nhận trong `PayoutRequests`. Admin xử lý theo số thực nhận đã chụp, nên thay đổi chính sách về sau không làm sai đối soát cũ.
- Dữ liệu payout legacy được backfill theo ý nghĩa đã biết trước đây: phí bằng 0 và số thực nhận bằng số tiền yêu cầu. Không suy đoán phí lịch sử.
- `PayoutPolicyHistories` lưu snapshot sau mỗi thay đổi, lý do và actor; trigger append-only chặn sửa/xóa. Snapshot tài chính payout cũng có trigger bất biến và CHECK tổng tiền.
- Admin có trang cấu hình riêng, bắt buộc nhập lý do và xem lịch sử. KOC nhận thông báo rõ số yêu cầu, phí và số thực nhận khi gửi/được chi trả.
- Migration `58_add_payout_policy_and_financial_snapshots.sql` cần chạy sau migration 57. Migration có transaction, backfill an toàn và có thể chạy lại.

## OTP xác nhận rút tiền

- KOC không còn tạo payout trực tiếp từ phiên đăng nhập. Hệ thống tạo challenge gắn với đúng KOC và số tiền, gửi OTP 6 số tới email đã xác minh rồi mới cho xác nhận.
- OTP hết hạn sau 5 phút, khóa sau 5 lần nhập sai và giới hạn tạo 3 challenge trong 10 phút cho mỗi KOC. Challenge chỉ dùng một lần; thao tác xác nhận kiểm tra ownership ở query database.
- Database chỉ lưu SHA-256 hash được salt bằng challenge ID, không lưu hoặc ghi log OTP dạng rõ. So sánh hash dùng constant-time để giảm rò rỉ timing.
- Sau khi OTP hợp lệ, service vẫn kiểm tra lại chính sách, tài khoản nhận tiền, số dư và giới hạn payout tại thời điểm tạo payout; OTP không thay thế validation tài chính.
- Giao diện dùng `autocomplete="one-time-code"`, bàn phím số trên mobile, thông báo email dạng che bớt và giải thích rõ thời hạn/số lần thử.
- Migration `59_create_payout_otp_challenges.sql` cần chạy sau migration 58; tạo FK, CHECK và index phục vụ rate-limit/expiry, không thay đổi payout lịch sử.

## Review nhận được và phản hồi công khai

- Trang Reviews của Brand/KOC được chia rõ “đánh giá bạn nhận được” và “đánh giá bạn đã gửi”, tránh trước đây chỉ thấy nội dung do chính mình gửi.
- Query review nhận được luôn scope bằng `RevieweeUserId` từ identity đăng nhập; ID trên form không cấp quyền phản hồi review của người khác.
- Người được đánh giá được phản hồi công khai đúng một lần, dài 2–2.000 ký tự. UI cảnh báo trước khi gửi vì phản hồi trở thành một phần lịch sử hợp tác.
- `ReviewReplies` có unique theo review, FK review/author, CHECK nội dung và trigger xác nhận author đúng là reviewee. Trigger append-only chặn update/delete, nên không thể sửa lời giải thích sau khi phát sinh tranh chấp.
- Reviewer nhận notification khi có phản hồi; audit chỉ lưu correlation ID, không sao chép nội dung phản hồi nhạy cảm sang payload.
- Migration `60_create_review_replies.sql` cần chạy sau migration 59. Appeal lên Admin chưa gộp vào reply; đây là lifecycle riêng cần trạng thái, căn cứ, quyết định và history ở bước tiếp theo.

## Khiếu nại review và quyết định Admin

- Người được đánh giá có thể tạo đúng một appeal cho review đang hiển thị, với căn cứ 10–2.000 ký tự. Ownership được kiểm tra ở service và trigger database bằng `RevieweeUserId`.
- Lifecycle canonical là `Open -> Accepted/Rejected`; hai trạng thái kết luận là terminal. Admin bắt buộc ghi kết luận 10–2.000 ký tự, actor và thời điểm xử lý.
- Chấp nhận appeal sẽ ẩn review và ghi `ReviewStatusHistory` trong cùng transaction với kết luận appeal. Từ chối giữ nguyên review; không có trạng thái nửa xử lý.
- Brand/KOC thấy trạng thái, căn cứ và kết luận ngay trên review nhận được. Admin có queue ưu tiên Open, đầy đủ review gốc, hai bên, căn cứ và timeline trước khi quyết định.
- `ReviewAppealHistories` append-only; review ID, appellant và reason của hồ sơ appeal là bất biến. Không xóa appeal kể cả sau khi đã xử lý.
- Migration `61_create_review_appeals.sql` cần chạy sau migration 60; tạo hai bảng, FK/CHECK/index và trigger bảo vệ identity/history.

## OTP kích hoạt tài khoản KOC

- Sau khi hoàn tất đăng ký, KOC được chuyển thẳng tới trang kích hoạt và có thể nhập OTP 6 số trong email. Link kích hoạt 24 giờ vẫn được giữ làm fallback và tương thích token cũ.
- OTP được sinh bằng CSPRNG, gắn salt với user ID, database chỉ lưu SHA-256 hash. So sánh dùng constant-time; chỉ chấp nhận sáu chữ số ASCII để tránh ký tự Unicode gây nhầm.
- OTP khóa sau 5 lần nhập sai. Query chỉ lấy challenge mới nhất còn hạn/chưa dùng của đúng email; thông báo lỗi không tiết lộ email có tồn tại hay không ở bước kiểm tra đầu vào.
- Khi kích hoạt thành công, `EmailConfirmed` được cập nhật và token lưu `UsedAtUtc`; các cột attempt/used giữ lại bằng chứng xác thực thay vì xóa token.
- Email hiển thị cả OTP dễ nhập trên mobile và link dự phòng. Trang kích hoạt dùng `autocomplete="one-time-code"` và vẫn cho quay lại đăng nhập rõ ràng.
- Migration `62_add_account_activation_otp.sql` cần chạy sau migration 61. Token legacy có `CodeHash = NULL` và tiếp tục dùng link; migration không backfill OTP suy đoán.

## Gửi lại OTP kích hoạt

- Trang kích hoạt có khu vực “Không nhận được mã?” để gửi lại mà không quay về đăng ký. Email mới vẫn chứa cả OTP và link fallback.
- Rate-limit tính cả mã ban đầu: tối đa 3 challenge trong cửa sổ 15 phút cho mỗi user. Count và insert chạy trong transaction `Serializable` để hai request đồng thời không vượt giới hạn.
- Endpoint luôn trả thông báo chung “nếu email hợp lệ...” dù email không tồn tại, đã kích hoạt, bị rate-limit hay SMTP lỗi; không tạo oracle dò tài khoản.
- OTP mới nhất còn hạn được dùng khi xác nhận bằng code. Token cũ vẫn được giữ để bảo toàn audit và link đã gửi; thành công cập nhật `UsedAtUtc`, không xóa lịch sử.
- Không có migration mới cho resend: hạng mục dùng index/cột của migration 62.

## Ảnh bằng chứng vận chuyển

- Brand và KOC có thể tải ảnh trực tiếp trên card shipment: Brand lưu ảnh đóng gói/giao hàng; KOC lưu ảnh nhận hàng hoặc sự cố. Gallery responsive mở ảnh gốc ở tab mới.
- Chỉ nhận JPG/JPEG, PNG hoặc WEBP tối đa 5 MB. Web kiểm tra extension và magic bytes; không nhận SVG để tránh active content, không nhận URL ngoài thư mục evidence do hệ thống quản lý.
- Evidence gắn shipment, uploader, role snapshot, loại, caption và timestamp. Mỗi bên tối đa 10 ảnh/vận đơn; count/insert chạy `Serializable` và trigger database kiểm tra lại giới hạn.
- Service scope shipment bằng Brand/KOC đăng nhập. Trigger database đối chiếu uploader với Brand của campaign hoặc KOC của participation, nên không thể giả role hay gắn ảnh vào vận đơn khác.
- `ShipmentEvidence` là journal append-only; không update/delete. Audit chỉ lưu evidence ID/type/có caption, không sao chép nội dung caption.
- Migration `63_create_shipment_evidence_journal.sql` cần chạy sau migration 62; tạo bảng, FK/CHECK/index và trigger ownership/append-only.

## Tracking checkpoint chi tiết

- Shipment có journal checkpoint riêng ngoài status lifecycle. Brand có thể ghi `PickedUp`, `AtHub`, `OutForDelivery`, `DeliveryAttempt`, `Exception` hoặc `Other`, kèm thời điểm Việt Nam, địa điểm và mô tả.
- Chỉ cho ghi thủ công khi shipment `InTransit/Issue`; service scope Brand sở hữu campaign. Trigger database kiểm tra lại Brand owner, trạng thái và thời điểm không trước lúc tạo/không quá 10 phút tương lai.
- KOC thấy hành trình mở sẵn, mới nhất trước; Brand có cùng timeline và form thêm checkpoint thu gọn. Mỗi checkpoint gửi notification cho đúng KOC.
- Thời điểm nhập theo UTC+7 rồi chuẩn hóa UTC trước khi lưu. `OccurredAtUtc` là thời điểm sự kiện carrier, `CreatedAtUtc` là thời điểm hệ thống nhận bản ghi; giữ cả hai để audit cập nhật trễ.
- `ShipmentTrackingEvents` append-only và có source snapshot `ManualBrand/CarrierWebhook`; cấu trúc đã sẵn cho adapter carrier sau này nhưng hiện chưa giả lập webhook/API.
- Hotfix evidence: nếu file ảnh đã lưu nhưng service từ chối DB do quyền/giới hạn, Web xóa đúng file mới trong thư mục evidence đã khóa path; tránh file orphan.
- Migration `64_create_shipment_tracking_event_journal.sql` cần chạy sau migration 63.

## Admin giám sát evidence và tracking shipment

- Card sự cố trên Admin Operations đã hiển thị toàn bộ ảnh evidence của Brand/KOC, checkpoint carrier, mã vận đơn và status timeline trước vùng quyết định.
- Admin chỉ đọc evidence/tracking; không có action sửa/xóa hoặc giả checkpoint của hai bên. Quyết định tiếp tục giao/hoàn hàng vẫn đi qua lifecycle shipment và status history hiện hữu.
- Layout sự cố đổi sang card toàn chiều rộng: desktop chia bằng chứng 7 cột và quyết định 5 cột; mobile xếp dọc để ảnh/căn cứ luôn xuất hiện trước action can thiệp.
- Query chỉ load evidence/tracking cho shipment đang `Issue`, theo batch và group trong bộ nhớ; không tạo N+1 query trên từng card.
- Không có migration mới cho hạng mục này; Admin dùng hai journal của migration 63 và 64.

## Ledger vi phạm và điểm KOC hiệu lực

- Admin có trang riêng để ghi nhận vi phạm Brand/KOC theo email chính xác, category, severity `Low/Medium/High`, penalty 0–100 và căn cứ bắt buộc. Người dùng nhận notification có deep-link về profile.
- Vi phạm giữ facts gốc bất biến; lifecycle `Active -> Revoked`, thu hồi bắt buộc lý do và không xóa record. `UserViolationHistories` append-only hiển thị timeline trên card Admin.
- Không sửa trực tiếp `KocProfile.Score`. Matching Admin và shortlist Brand tính `max(0, base score - tổng penalty Active)`; khi violation được thu hồi, penalty tự hết ảnh hưởng nhưng lịch sử vẫn còn.
- Penalty chỉ ảnh hưởng score KOC; violation Brand vẫn là ledger vận hành nhưng không bị ép vào công thức matching không liên quan.
- Database có CHECK severity/penalty/status/resolution, FK actor, index theo user/status và trigger bảo vệ facts/history.
- Migration `65_create_user_violation_ledger.sql` cần chạy sau migration 64; đồng thời thêm menu Admin `Vi phạm tài khoản`.

## Danh bạ Brand và bảng xếp hạng KOC thật

- Rà soát phát hiện trang `/Admin/Brands` cũ vẫn hiển thị KPI, tên Brand và doanh thu mẫu hard-code. Trang này đã được thay hoàn toàn bằng dữ liệu từ `Users`, `BrandProfiles`, `KocProfiles`, campaign, participation và ledger vi phạm.
- Admin tra cứu đồng thời Brand/KOC theo tên hoặc email, lọc trạng thái hồ sơ và sắp xếp mới nhất/theo tên/điểm KOC. Query lọc, tổng hợp và giới hạn tối đa 150 dòng mỗi nhóm tại database; không tải toàn bộ rồi lọc trong Web.
- Card Brand hiển thị trạng thái tài khoản/hồ sơ, ngành, số campaign hoàn tất trên tổng campaign và số vi phạm Active. Không còn KPI doanh thu suy đoán hoặc tên đối tác giả.
- Bảng KOC hiển thị điểm gốc, penalty Active, điểm hiệu lực, followers và số hợp tác Completed. Xếp hạng canonical dùng điểm hiệu lực giảm dần, sau đó số hợp tác hoàn tất, followers và profile ID để kết quả ổn định.
- Công thức vẫn là `max(0, BaseScore - ActivePenalty)`. Ranking chỉ đọc ledger, không ghi đè `KocProfiles.Score`; thu hồi vi phạm tự làm điểm hiệu lực phục hồi trong lần đọc kế tiếp.
- Từ danh bạ, Admin mở được hồ sơ chi tiết và ledger vi phạm. Trang chỉ dành cho role Admin; trạng thái/filter/sort từ query string đều được chuẩn hóa phía server.
- Không có migration mới cho hạng mục này. Trang phụ thuộc `UserViolations` của migration 65; cần chạy `65_create_user_violation_ledger.sql` trước khi mở danh bạ trên database chưa cập nhật.

## Catalog Học viện KOC và lịch sử xuất bản

- Rà soát phát hiện `/Admin/Academy` cũ chỉ là dashboard mẫu: 6 khóa học, số học viên, tỷ lệ hoàn thành và rating đều hard-code; các nút không nối backend. Toàn bộ KPI giả đã được loại bỏ.
- `AcademyCourses` là nguồn catalog duy nhất cho Admin và trang công khai `/hoc-vien-koc`. Admin quản lý tên, cấp độ, số tuần, mô tả, syllabus theo dòng, thứ tự và lifecycle `Draft/Published/Archived`; chỉ `Published` được public query.
- Không hiển thị số học viên, completion rate hoặc rating khi hệ thống chưa có enrollment/progress thật. Empty state công khai nói catalog đang cập nhật thay vì dựng số liệu mẫu.
- Trang public cũng đã bỏ các claim không có nguồn như 5.000 học viên, 95% hoàn thành, rating 4.9, case study tăng followers/thu nhập và số lượng tài nguyên giả. Chỉ số duy nhất hiển thị là số khóa Published được query trực tiếp.
- Mỗi lần tạo, sửa nội dung hoặc đổi trạng thái tạo một `AcademyCourseHistories` có actor, action, trạng thái trước/sau, lý do và thời điểm. History append-only; course không được xóa, chỉ chuyển Archived.
- Update nội dung và history dùng chung một `SaveChanges`, bảo đảm atomic trong transaction mặc định của EF. Audit log chỉ lưu metadata/lý do cần thiết, không nhân đôi toàn bộ syllabus.
- Database có CHECK độ dài, status, duration/order; FK actor, index public query `(Status, OrderNo, Title)` và index timeline. Luồng `Archived -> Published` bị chặn, phải đưa về Draft để review trước.
- Migration `66_create_academy_course_catalog_history.sql` cần chạy sau migration 65; script đồng thời bảo đảm menu Admin Học viện được gán cho role Admin.

## Hợp nhất package Brand, campaign và matching

- Rà soát phát hiện `/goi-koc` quảng cáo Starter/Growth/Premium/Enterprise và giá hard-code nhưng matching thật dùng `CampaignPackagePolicies`; form campaign còn cho Brand nhập `PackageCode` tự do. Ba nguồn dữ liệu có thể mâu thuẫn và áp sai quyền chọn KOC.
- Trang public package hiện chỉ query policy `IsActive`, theo đúng khoảng ngân sách, quyền Brand chọn và quota đang được matching sử dụng. Đã bỏ giá/gói/hotline giả và giải thích rõ package là cơ chế matching, không phải hóa đơn trọn gói.
- Khi tạo hoặc sửa campaign, server tự resolve đúng một package Active chứa `BudgetAmount` và ghi mã canonical; giá trị `PackageCode` từ form/client bị bỏ qua. Nếu policy bị cấu hình hở, hệ thống từ chối lưu campaign thay vì âm thầm gán `STANDARD`.
- Admin sửa policy bắt buộc lý do 5–1.000 ký tự. `CampaignPackagePolicyHistories` lưu snapshot sau thay đổi, actor và thời điểm; giao diện hiển thị timeline ngay dưới từng policy.
- History append-only; policy không được xóa, `Code` và `CreatedAtUtc` bất biến. Tắt policy dùng `IsActive` nhưng service vẫn kiểm tra toàn bộ coverage trước khi lưu để không tạo khoảng trống/chồng lấn.
- Không backfill actor cho snapshot policy cũ vì không được suy đoán người thực hiện. History bắt đầu từ lần thay đổi đầu tiên sau migration; audit cũ vẫn giữ nguyên.
- Migration `67_create_campaign_package_policy_history.sql` cần chạy sau migration 66.

## Portal KOC dùng catalog Học viện thật

- Rà soát sau module Academy phát hiện `/Koc/Academy` cũ vẫn hiển thị trình độ Beginner, 8/18 bài, 120 điểm và hai tiến độ 75%/40% hard-code.
- Portal KOC hiện query cùng catalog `AcademyCourses` Published với public page; hiển thị tên, cấp độ, thời lượng, mô tả và syllabus theo thứ tự Admin cấu hình.
- Đã loại bỏ toàn bộ tiến độ, điểm và chứng chỉ giả. UI nói rõ hệ thống chưa có lesson/enrollment/assessment/certificate nên chưa theo dõi phần trăm; đây là giới hạn chức năng minh bạch, không phải dữ liệu rỗng bị che giấu.
- KOC vẫn đi qua `KocGuardedPageModel`, chỉ hồ sơ hoàn chỉnh/đã duyệt mới dùng portal; catalog public vẫn xem được ở trang tổng quan.
- Không có migration mới; hạng mục dùng bảng của migration 66.

## Admin chọn KOC social sync và chống job trùng

- Admin Dashboard trước đây bắt nhập `KOC Profile ID` dạng GUID để sync một KOC. Đã thay bằng select có tên, email, các nền tảng Active và trạng thái job gần nhất; chỉ KOC Approved có OAuth connection Active được liệt kê.
- Sync một KOC kiểm tra lại profile/connection ở service; ID từ form không cấp quyền hoặc bỏ qua điều kiện. Sync toàn bộ chỉ enqueue KOC đủ điều kiện và bỏ qua KOC đã có job Pending/Running.
- Brand sync theo campaign áp dụng cùng quy tắc: chỉ KOC trong participation có connection Active và chưa có job mở mới được enqueue; batch rỗng trả thông báo rõ thay vì tạo job chắc chắn thất bại.
- Service chặn job trùng trước insert để phản hồi dễ hiểu; database có index theo KOC/status và trigger dùng khóa `UPDLOCK,HOLDLOCK` làm hàng rào cuối cho hai request đồng thời. Khi va chạm constraint, Web nhận kết quả nghiệp vụ thay vì lỗi 500.
- Migration xử lý dữ liệu trùng cũ theo hướng bảo toàn lịch sử: giữ job mở sớm nhất, chuyển các job mở dư sang Failed với lý do nâng cấp và timestamp; không xóa record.
- Danh sách candidate load theo batch: profile/connection một query, job history một query rồi group trong memory; không query từng KOC.
- Migration `68_prevent_duplicate_open_social_sync_jobs.sql` cần chạy sau migration 67.

## Theo dõi job Social Sync và tính đúng metric

- Admin Dashboard bổ sung bảng 30 job mới nhất: KOC, scope, role yêu cầu, trạng thái, thời điểm bắt đầu/kết thúc và lỗi. Admin không còn phải suy đoán sau khi bấm enqueue hoặc dò database.
- Danh sách job là read-only; retry vẫn đi qua select KOC và quy tắc một job mở, không sửa/xóa job cũ. `SocialSyncJobs` tiếp tục là lịch sử vận hành gốc.
- Sửa ba nullable warning: phép chia YouTube dùng denominator đã kiểm tra; URL profile chỉ gán khi connection có URL thật; không còn gán nullable vào trường channel.
- Bỏ công thức TikTok `followers / video_count` từng bị gắn nhãn `AvgViews`. Endpoint user info hiện không trả tổng lượt xem, nên `AvgViews` giữ null thay vì tạo metric sai; follower/video count thật vẫn được lưu.
- Không có migration mới cho hạng mục hiển thị/metric này; cơ chế chống job trùng vẫn dùng migration 68.

## Khóa/mở khóa tài khoản Brand và KOC

- Filter `Locked` đã tồn tại nhưng Admin trước đây không có action quản trị. Danh bạ Brand/KOC hiện cho khóa tài khoản `Active + Approved` và mở lại đúng cặp `Locked + Locked`; mọi trạng thái lệch bị service từ chối.
- Khóa/mở cập nhật `Users.Status` và profile status trong cùng transaction. Khi khóa, toàn bộ `AuthSessions` chưa revoked được đóng với cùng lý do; profile guard cũng ngăn tài khoản đang có cookie cũ tiếp tục dùng chức năng.
- Lý do bắt buộc 10–1.000 ký tự. Người dùng nhận notification; audit lưu actor/correlation; `UserAccountStatusHistories` lưu trạng thái user và profile trước/sau, loại profile, actor, lý do và thời điểm.
- Ledger account append-only và được hiển thị ngay trên card Admin. Không xóa hay sửa lịch sử; không suy đoán/backfill actor cho các lần khóa legacy chưa có ledger.
- Service lấy transaction-owned application lock theo đúng `UserId` trước khi đọc trạng thái. Hai Admin thao tác cùng tài khoản được tuần tự hóa; request đến sau đọc trạng thái mới và bị rule từ chối thay vì tạo hai ledger giống nhau. Tài khoản khác vẫn xử lý song song.
- Timeline trên danh bạ hiển thị thêm email actor từ FK lịch sử để support xác định rõ ai đã khóa/mở; không lấy actor từ audit bằng suy luận thời gian.
- Trigger profile history hiện hữu của migration 30 tiếp tục chụp thay đổi profile song song với ledger account chuyên biệt; đây là hai góc nhìn khác nhau, không phải hai nguồn quyết định trạng thái.
- Migration `69_create_user_account_status_history.sql` cần chạy sau migration 68.

## Notification template có version history

- Admin có trang `/Admin/Settings/NotificationTemplates` quản lý template theo khóa bất biến `Notification Type + AudienceRole`. Có thể bật/tắt nhưng không xóa hoặc đổi identity.
- Placeholder cố ý giới hạn ở `{Title}` và `{Body}` để tái sử dụng nội dung động đã được từng nghiệp vụ xác thực; placeholder lạ hoặc dấu ngoặc chưa đóng bị từ chối. Output được trim và clamp đúng `Notifications.Title(200)` / `Body(1000)`.
- Việc áp dụng nằm tập trung trong `TopstarDbContext` ngay trước khi lưu mọi notification mới. Vì vậy matching, dispute, shipment, review, wallet, violation, account status và job tương lai cùng tuân một cơ chế; không có template Active thì giữ nguyên title/body gốc.
- Mỗi lần tạo/sửa/bật/tắt append `NotificationTemplateHistories` với snapshot title/body/status, lý do, actor và thời điểm. History append-only; template không được delete để notification cũ luôn điều tra được theo version đã tồn tại.
- Template chỉ tác động notification tạo sau khi version được lưu, không rewrite notification cũ vốn đã immutable. Đây là chủ ý bảo toàn bằng chứng giao tiếp.
- Migration `70_create_notification_template_version_history.sql` cần chạy sau migration 69; script tạo tables, constraints/index/triggers và menu Admin.

## Học viện KOC có bài học, tiến độ và chứng nhận thật

- Admin quản lý danh sách bài học ngay trong từng khóa: tiêu đề, URL HTTP/HTTPS, thứ tự và trạng thái xuất bản. Bài học không bị xóa; tắt `IsActive` để ngừng hiển thị mà vẫn giữ được completion cũ.
- Mỗi lần tạo/sửa/tắt bài học append một `AcademyLessonHistories` chứa snapshot nội dung, lý do, actor và thời điểm. Trigger database chặn update/delete history.
- KOC chỉ đăng ký khóa `Published` có ít nhất một bài Active. Unique `(CourseId, KocUserId)` và transaction `Serializable` chống đăng ký trùng.
- Khi đăng ký, `AcademyEnrollmentLessons` chụp chính xác danh sách bài bắt buộc, tiêu đề và thứ tự. Admin thêm/tắt/đổi tên bài về sau không làm thay đổi mẫu số tiến độ hoặc điều kiện chứng nhận của enrollment cũ.
- Enrollment đồng thời chụp tên khóa học và tên hiển thị KOC; trang xác minh chứng nhận không đổi theo lần sửa profile/catalog về sau.
- Tiến độ được tính từ `AcademyLessonCompletions` thật trên tổng bài Active, không nhập tay và không dùng số mẫu. Completion là journal append-only, unique theo enrollment/bài học; trigger kiểm tra bài thuộc đúng khóa đã đăng ký.
- Khi hoàn thành toàn bộ bài Active, hệ thống chụp `CompletedAtUtc` và mã chứng nhận duy nhất. Course/KOC/thời điểm đăng ký bất biến; mã chứng nhận đã phát hành không thể sửa hoặc thu hồi ngầm.
- Trang public `/xac-minh-chung-nhan?code=...` xác minh trực tiếp mã đã phát hành, hiển thị tên KOC, khóa học, số bài snapshot và thời điểm hoàn tất; không lộ email, UserId hoặc dữ liệu hồ sơ nhạy cảm. Mã mới dùng 80 bit từ enrollment ID để khó dò hàng loạt.
- Database chỉ cho ghi completion nếu lesson nằm trong snapshot và chỉ cho phát hành certificate khi toàn bộ snapshot đã hoàn thành; không thể bỏ qua rule bằng SQL trực tiếp.
- Giao diện KOC ưu tiên trạng thái chưa đăng ký/đang học/hoàn tất, progress bar, nút mở nội dung và action hoàn thành ngay trên từng bài; mobile xếp dọc để thao tác rõ ràng.
- Admin Academy có bảng 100 enrollment gần nhất với KOC/email, khóa học snapshot, tiến độ thật, thời điểm đăng ký và link xác minh chứng nhận. Empty state không dựng completion rate; dữ liệu email chỉ nằm trong portal Admin.
- Query Admin giới hạn tại database và tính số bài/completion bằng correlated aggregate trong một truy vấn; không chạy N+1 theo từng học viên.
- Migration `71_create_academy_enrollment_progress_history.sql` cần chạy sau migration 70.
- Migration `72_harden_academy_enrollment_certificate_snapshots.sql` là guard nâng cấp cho database có thể đã chạy bản 71 trước khi bổ sung snapshot. Script không suy đoán dữ liệu enrollment cũ: nếu phát hiện enrollment tồn tại nhưng thiếu snapshot, nó rollback và yêu cầu đối soát thủ công.
- Hotfix migration 72: CHECK constraint tham chiếu hai cột snapshot mới được tạo bằng dynamic SQL để tránh lỗi compile-time `Invalid column name` của SQL Server trong cùng batch. Script vẫn nằm trong transaction và có thể chạy lại sau lần lỗi đã rollback.

## Admin tra cứu Audit Logs tập trung

- Tài liệu Admin yêu cầu audit trails nhưng trước đây `AuditLogs` chỉ được các workflow ghi vào database, chưa có service, màn hình tra cứu hoặc menu vận hành.
- Trang `/Admin/AuditLogs` lọc theo email actor/Entity ID, entity, action và khoảng ngày Việt Nam; trả tối đa 200 bản ghi mới nhất và hiển thị tổng số kết quả phù hợp.
- Danh sách chỉ hiển thị metadata actor, action, entity, ID và timestamp; không đổ `PayloadJson` ra UI để tránh mở rộng bề mặt lộ lý do dispute, dữ liệu tài chính hoặc nội dung vận hành nhạy cảm.
- Filter/limit chạy tại database; actor email được resolve trong query, không tạo N+1. Khoảng ngày nhập UTC+7 được chuẩn hóa UTC và cận trên bao trọn ngày đã chọn.
- Audit reader chỉ có API đọc. Migration thêm hai index phục vụ timeline/filter và trigger append-only chặn update/delete; dữ liệu cũ giữ nguyên, không backfill hoặc rewrite.
- Migration `73_add_admin_audit_log_reader.sql` cần chạy sau migration 72; script đồng thời thêm menu và role mapping Admin.

## Snapshot danh tính actor trong Audit Logs

- Audit reader ban đầu phải join `Users` hiện tại để lấy email actor; khi email hoặc role thay đổi, cách hiển thị này có thể làm sai ngữ cảnh lịch sử.
- Mọi `AuditLogEntity` mới được `TopstarDbContext` tự chụp `ActorEmailSnapshot` và danh sách role canonical ngay trước `SaveChanges`; các workflow không cần tự nhớ gán snapshot nên rule áp dụng tập trung.
- Actor null được chụp `System/SYSTEM`. Actor có ID nhưng không resolve được được ghi `Unknown/UNKNOWN`, không âm thầm gán danh tính khác.
- Nhật ký legacy giữ snapshot null và UI/service fallback về user hiện tại với nhãn role `LEGACY`; không backfill email/role quá khứ bằng dữ liệu hiện tại.
- Migration `74_add_audit_actor_identity_snapshots.sql` cần chạy sau migration 73; chỉ thêm hai cột nullable và duy trì trigger append-only.
- Audit UI hiển thị role snapshot cạnh actor. Desktop dùng bảng; mobile chuyển thành card dọc với action/time ở đầu, entity/ID ở giữa và actor/role ở cuối để không phải kéo ngang.
- Rà encoding xác nhận source UTF-8 đúng; chuỗi mojibake từng thấy là do PowerShell đọc bằng code page mặc định, không rewrite file hoặc tạo migration không cần thiết.
- Filter Entity/Action không còn là text tự do dễ gõ sai. Hai danh mục lấy `DISTINCT` trực tiếp từ AuditLogs, sắp xếp và giới hạn 100 giá trị tại database; workflow mới tự xuất hiện mà không cần hard-code UI.
- Bỏ giới hạn chỉ xem 200 log mới nhất: reader phân trang server-side 50 dòng, chuẩn hóa page tối thiểu 1/tối đa trang cuối và giữ toàn bộ filter khi chuyển trước/sau. Admin truy cập được lịch sử cũ mà không tải toàn bộ ledger.
- Admin có thể xuất CSV theo đúng filter hiện tại, tối đa 5.000 log mới nhất. File chỉ chứa metadata, có UTF-8 BOM và mọi cell text được quote/escape; giá trị bắt đầu `= + - @` được prefix apostrophe để chặn spreadsheet formula injection.
- CSV guard kiểm tra ký tự nguy hiểm đầu tiên sau whitespace/tab và có unit test cho formula, quote, newline và null.
- Mỗi lần export tự append audit `AuditLog/Export` với Admin actor, filter đã chuẩn hóa, số dòng và giới hạn 5.000. Không lưu nội dung CSV; actor email/role vẫn được snapshot tập trung trước khi ghi.

## Menu Admin hai cấp và sửa tiếng Việt

- Menu Admin trước đây có 18 nghiệp vụ ở root nên vỡ thành nhiều hàng ngang; database chỉ có hai parent thực tế. Navigation mới giữ đúng hai cấp với bảy root: Tổng quan, Tài khoản, Chiến dịch, Vận hành, Tài chính, Phân tích và Cài đặt hệ thống.
- Desktop hiển thị một hàng nhóm và dropdown cấp 2. Mobile có nút `Menu quản trị`, mỗi nhóm mở danh sách con dạng block, không ép người dùng kéo ngang.
- Hai menu legacy `/Admin/Users` và `/Admin/Permissions` bị ẩn vì repo chưa có Razor Page tương ứng; không tiếp tục hiển thị liên kết chết.
- Migration sửa trực tiếp title mojibake `Duyệt hồ sơ`, `Cấu hình package KOC` và chuẩn hóa title nhóm. Phải chạy `sqlcmd -f 65001` hoặc mở file bằng UTF-8 trong SSMS; chạy ANSI có thể ghi mojibake dù chuỗi có tiền tố `N`.
- Migration `75_group_admin_navigation_two_levels_fix_vietnamese.sql` idempotent, đã chạy và xác nhận trên TopstarDb local: toàn bộ menu visible có tối đa hai cấp và title tiếng Việt đúng.
