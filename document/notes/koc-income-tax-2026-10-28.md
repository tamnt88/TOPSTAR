# Thuế KOC khi cộng ví

Triển khai 165, 166, 167 trước binaries. Không chạy SQL tự động trên database sử dụng. Chính sách mặc định chưa bật; không tính lại thuế ví cũ.

Admin → Tài chính → Thuế thu nhập KOC: lưu phiên bản có ngày hiệu lực, thuế suất %, ngưỡng từng khoản chi trả. Thuế suất và ngưỡng cần kế toán xác nhận; không mặc định gán 10% hay ngưỡng pháp lý trong code.
Admin → Trung tâm tài khoản KOC → Thuế & CCCD: kiểm tra tài liệu, phân loại cá nhân khấu trừ / kinh doanh xử lý riêng / không khấu trừ theo căn cứ. Nhập căn cứ để audit. KOC thay đổi giấy tờ sẽ hủy trạng thái xác minh cho lần chi trả mới.

Các đường tạo WalletTransactions Earning/CampaignEarning cùng đi qua hook trước SaveChanges. Lưu gross, thuế, net, phiên bản chính sách. Đạt ngưỡng khấu trừ trên toàn gross, làm tròn nguyên đồng AwayFromZero. Ví Amount là net. Ledger ký quỹ vẫn ghi gross để đối soát ngân sách và phần thuế giữ lại. Không tính lại khi rút tiền hoặc đổi trạng thái khả dụng.

Thiếu hồ sơ/kiểm tra: khoản thu nhập OnHold, TaxState Awaiting, không khả dụng để rút. Admin xác minh rồi bấm Đối soát lại; giữ phiên bản chính sách của khoản tạo ban đầu. Chuyển trạng thái ví trực tiếp cũng không vượt qua bước kiểm tra thuế.

Đối soát phân biệt đã khấu trừ và chứng từ đã nộp. Nút Đã nộp chỉ ghi nhận chứng từ kế toán, không gọi cơ quan thuế hay tự chuyển tiền. Chưa xuất chứng từ khấu trừ điện tử hoặc tích hợp khai thuế. Ngưỡng áp dụng trên từng khoản nghiệm thu/thu nhập, không trên lệnh rút và không hỗ trợ chia một khoản chi trả để thay ngưỡng. Kế toán cần kiểm tra các đợt chi trả có cùng nghĩa vụ theo hợp đồng.

SQL trigger không cho sửa gross/tax/net/policy đã chốt. Hoàn/điều chỉnh sau chốt chưa tự động hỗ trợ; cần quy trình kế toán riêng. Phiên bản chính sách bất biến, audit việc phân loại/tính thuế/ghi nhận nộp thuế không chứa CCCD rõ.
