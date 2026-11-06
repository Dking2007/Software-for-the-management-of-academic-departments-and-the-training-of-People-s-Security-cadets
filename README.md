# Phần mềm quản lý khoa và lớp đào tạo (bản minh họa)

Ứng dụng web học tập mô phỏng việc quản lý khoa, tổ chuyên môn, cán bộ giảng viên, lớp đào tạo, học viên, lịch đào tạo, điểm danh và điểm số tại Học viện An ninh nhân dân. Dự án là bài tập lớn của nhóm, phục vụ mục đích học Blazor và chạy thử cục bộ. Thiết kế tổng thể và kế hoạch nằm ở [docs/THIET_KE_TONG_THE.md](docs/THIET_KE_TONG_THE.md).

> **Lưu ý.** Toàn bộ hồ sơ trong ứng dụng là dữ liệu tổng hợp, không đại diện người thật. Cơ chế đăng nhập, tài khoản mẫu và phân quyền chỉ để minh họa. Không dùng bản này cho triển khai thực tế.

## 1. Công nghệ

| Thành phần | Phiên bản | Vai trò trong dự án |
| --- | --- | --- |
| .NET SDK | 8.0 | Nền tảng biên dịch và chạy ứng dụng |
| Blazor Web App | .NET 8, Interactive Server | Giao diện chạy trên máy chủ, cập nhật qua kết nối SignalR |
| Radzen.Blazor | 6.4.0 | Bảng dữ liệu, biểu mẫu, hộp thoại và bộ lọc |
| ASP.NET Core Identity | 8.0.22 | Băm mật khẩu, khóa tài khoản sau nhiều lần đăng nhập sai |
| ClosedXML | 0.104.2 | Xuất, nhập tệp Excel (.xlsx) |
| EF Core SQLite | 8.0.22 | Lưu tài khoản (`App_Data/tai-khoan.db`) và dữ liệu nghiệp vụ (`App_Data/du-lieu.db`) |

## 2. Chạy ứng dụng

Máy cần cài .NET 8 SDK. Cách đơn giản nhất là chạy tệp `start.cmd` tại thư mục gốc dự án.

```powershell
.\start.cmd          # profile https, địa chỉ https://localhost:7061
.\start.cmd http     # profile http, địa chỉ http://localhost:5026, không cần chứng chỉ HTTPS
```

Tệp này kiểm tra .NET 8 SDK, tải gói thư viện rồi khởi động ứng dụng. Thư mục gốc chứa cả `Nhom2-BTL.csproj` và `Nhom2-BTL.slnx`, vì vậy khi chạy lệnh thủ công cần chỉ rõ tệp dự án.

```powershell
dotnet restore Nhom2-BTL.csproj
dotnet run --project Nhom2-BTL.csproj --launch-profile https
```

Trong Visual Studio, mở `Nhom2-BTL.slnx` và nhấn F5. Nhấn `Ctrl+C` trong Terminal để dừng ứng dụng.

## 3. Tài khoản mẫu

Bảy tài khoản dưới đây chỉ được tạo khi ứng dụng chạy ở môi trường `Development`. Ở môi trường `Production`, ứng dụng từ chối đăng nhập bằng các tài khoản này.

| Tên đăng nhập | Mật khẩu | Vai trò | Phạm vi được ghi |
| --- | --- | --- | --- |
| `admin` | `demo123` | Admin | Toàn bộ dữ liệu, tài khoản, nhật ký |
| `quanlykhoa` | `demo123` | Quản lý khoa | Khoa, tổ, cán bộ, chuyên môn giảng dạy |
| `quanlydaotao` | `demo123` | Quản lý đào tạo | Chuyên đề, lịch, phòng; cấp tài khoản giảng viên |
| `quanlyhocvien` | `demo123` | Quản lý học viên | Lớp, học viên, điểm danh, điểm; cấp tài khoản học viên |
| `giangvien` | `demo123` | Giảng viên (cán bộ CB006) | Điểm danh và nhập điểm cho lớp, chuyên đề mình dạy |
| `hocvien` | `demo123` | Học viên năm thứ 4 (học viên đầu tiên lớp B1D55) | Tự điểm danh, lịch học hiện hành, bảng điểm 4 năm học / 7 học kỳ, lịch thi |
| `nguoixem` | `demo123` | Người xem (Guest) | Không được ghi |

