# Lịch sử cập nhật

Hệ thống quản lý Khoa/Lớp đào tạo, Học viện An ninh nhân dân. Dữ liệu và tài khoản đều giả lập, phục vụ học tập. Mới nhất ở trên.

## 12/11/2026 (chiều): Nút lá cờ hướng dẫn theo từng trang

- Nút lá cờ luôn hướng dẫn **đúng trang đang mở**, không còn chuyển sang giới thiệu chung. Thêm hướng dẫn riêng cho Trang chủ, Văn bản thông báo, Hệ đào tạo, Chi tiết lớp; trang khác tự dựng hướng dẫn từ bố cục của chính trang (đầu trang và mô tả, thao tác chính, tab, tìm và lọc, vùng dữ liệu).
- Giới thiệu chung chỉ dành cho người mới (thẻ mời lần đầu) hoặc khi chủ động bấm "Xem giới thiệu chung" ở trang Trợ giúp; bỏ nút "Xem giới thiệu chung" ở cuối hướng dẫn trang.
- Chấm gợi ý trên nút lá cờ chỉ hiện ở trang có hướng dẫn riêng mà tài khoản chưa xem, tắt sau khi xem.

## 12/11/2026: Thẻ tài khoản trên trang chủ, sửa trang giới thiệu

- Trang chủ: thẻ tài khoản kiểu thẻ ngành (chip, quân hiệu, chữ cái đầu, vai trò, phạm vi, giờ bắt đầu phiên); vào trang thì thẻ lật vào, ánh kim lướt qua rồi lơ lửng nhẹ. Tắt khi giảm chuyển động.
- Trang giới thiệu: nút "Xem bảng phân quyền" cuộn xuống đúng khối thay vì tải lại trang (liên kết `#...` bị thẻ `<base href="/">` hiểu thành trang chủ).
- Trang giới thiệu chỉ giới thiệu các vai trò, không còn liên kết điền sẵn tài khoản mẫu vào trang đăng nhập.

## 11–13/11/2026: Giao diện "Sổ bộ", trang giới thiệu, sửa lỗi theo góp ý

**Sửa lỗi**
- Sửa văn bản kèm ảnh không còn làm đơ trang: biểu mẫu tách thành component riêng (`Components/BieuMauThongBao.razor`); lưu xong hộp thoại tự đóng và báo đã lưu, đóng được bằng × hoặc Esc. Nhập Excel bắt mọi lỗi đọc tệp; thanh "Tải lại trang" khi mất kết nối nổi trên hộp thoại.
- Giao diện tối: chữ trên nút, nhãn tô màu đổi theo token `--chu-tren-mau`; hộp gợi ý biểu đồ đọc được ở cả hai chế độ.
- Chữ tràn khỏi thẻ (Hoạt động gần đây, nhãn gãy dòng, danh sách văn bản ở màn vừa); hoạt động hiển thị tiêu đề thay cho mã GUID.
- Lịch theo ngày và Điểm danh đi làm bị vỡ cột do trùng tên lớp CSS; bảng công làm lại: các ngày rộng bằng nhau, cột tên cố định khi cuộn ngang. Lịch theo ngày ghi rõ "Đang học", "Bắt đầu lúc…".

**Tính năng**
- Bấm ảnh văn bản/bản tin để xem lớn (`wwwroot/xem-anh.js`).
- Hướng dẫn nhập môn nhớ theo tài khoản (bảng `HuongDanDaXem`): người mới được mời bằng thẻ nhỏ, xem hoặc bỏ qua thì không mời lại; hướng dẫn riêng từng trang chỉ gợi ý bằng chấm trên nút lá cờ.
- Trang giới thiệu công khai `/gioi-thieu` (chưa đăng nhập mở `/` sẽ tới đây): bản chạy thử tự động trong khung ứng dụng, ba mảng nghiệp vụ, bảng phân quyền theo vai trò, năm bước, bảy quy tắc bảo vệ dữ liệu.
- Cột Khoá · niên khoá (Hệ đào tạo) thu gọn/mở rộng được. Điện thoại: menu thành ngăn kéo.

**Giao diện**
- Phong cách "Sổ bộ" (`wwwroot/phong-cach.css`): tối giản, dữ liệu trước, một màu thương hiệu, mỗi mảng một màu nhận diện; phông tiêu đề Be Vietnam Pro (OFL, đặt sẵn trong `wwwroot/fonts`).
- Trang chủ theo thứ tự ưu tiên: lời chào và tóm tắt việc hôm nay, lối tắt theo vai trò, bốn chỉ số "Hôm nay", vùng làm việc, quy mô Học viện.
- Đường dẫn luôn một dòng; nút Sáng/Tối chỉ còn ở trang công khai (trong hệ thống dùng Cài đặt).
- Kiểm thử: NghiepVu 502, TaiKhoan 74, ThongBao 29 đều đạt.

## 10/11/2026 (khuya): Xuất Excel lịch phân công trực ban

