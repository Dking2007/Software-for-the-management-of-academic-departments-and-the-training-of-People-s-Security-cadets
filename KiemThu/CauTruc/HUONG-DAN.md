# Kiểm tra cấu trúc Khoa/Lớp

Bộ kiểm thử này được chuẩn bị cùng thay đổi, chưa được chạy bởi trợ lý theo yêu cầu không tự build/chạy.

Tại thư mục `D:\T01\LTMT\WebQuanLy-LTMT`, sau khi build ứng dụng, chạy:

```powershell
dotnet run --project KiemThu/CauTruc/CauTruc.csproj
```

Lệnh biên dịch và kiểm tra bằng một kho bộ nhớ riêng. Không mở ứng dụng web, không tạo cơ sở dữ liệu và không thay dữ liệu của phiên đang dùng. Kỳ vọng: 9 dòng PASS và dòng hoàn tất. Nếu restore không truy cập được NuGet, kiểm tra kết nối/gói đã được khôi phục trong Visual Studio.

## Kiểm tra giao diện sau khi tự chạy ứng dụng

1. Sidebar có đúng ba nhóm, thu gọn/mở rộng và menu bên cạnh không bị lệch; chữ cạnh logo không xuống dòng.
2. Vào Quản lý học viên → Danh sách lớp → bấm mã lớp. Kiểm tra mã, hệ, khóa, niên khóa, chủ nhiệm, sĩ số và danh sách học viên bên dưới.
3. Tìm kiếm, đổi số dòng, bấm Bỏ bộ lọc trong chi tiết lớp. Không được xuất hiện học viên lớp khác.
4. Vào Quản lý khoa → bấm tên khoa → bấm tên tổ. Kiểm tra khoa/tổ ở phần thông tin và cán bộ bên dưới. Tab Cán bộ trong khoa phải gồm mọi tổ thuộc khoa.
5. Đổi mã lớp trên URL thành mã không tồn tại; đổi mã khoa sang khoa khác nhưng giữ mã tổ. Phải hiện Không tìm thấy đơn vị.
6. Sửa/xóa dữ liệu bằng Admin; kiểm tra tiêu đề và sĩ số cập nhật. Thử bằng Người xem: không được thay dữ liệu. Quyền ghi vẫn do dịch vụ cũ kiểm tra.
7. Quản lý đào tạo: số lớp, học viên theo chuyên ngành/khóa phải khớp các danh sách. D58/K6: 2026–2030; D57/K5: 2025–2029.
8. Cuộn ngang từng bảng: không còn cột bị ẩn, cột thao tác vẫn cố định, nền đặc. Khoa không còn cột Trạng thái.

Các thay đổi dữ liệu chỉ tồn tại trong bộ nhớ và có thể mất khi khởi động lại.