Người dùng có thể tự tạo tài khoản tại `/dang-ky`. Tài khoản mới luôn nhận vai trò Guest. Cấp tài khoản chỉ đi xuống: Admin cấp mọi vai trò, Quản lý đào tạo cấp tài khoản giảng viên, Quản lý học viên cấp tài khoản học viên.

## 4. Phân quyền

Mỗi tài khoản một vai trò: Admin, ba manager (Khoa / Đào tạo / Học viên), Giảng viên, Học viên và Người xem. Khi chưa xác định được vai trò, ứng dụng từ chối truy cập. Ma trận đầy đủ và bảy hàng rào nằm ở mục 6 của tài liệu thiết kế. Tóm tắt:

- Dịch vụ trả **mặt cắt dữ liệu theo vai trò**, không trả bản ghi gốc: Quản lý khoa và Quản lý đào tạo không nhận danh sách, điểm từng học viên; Quản lý học viên không nhận dữ liệu cá nhân cán bộ; giảng viên chỉ thấy lớp mình dạy và không thấy CCCD, SĐT, địa chỉ học viên; học viên chỉ thấy phần của mình, bạn cùng lớp chỉ họ tên và lớp.
- Dữ liệu cá nhân hiển thị dạng che; bấm "Hiện đủ" để xem, lượt xem được ghi nhật ký.
- Mọi thao tác thêm, sửa, xoá tự ghi **nhật ký** (giá trị cũ → mới) ngay trong `SaveChanges`. Nhật ký chỉ thêm; CSDL có trigger chặn sửa và xoá. Admin xem tại `/quan-tri/nhat-ky`.

- **Ranh giới ba mảng**: mỗi mảng có một chủ sở hữu được thêm, sửa, xoá, nhập Excel (Khoa: khoa, tổ, cán bộ, chấm công; Đào tạo: chuyên ngành, chuyên đề, phòng, lịch, chương trình, lịch thi; Học viên: lớp, hồ sơ học viên, điểm danh, điểm). Menu của từng manager chỉ hiện mảng mình sở hữu và mục "phối hợp" cần cho công việc. Việc chung: xét tư cách thi (QLHV đề xuất → Khoa xác nhận → Đào tạo duyệt) và **sổ đầu bài online** (giảng viên ghi, ba quản lý xem). Ma trận đầy đủ ở trang Trợ giúp.

Việc ẩn nút trên giao diện chỉ hỗ trợ trải nghiệm. Quyền được kiểm tra lại ở lớp dịch vụ (`Services/DichVuPhanQuyenDemo.cs`, `Services/DichVuQuanLyDuLieuDemo.cs`) trước mỗi thao tác ghi. Khi Admin đổi vai trò, mật khẩu hoặc khóa một tài khoản, phiên đăng nhập cũ của tài khoản đó mất hiệu lực ở yêu cầu kế tiếp.

## 4b. Tính năng chính

