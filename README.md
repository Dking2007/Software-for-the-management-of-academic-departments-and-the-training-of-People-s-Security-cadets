# Phần mềm quản lý khoa và lớp đào tạo (bản minh họa)

Ứng dụng web học tập mô phỏng việc quản lý khoa, tổ chuyên môn, cán bộ giảng viên, lớp đào tạo và học viên tại Học viện An ninh nhân dân. Dự án là bài tập lớn của Nhóm 2, phục vụ mục đích học Blazor và chạy thử cục bộ.

> **Lưu ý.** Toàn bộ hồ sơ trong ứng dụng là dữ liệu tổng hợp, không đại diện người thật. Cơ chế đăng nhập, tài khoản mẫu và phân quyền chỉ để minh họa. Không dùng bản này cho triển khai thực tế.

## 1. Công nghệ

| Thành phần | Phiên bản | Vai trò trong dự án |
| --- | --- | --- |
| .NET SDK | 8.0 | Nền tảng biên dịch và chạy ứng dụng |
| Blazor Web App | .NET 8, Interactive Server | Giao diện chạy trên máy chủ, cập nhật qua kết nối SignalR |
| Radzen.Blazor | 6.4.0 | Bảng dữ liệu, biểu mẫu, hộp thoại và bộ lọc |
| ASP.NET Core Identity | 8.0.22 | Băm mật khẩu, khóa tài khoản sau nhiều lần đăng nhập sai |
| EF Core SQLite | 8.0.22 | Lưu tài khoản trong tệp `App_Data/tai-khoan.db` |

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

Bốn tài khoản dưới đây chỉ được tạo khi ứng dụng chạy ở môi trường `Development`. Ở môi trường `Production`, ứng dụng từ chối đăng nhập bằng các tài khoản này.

| Tên đăng nhập | Mật khẩu | Vai trò | Phạm vi được ghi |
| --- | --- | --- | --- |
| `admin` | `demo123` | Admin | Toàn bộ dữ liệu và quản lý tài khoản |
| `quanlykhoa` | `demo123` | Manager | Khoa, tổ chuyên môn, cán bộ giảng viên |
| `quanlyhocvien` | `demo123` | Manager | Lớp đào tạo, học viên |
| `nguoixem` | `demo123` | Guest | Không được ghi |

Người dùng có thể tự tạo tài khoản tại `/dang-ky`. Tài khoản mới luôn nhận vai trò Guest và phải có mật khẩu tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Chỉ Admin mới nâng được vai trò của tài khoản khác.

## 4. Phân quyền

Ứng dụng có ba vai trò. Khi chưa xác định được vai trò, ứng dụng từ chối truy cập.

- **Guest** xem danh sách, tìm kiếm và xem chi tiết. Các trường cá nhân như số điện thoại, số CCCD, email và địa chỉ thường trú bị lọc khỏi kết quả.
- **Manager** có quyền của Guest, cộng thêm quyền thêm, sửa, xóa trong phạm vi được giao (Khoa hoặc Lớp). Manager chỉ xem được trường cá nhân của đối tượng thuộc phạm vi mình.
- **Admin** có toàn bộ quyền và quản lý tài khoản tại `/quan-tri/tai-khoan`.

Việc ẩn nút trên giao diện chỉ hỗ trợ trải nghiệm. Quyền được kiểm tra lại ở lớp dịch vụ (`Services/DichVuPhanQuyenDemo.cs`, `Services/DichVuQuanLyDuLieuDemo.cs`) trước mỗi thao tác ghi. Khi Admin đổi vai trò, mật khẩu hoặc khóa một tài khoản, phiên đăng nhập cũ của tài khoản đó mất hiệu lực ở yêu cầu kế tiếp.

## 5. Dữ liệu

Ứng dụng dùng hai nơi lưu dữ liệu với vòng đời khác nhau.

| Loại dữ liệu | Nơi lưu | Vòng đời |
| --- | --- | --- |
| Khoa, tổ, cán bộ, lớp, học viên | Bộ nhớ, lớp `KhoDuLieuDemo` (singleton) | Mất khi dừng ứng dụng, sinh lại khi khởi động |
| Tài khoản và vai trò | SQLite, `App_Data/tai-khoan.db` | Giữ lại sau khi khởi động lại |
| Phiên đăng nhập | Bộ nhớ, lớp `KhoPhienDemo` | Mất khi khởi động lại, người dùng phải đăng nhập lại |

Dữ liệu mẫu được sinh theo công thức cố định, không dùng số ngẫu nhiên. Mỗi lần khởi động, ứng dụng tạo lại cùng một bộ dữ liệu gồm:

| Đối tượng | Số bản ghi | Quy tắc sinh |
| --- | --- | --- |
| Khoa | 12 | Mã `KH-01` đến `KH-12` |
| Tổ chuyên môn | 24 | Hai tổ cho mỗi khoa |
| Cán bộ giảng viên | 108 | Chín cán bộ cho mỗi khoa, gồm trưởng khoa, hai phó khoa và cán bộ các tổ |
| Lớp đào tạo | 106 | 90 lớp chính quy (khóa D53 đến D58) và 16 lớp văn bằng 2 (khóa K3 đến K6) |
| Học viên | 4.240 | Bốn mươi học viên cho mỗi lớp |

Mọi thay đổi trên khoa, lớp, cán bộ và học viên chỉ tồn tại trong tiến trình đang chạy. Nếu chạy nhiều tiến trình cùng lúc, mỗi tiến trình giữ một bản dữ liệu riêng và không đồng bộ với nhau.

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
# Nghiệp vụ dữ liệu: thêm, sửa, xóa, ràng buộc, tìm kiếm, từ chối ghi với Guest
dotnet run --project KiemThu/NghiepVu.csproj

# Tài khoản và phân quyền: dùng cơ sở dữ liệu tạm, không sửa App_Data
dotnet run --project KiemThu/TaiKhoan/TaiKhoan.csproj
```

Kết quả các lần chạy trước được ghi trong `BAO_CAO_KIEM_THU_2026_10_29.md` và `HUONG_DAN_TAI_KHOAN.md`. Các bộ kiểm thử này chưa bao gồm kiểm thử trên trình duyệt và kiểm thử xâm nhập.

## 8. Giới hạn

- Dữ liệu nghiệp vụ nằm trong bộ nhớ, không có sao lưu hay lưu bền vững.
- Cơ chế phiên chỉ phù hợp với một tiến trình máy chủ.
- Mật khẩu `demo123` của tài khoản mẫu được ghi cứng trong mã nguồn và công khai trong tài liệu này.
- Trước khi dùng cho môi trường thật, cần thay bằng hệ thống danh tính, cơ sở dữ liệu, nhật ký quản trị và quy trình bảo vệ dữ liệu phù hợp.

Tài liệu chi tiết hơn về tài khoản, phạm vi Manager và cách khởi tạo Admin riêng bằng User Secrets nằm trong `HUONG_DAN_TAI_KHOAN.md`.
