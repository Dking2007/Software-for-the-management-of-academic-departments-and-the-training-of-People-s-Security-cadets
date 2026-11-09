# Tài khoản và phân quyền

Dữ liệu tài khoản lưu tại App_Data/tai-khoan.db và tồn tại sau khi dừng chương trình. Phiên đăng nhập mất khi khởi động lại. Dữ liệu khoa/lớp/học viên/cán bộ là dữ liệu tổng hợp, lưu tại App_Data/du-lieu.db.

## Tài khoản mẫu (chỉ Development)

| Tên đăng nhập | Mật khẩu mẫu | Vai trò | Phạm vi ghi |
|---|---|---|---|
| admin | demo123 | Admin | Tất cả, tài khoản, nhật ký |
| nguoixem | demo123 | Guest | Không |
| quanlykhoa | demo123 | Manager (Khoa) | Khoa, tổ, cán bộ, chuyên môn giảng dạy |
| quanlydaotao | demo123 | Manager (DaoTao) | Chuyên đề, lịch, phòng; cấp tài khoản giảng viên |
| quanlyhocvien | demo123 | Manager (Lop) | Lớp, học viên, điểm danh, điểm; cấp tài khoản học viên |
| giangvien | demo123 | GiangVien (CB006) | Điểm danh, nhập điểm lớp mình dạy |
| hocvien | demo123 | HocVien (học viên đầu tiên lớp B1D55, năm thứ 4) | Tự điểm danh, lịch học, bảng điểm các năm học/học kỳ, lịch thi |

Vai trò Giảng viên và Học viên lưu mã hồ sơ gắn kèm trong cột `PhamVi` (mã cán bộ / mã học viên); Manager lưu mảng `Khoa`, `DaoTao` hoặc `Lop`. Cấp tài khoản chỉ đi xuống: Quản lý đào tạo chỉ tạo/sửa/xoá tài khoản Giảng viên, Quản lý học viên chỉ tài khoản Học viên, Admin mọi vai trò.

Khởi động tự đổi tên tài khoản mẫu guest thành nguoixem theo mã cố định, không đổi mật khẩu. Nếu trùng tên tài khoản khác, ứng dụng báo lỗi thay vì ghi đè. Các tài khoản mẫu không thể sửa/xóa/cấp lại mật khẩu qua trang quản trị; tài khoản tạo mới có đầy đủ thao tác. Tất cả tài khoản mẫu bị từ chối đăng nhập trong Production. Không dùng mật khẩu mẫu khi triển khai.

## Quyền và dữ liệu cá nhân

Manager không thấy mục quản trị và không truy cập được trang/dịch vụ quản lý tài khoản. Dịch vụ kiểm tra quyền cho từng thao tác ghi, không chỉ ẩn nút.

Manager Khoa xem dữ liệu cá nhân của cán bộ; Manager Lớp xem dữ liệu cá nhân của học viên. Phân hệ còn lại vẫn tra cứu được dữ liệu thông thường nhưng bị lọc mã cán bộ/học viên, SĐT, CCCD, email và địa chỉ thường trú (nếu mô hình có trường đó) tại dịch vụ. Guest cũng bị lọc thông tin này để không thể dùng tài khoản tự đăng ký để vượt giới hạn. Cán bộ hiện chưa có trường địa chỉ thường trú trong mô hình.

Thay đổi quyền/phạm vi/mật khẩu/khóa hoặc xóa tài khoản làm phiên cũ mất hiệu lực khi kiểm tra quyền/yêu cầu tiếp theo. Dữ liệu người dùng đã xem trước đó không thể thu hồi khỏi trí nhớ hoặc ảnh chụp của họ.

## Trang quản lý tài khoản

Đăng nhập admin, mở Quản trị hệ thống → Tài khoản và phân quyền. Tìm theo mã/tên và lọc vai trò. Thêm hoặc sửa bằng biểu mẫu; mã tài khoản không đổi. Manager phải chọn Khoa hoặc Lớp đào tạo. Không được tự sửa/xóa tài khoản đang đăng nhập. Xóa cần xác nhận.

Cột mật khẩu hiển thị demo123 cho tài khoản mẫu; tài khoản thông thường hiển thị Không thể xem lại, không chứa mật khẩu/bản băm. Khi thêm nhập mật khẩu ban đầu; khi sửa nhập mật khẩu mới để cấp lại, để trống để giữ nguyên. Mật khẩu tài khoản mới tối thiểu 12 ký tự gồm hoa, thường, số và ký tự đặc biệt.

## Khởi chạy và kiểm tra

Dừng phiên Visual Studio cũ rồi F5. Nâng schema bổ sung PhamVi tự thực hiện một lần, giữ các tài khoản cũ. Nên sao lưu App_Data trước khi nâng cấp môi trường có dữ liệu quan trọng. Chỉ chạy một tiến trình khi nâng schema.

Thử mỗi tài khoản mẫu: Manager chỉ có nút sửa trong vùng mình; vùng đối diện không hiện các cột cá nhân. Admin thêm tài khoản, đổi phạm vi và cấp lại mật khẩu; thử đăng nhập bằng mật khẩu cũ và thử thao tác bằng tab cũ. Dừng/chạy lại để kiểm tra lưu tài khoản.

Trước triển khai thật còn cần HTTPS, sao lưu/khôi phục, rà soát bản vá, nhật ký quản trị bền vững, luồng khôi phục tài khoản và kiểm thử trình duyệt/triển khai. Kết quả kiểm thử tự động không bảo đảm an toàn tuyệt đối. Cơ chế phiên hiện phù hợp một tiến trình máy chủ.

Admin riêng được thiết lập bằng cấu hình bí mật KhoiTaoAdmin:TenDangNhap và KhoiTaoAdmin:MatKhau (User Secrets trong Development hoặc cấu hình bảo mật máy chủ). Không ghi mật khẩu thật vào mã nguồn. Xóa cấu hình khởi tạo sau khi tạo thành công.

## Kết quả kiểm tra thay đổi Manager

Build: 0 lỗi, 0 cảnh báo. Bộ kiểm thử tài khoản/phân quyền: 56 kiểm tra PASS, gồm tài khoản mẫu, CRUD tài khoản, đặt lại mật khẩu, thu hồi phiên, phạm vi Manager, lọc dữ liệu cá nhân tại dịch vụ, giữ mã khi đổi tên và nâng schema SQLite cũ lặp lại được.

Mã kiểm thử: KiemThu/TaiKhoan/Program.cs.txt (đuôi .txt tránh bị dự án kiểm thử cũ biên dịch lẫn). Chạy từ thư mục gốc bằng:

```powershell
dotnet run --project KiemThu/TaiKhoan/TaiKhoan.csproj -p:OutputPath=D:/T01/LTMT/Nhom2-BTL/.tmp/test-output/
```

Lệnh dùng database tạm riêng, không sửa database ứng dụng. Chưa kiểm tra trực quan bằng trình duyệt hoặc kiểm thử xâm nhập. Lần chạy kiểm thử có cảnh báo NU1900 do môi trường không truy cập được nguồn kiểm tra lỗ hổng NuGet; không coi đây là kết quả rà soát lỗ hổng gói phụ thuộc.