| Mảng | Tính năng |
| --- | --- |
| Quản lý khoa | Danh sách khoa, tổ, cán bộ; **sơ đồ tổ chức** hai sơ đồ (dạng cột và canvas pan/zoom, ý tưởng từ repo LTMT); **lịch sử bổ nhiệm** lãnh đạo khoa/tổ |
| Quản lý đào tạo | Chuyên ngành, khoá/niên khoá, chuyên đề và giáo viên đủ điều kiện, phòng học, **lịch tuần** có chặn trùng giảng viên/phòng/lớp, sức chứa, phòng bảo trì |
| Quản lý học viên | Lớp (mã tự sinh; hệ chính quy, **liên thông**, văn bằng 2), học viên, trang lớp 4 tab: học viên, lịch, **điểm danh**, **điểm 30/70** |
| Cá nhân | Giảng dạy của tôi (giảng viên), Học tập của tôi với tự điểm danh (học viên), **hồ sơ cá nhân** và đổi mật khẩu |
| Thông tin | **Bản tin Học viện** bố cục kiểu báo điện tử (tin mới chạy chữ, tiêu điểm, chuyên mục, đọc nhiều); nội dung giả lập |
| Chương trình | **Chương trình & phân công** (khoa phụ trách lớp nào, khi nào, lớp đang học gì), **xét tư cách thi** (QLHV → Khoa → Đào tạo), **lịch thi**, tab chuyên cần, lớp đi học/nghỉ hôm nay, **điểm danh đi làm** của cán bộ |
| Hệ thống | **Văn bản, thông báo** theo thể thức hành chính (số/ký hiệu, mức độ, hiệu lực, nơi nhận, đã đọc, chuông có số chưa đọc, ảnh minh hoạ); **xuất/nhập Excel** (ClosedXML) đúng mặt cắt quyền; tìm kiếm Ctrl+K; nhật ký chỉ thêm; trang chủ có Cần chú ý, văn bản ưu tiên, hoạt động gần đây |
| Nhập Excel | Tải tệp mẫu có danh sách chọn → xem trước lỗi từng dòng → xác nhận. Có cho học viên, lớp, cán bộ, chuyên đề, phòng học và **bảng điểm** (tệp mẫu điền sẵn danh sách lớp) |
| Sổ đầu bài | Tab "Sổ đầu bài" ở trang lớp: nội dung bài, nhận xét, xếp loại tiết A–D, sĩ số có mặt lấy từ điểm danh |
| Thi cuối học phần | Tổng quan tiến độ, xử lý hàng loạt theo bước của vai trò, QLHV điều chỉnh tư cách từng học viên (bắt buộc lý do), lịch sử xử lý, thông tin bài thi (thời lượng, cấu trúc đề, phạm vi ôn tập, tài liệu), gửi thông báo thi tới học viên và giảng viên của lớp |
| Trường dữ liệu mở rộng | `/quan-tri/truong-du-lieu`: Admin và quản lý sở hữu mảng thêm trường mới cho khoa, tổ, cán bộ, lớp, học viên, chuyên đề, phòng (văn bản, số, ngày, có/không, danh sách); trường tự hiện trong biểu mẫu, cột bảng, thẻ xem nhanh; lưu ở bảng riêng, có nhật ký |
| Xem nhanh | Rê chuột vào tên cán bộ, học viên, mã lớp, tên khoa, tổ để xem thẻ thông tin (đã cắt theo quyền) |
| Cài đặt cá nhân | Sáng/tối/theo hệ thống, màu chủ đạo, cỡ chữ (có tự động), mật độ, độ rộng nội dung, phông, bo góc, tương phản cao, giảm chuyển động; nội dung giãn theo cửa sổ khi thu nhỏ trình duyệt |
| Ngôn ngữ | Tiếng Việt, English, ພາສາລາວ (tiếng Lào): dịch nhãn giao diện trên toàn trang (`wwwroot/dich-trang.js` + `Services/TuDienGiaoDien.cs`); câu chưa có trong từ điển giữ nguyên tiếng Việt |

## 5. Dữ liệu

| Loại dữ liệu | Nơi lưu | Vòng đời |
| --- | --- | --- |
| Khoa, tổ, cán bộ, lớp, học viên | SQLite `App_Data/du-lieu.db`, đệm đọc trong `KhoDuLieuDemo` | Giữ lại sau khi khởi động lại |
| Chuyên đề, chuyên môn giảng dạy, phòng, buổi học, điểm danh, kết quả, nhật ký | SQLite `App_Data/du-lieu.db` | Giữ lại sau khi khởi động lại |
| Tài khoản và vai trò | SQLite, `App_Data/tai-khoan.db` | Giữ lại sau khi khởi động lại |
| Phiên đăng nhập | Bộ nhớ, lớp `KhoPhienDemo` | Mất khi khởi động lại, người dùng phải đăng nhập lại |

Lần chạy đầu tiên (CSDL trống), ứng dụng sinh dữ liệu mẫu theo công thức cố định rồi lưu vào CSDL:

| Đối tượng | Số bản ghi | Quy tắc sinh |
| --- | --- | --- |
| Khoa | 12 | Mã `KH-01` đến `KH-12` |
| Tổ chuyên môn | 24 | Hai tổ cho mỗi khoa |
| Cán bộ giảng viên | 108 | Chín cán bộ cho mỗi khoa, gồm trưởng khoa, hai phó khoa và cán bộ các tổ |
| Lớp đào tạo | 106 | 90 lớp chính quy (khóa D53 đến D58) và 16 lớp văn bằng 2 (khóa K3 đến K6) |
| Học viên | 4.240 | Bốn mươi học viên cho mỗi lớp |
| Chuyên đề | 24 | Hai chuyên đề cho mỗi khoa; mọi cán bộ của khoa đủ điều kiện dạy |
| Phòng | 24 | Giảng đường, phòng máy, phòng thi, sân tập, một phòng nhỏ 30 chỗ, một phòng bảo trì |
| Buổi học | khoảng 350 | Lịch 4 tuần (tuần trước đến hai tuần sau ngày sinh dữ liệu) cho lớp khóa D58 và K6, không trùng giảng viên/phòng |
| Điểm danh, điểm | khoảng 5.600 / 4.700 | Điểm danh các buổi đã qua; điểm kiểm tra cho khoảng 85% học viên (phần còn lại để thấy trường hợp thiếu điểm) |

