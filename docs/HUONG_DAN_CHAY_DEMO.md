# Hướng dẫn chạy thử

Ứng dụng dùng Blazor Web App trên .NET 8, render mode Interactive Server và Radzen.Blazor 6.4.0.

## Chạy ứng dụng

Mở Terminal tại thư mục chứa `Nhom2-BTL.csproj`, sau đó chạy:

```powershell
dotnet restore
dotnet run
```

Mở địa chỉ HTTPS hoặc HTTP được in trong Terminal. Sau khi đăng nhập, chọn **Khoa**, **Lớp đào tạo** hoặc **Học viên** ở thanh điều hướng để kiểm tra các danh sách.

## Tài khoản thử nghiệm

| Vai trò | Tên đăng nhập | Mật khẩu |
| --- | --- | --- |
| Admin | `quantri` | `demo123` |
| Viewer | `xem` | `demo123` |

Đăng nhập tại `/dang-nhap`. Sau khi đăng nhập, tên và vai trò xuất hiện ở góc trên bên phải. Nút **Đăng xuất** xóa trạng thái trong phiên Blazor hiện tại và đưa về trang đăng nhập. Thử nhập sai mật khẩu để xem thông báo lỗi.

## Chức năng theo vai trò

- **Admin** (`quantri`): xem, tìm kiếm, sắp xếp, phân trang; thêm và chỉnh sửa khoa, lớp, học viên; xóa sau khi xác nhận.
- **Viewer** (`xem`): xem và tra cứu các danh sách; giao diện không hiện thao tác ghi. Service cũng kiểm tra claim Admin trước mỗi lần thay đổi dữ liệu.
- Xóa khoa đang được lớp sử dụng hoặc lớp đang có học viên sẽ bị từ chối để tránh liên kết mồ côi.
- Mã khoa, mã lớp và mã học viên không phân biệt hoa/thường khi kiểm tra trùng. Quan hệ lớp-khoa và học viên-lớp được xác thực lại ở service.
- Dữ liệu mẫu gồm 5 khoa, 50 lớp và 250 học viên giả lập ổn định trong tiến trình hiện tại. Mỗi lớp có 5 hồ sơ học viên mẫu.

## Giới hạn của bản demo

- Tài khoản và mật khẩu là dữ liệu giả, được kiểm tra trong bộ nhớ. Trạng thái đăng nhập tồn tại trong phiên Interactive Server; khi tải lại trang hoặc tạo phiên mới, người dùng cần đăng nhập lại.
- Đây không phải xác thực an toàn để triển khai thực tế. Không dùng tài khoản này cho hệ thống thật. Khi triển khai thật cần thay bằng giải pháp danh tính phù hợp và lưu thông tin xác thực an toàn.
- Tài khoản, vai trò và trạng thái phiên là cơ chế minh họa; không dùng cho triển khai thật. Thay đổi dữ liệu chỉ nằm trong bộ nhớ và sẽ mất khi ứng dụng dừng hoặc khởi động lại.
- Nếu chạy nhiều tiến trình/phiên bản ứng dụng, mỗi tiến trình có kho dữ liệu riêng; không có đồng bộ hay lưu bền vững.
- Toàn bộ hồ sơ khoa, lớp và học viên đều là dữ liệu giả lập, không đại diện cá nhân hoặc hồ sơ thật.
