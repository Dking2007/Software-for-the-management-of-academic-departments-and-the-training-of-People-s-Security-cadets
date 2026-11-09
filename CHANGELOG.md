# Lịch sử cập nhật

Hệ thống quản lý Khoa/Lớp đào tạo, Học viện An ninh nhân dân. Dữ liệu và tài khoản đều giả lập, phục vụ học tập. Mới nhất ở trên.

## 10/11/2026: Hoàn thiện theo góp ý và dọn mã nguồn

**Trang lớp**
- Tab Lịch học có bốn cách xem: lưới tuần, theo ngày, tháng, danh sách; có tìm tự do, lọc chuyên đề, xuất Excel.
- Sổ đầu bài có bốn cách xem: theo tuần như trang sổ giấy, lịch tháng tô màu theo xếp loại, theo chuyên đề, danh sách. Các ô số liệu bấm được để lọc nhanh; ghi sổ qua hộp thoại riêng.

**Hướng dẫn nhập môn**
- Chỉ màn chào mừng có nền mờ nhám, các bước sau làm tối nền và khoét sáng vùng giới thiệu.
- Bấm ra ngoài khung để đóng.

**Sửa lỗi**
- Tin mẫu của Bản tin không còn mang giờ tương lai khi khởi tạo trước 8 giờ sáng.
- Thông báo trống ở tab Điểm danh hiện đúng theo vai trò giảng viên.

**Dọn mã nguồn**
- Bỏ font không dùng, luật CSS mồ côi, hàm không còn nơi gọi.
- Chuyển hướng dẫn vào `docs/`, báo cáo các giai đoạn cũ vào `docs/lich-su/`.

## 09/11/2026: Vòng góp ý 11 điểm

- **Tìm kiếm tự do** (`TimKiemMo`) ở Ctrl+K, danh sách, lịch, sơ đồ: không cần dấu, không cần đúng thứ tự, chấp nhận mọi dấu phân cách, gõ sai một chữ vẫn ra.
- **Lịch đào tạo** có bốn cách xem: theo lớp (ma trận lớp × ngày, nhóm theo khoá), lưới tuần, theo ngày, tháng; thêm thẻ số liệu và bộ lọc.
- **Excel**
  - Xuất lịch theo khoảng ngày bất kỳ, nhập lịch có xem trước và chặn trùng.
  - Báo cáo điểm danh đi làm bốn trang.
  - Báo cáo giảng dạy cho giảng viên, kết quả học tập cho học viên.
  - Bổ sung nút xuất cho khoa, tổ, phòng, chuyên đề, phân công, xét thi, chuyên cần, sổ đầu bài.
- **Sơ đồ tổ chức**: cột thành viên kèm thẻ thông tin khi rê chuột, nút đóng mở từng cột.
- **Giao diện**: sửa các khối bị dính, nút về đầu trang, hướng dẫn nhập môn theo vai trò, favicon theo logo Học viện.

## 07/11/2026: Chuẩn hoá khung giao diện

- Đầu trang thống nhất (`DauTrang`) có biểu tượng và màu nhấn theo phân hệ.
- Trạng thái thu gọn menu được nhớ trên trình duyệt, tự thu ở màn hình hẹp.
- Trang chủ theo vai trò, có ô bắt đầu nhanh.

## 05–06/11/2026: Tích hợp toàn bộ hệ thống

- Chuyển dữ liệu nghiệp vụ sang EF Core SQLite.
- Phân quyền Admin, ba quản lý, giảng viên, học viên, người xem theo mặt cắt dữ liệu; nhật ký chỉ thêm.
- Lịch, phòng, chặn trùng; điểm danh, điểm số, xếp loại; sổ đầu bài; chuyên cần; điểm danh đi làm.
- Chương trình và phân công; xét tư cách thi, lịch thi.
- Nhập, xuất Excel; sơ đồ tổ chức; bản tin; văn bản, thông báo; trường dữ liệu mở rộng.
- Giao diện ba ngôn ngữ Việt, Anh, Lào; bảng cài đặt cá nhân.

## 03–05/11/2026: Nền tảng

- Khởi tạo dự án Blazor Web App .NET 8 với Radzen, đăng nhập minh hoạ, danh sách khoa, tổ, cán bộ, lớp, học viên.
- Cập nhật giao diện và cấu trúc quản lý Khoa/Lớp theo biên bản nhóm.

Tài liệu của các giai đoạn trước nằm trong `docs/lich-su/`.