- Tab Lịch trực có nút **Xuất Excel**: chọn khoảng ngày (mặc định tháng đang xem) và nội dung, xem trước số liệu rồi tải tệp.
- Các trang tính: Lịch trực (mỗi ngày làm việc một dòng, tô màu trạng thái), Dạng lịch (theo tuần, in vừa một trang ngang để dán bảng tin), Số ca (gồm cả người chưa có ca), Báo cáo đã chốt. Quản lý khoa/Admin chọn thêm bảng **Toàn Học viện** (ngày × 12 khoa, ô chưa phân công tô đỏ).
- Quyền theo trang trực ban: cán bộ chỉ xuất khoa mình; tệp không có dữ liệu cá nhân; mỗi lần xuất ghi nhật ký.
- Kiểm thử: thêm 5 kiểm tra (NghiepVu 502).

## 10/11/2026 (tối): Trực ban khoa

- Trang mới **Trực ban** (`/khoa/truc-ban`, Quản lý khoa → Trực ban). Thiết kế ở `docs/THIET_KE_TONG_THE.md` mục 6.6.
- Quyền theo ca: đúng ngày trực, người trực điểm danh đi làm cho cán bộ toàn khoa, nắm quân số học viên học chuyên đề của khoa ở mức số liệu, ghi sổ trực ban, chốt báo cáo. Hết ngày, đổi người trực hoặc chốt báo cáo thì quyền tự hết.
- Lịch trực theo tháng: chọn người từng ngày hoặc xếp tự động luân phiên đều; đếm số ca từng người.
- Báo cáo đã chốt giữ số liệu cán bộ và học viên tại lúc chốt.
- Giảng dạy của tôi: thẻ "Hôm nay bạn trực ban khoa" hoặc ngày trực tiếp theo.
- Dữ liệu mẫu: lịch trực 12 khoa từ 14 ngày trước tới 14 ngày sau; tài khoản `giangvien` trực khoa KH-01 hôm nay.
- Trợ giúp: thêm dòng trực ban vào ma trận vai trò. Dịch Anh/Lào đủ trang (`Services/TuDienGiaoDienTrucBan.cs`).
- Kiểm thử: thêm 26 kiểm tra (NghiepVu 497).

## 10/11/2026 (chiều): Vòng góp ý 9 điểm

**Khung giao diện**
- Cuộn quá đầu/cuối trang không còn kéo thanh bên và đầu trang theo (tắt hiệu ứng nảy ở gốc trang).
- Khối xếp liền nhau luôn cách nhau ít nhất 16 px (ví dụ ô lọc ngay dưới thẻ "Thêm phân công").
- Mục con trên thanh bên quá dài thì xuống dòng thay vì bị cắt, rê chuột hiện đủ tên.

**Lịch đào tạo**
- Rê chuột vào mã lớp: thẻ thông tin lớp và các chuyên đề trong tuần. Rê chuột vào một buổi: chuyên đề, buổi thứ mấy, giờ, giảng viên, phòng, số có mặt, nội dung sổ đầu bài.
- Chọn ngày trên ô lịch: nhảy đến tuần chứa ngày đó, tô nổi ngày đã chọn ở mọi kiểu xem, có nút "Chỉ ngày này", nút nhanh Hôm nay / Ngày mai; bảng "lớp đi học / không có lịch" theo ngày đã chọn.
- Ô "Không có lịch" theo khoá: rê chuột thấy cụ thể lớp nào, bấm để mở trang mới **Tình hình lên lớp** (`/dao-tao/lich/tinh-hinh`): tổng hợp, lọc theo lý do nghỉ, từng hệ → khoá → lớp kèm buổi gần nhất và buổi tiếp theo.

**Quản lý học viên**
- Tách theo hệ: Tổng quan các hệ → Hệ chính quy / Hệ văn bằng 2 → khoá (niên khoá, năm thứ mấy) → lớp. Đường dẫn và breadcrumb theo đúng cấp.
- Bộ chọn cấp ở Danh sách học viên đổi thành Hệ → Khoá → Lớp.

**Bản tin**
- Ảnh minh hoạ vẽ riêng (SVG, `wwwroot/images/ban-tin/`) chọn theo nội dung bài; thư viện "Hình ảnh hoạt động", "Lối tắt nghiệp vụ".
- Bài viết có liên kết nội bộ `[chữ](/đường-dẫn)` trong thân bài và hộp "Xem thêm trên hệ thống".

**Tiếng Lào (và tiếng Anh)**
- Bổ sung khoảng 1.100 mục từ điển (`Services/TuDienGiaoDienBoSung.cs`, `TuDienGiaoDienBoSung2.cs`), gồm cả tên khoa, chuyên đề, phòng, cấp bậc, văn bản, bản tin, trợ giúp.
- Bộ dịch trình duyệt hiểu khuôn có số ("20 lớp đang học"), tách theo dấu "·", giữ tên người và chỉ dịch cấp bậc, dịch nội dung nhiều dòng; thân bài bản tin dịch nguyên đoạn ở máy chủ.
- Hướng dẫn nhập môn có bản tiếng Lào.
- Đã rà mọi trang với 7 vai trò ở chế độ tiếng Lào: chỉ còn tên người, số hiệu văn bản giữ nguyên.

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