Muốn đặt lại dữ liệu mẫu (và lịch theo ngày hiện tại): tắt ứng dụng, xoá các tệp `App_Data/du-lieu.db*` rồi chạy lại. CSDL cũ được tự bổ sung bảng còn thiếu khi nâng cấp phiên bản, không mất dữ liệu. Chỉ chạy một tiến trình máy chủ trên một tệp CSDL.

## 6. Cấu trúc thư mục

```text
Nhom2-BTL/
├── Program.cs              # Đăng ký dịch vụ, xác thực cookie, phân quyền, tuyến /phien/*
├── Models/                 # Lớp mô hình: Khoa, ToChuyenMon, CanBoGiangVien, LopDaoTao, HocVien
├── Services/               # Kho dữ liệu mẫu, dịch vụ tài khoản, phân quyền, thông báo
├── Components/
│   ├── Pages/              # Trang có địa chỉ riêng (@page)
│   └── Layout/             # Bố cục, thanh điều hướng, biểu mẫu thêm/sửa
├── wwwroot/                # CSS, JavaScript, phông chữ, hình ảnh
├── App_Data/               # Cơ sở dữ liệu tài khoản (không đưa lên Git)
└── KiemThu/                # Chương trình kiểm thử độc lập
```

Các trang chính gồm:

| Địa chỉ | Nội dung |
| --- | --- |
| `/` | Trang chủ, thống kê tổng hợp |
| `/khoa`, `/khoa/to`, `/khoa/can-bo` | Khoa, tổ chuyên môn, cán bộ giảng viên |
| `/lop`, `/hoc-vien` | Lớp đào tạo, học viên |
| `/dang-nhap`, `/dang-ky` | Đăng nhập, đăng ký |
| `/quan-tri/tai-khoan` | Quản lý tài khoản (chỉ Admin) |

## 7. Kiểm thử

Thư mục `KiemThu/` chứa các chương trình console gọi trực tiếp lớp dịch vụ. Chạy các lệnh sau từ thư mục gốc dự án.

```powershell
# Nghiệp vụ dữ liệu + tích hợp mốc 1 (vai trò, lịch, điểm danh, điểm, nhật ký, cấp tài khoản, tìm kiếm, lưu bền)
dotnet run --project KiemThu/NghiepVu.csproj

# Tài khoản và phân quyền: dùng cơ sở dữ liệu tạm, không sửa App_Data
dotnet run --project KiemThu/TaiKhoan/TaiKhoan.csproj

# Văn bản, thông báo
dotnet run --project KiemThu/ThongBao/ThongBao.csproj
```

Lần chạy ngày 06/11/2026: NghiepVu 440/440, TaiKhoan 74/74, ThongBao 29/29 đạt.

Kết quả các lần chạy trước được ghi trong `BAO_CAO_KIEM_THU_2026_10_29.md` và `HUONG_DAN_TAI_KHOAN.md`. Các bộ kiểm thử này chưa bao gồm kiểm thử trên trình duyệt và kiểm thử xâm nhập.

## 8. Giới hạn

- Dữ liệu nghiệp vụ lưu trong `App_Data/du-lieu.db` (SQLite). Muốn đặt lại dữ liệu mẫu: tắt ứng dụng, xoá `du-lieu.db` (và `du-lieu.db-shm`, `du-lieu.db-wal` nếu có) rồi chạy lại. Chưa có sao lưu tự động.
- Cơ chế phiên chỉ phù hợp với một tiến trình máy chủ.
- Mật khẩu `demo123` của tài khoản mẫu được ghi cứng trong mã nguồn và công khai trong tài liệu này.
- Trước khi dùng cho môi trường thật, cần thay bằng hệ thống danh tính, cơ sở dữ liệu, nhật ký quản trị và quy trình bảo vệ dữ liệu phù hợp.

Tài liệu chi tiết hơn về tài khoản, phạm vi Manager và cách khởi tạo Admin riêng bằng User Secrets nằm trong `HUONG_DAN_TAI_KHOAN.md`.
