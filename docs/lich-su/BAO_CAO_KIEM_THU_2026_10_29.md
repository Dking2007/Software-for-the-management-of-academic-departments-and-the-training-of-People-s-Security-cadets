# Báo cáo kiểm thử ngày 29/10/2026

## Kết quả

Lệnh chạy từ thư mục dự án: `dotnet run --project KiemThu/NghiepVu.csproj --no-restore`.
Ứng dụng được biên dịch thành công trong quá trình chạy kiểm thử. Tổng cộng 115 kiểm tra đạt, không có kiểm tra thất bại.

Các nhóm đã kiểm tra: dữ liệu khởi tạo, thêm/sửa/xóa, chuyển lớp/tổ/khoa, cập nhật thống kê, ràng buộc dữ liệu, bản sao độc lập, từ chối thao tác ghi với khách và Viewer, vé đăng nhập demo, HTML thành phần theo vai trò và tìm kiếm lớp.

## Vấn đề phát hiện và đã xử lý

1. Ô tìm kiếm chính chỉ cập nhật khi thay đổi kết thúc. Năm trang danh sách nay xử lý sự kiện nhập, cập nhật từ khóa ngay và quay về trang đầu.
2. Các bộ lọc lớp, tổ, hệ và khóa chưa cho gõ tìm lựa chọn. Tất cả bộ lọc trên trang danh sách đã bật ô nhập tìm kiếm, tìm theo chuỗi chứa và không phân biệt hoa thường. Bộ lọc khoa cũng áp dụng cùng cách tìm. Người dùng gõ trong phần tìm kiếm của danh sách mở ra rồi chọn giá trị có sẵn; ô tìm kiếm chính nhận từ khóa bất kỳ.
3. Trang lớp hứa tìm theo hệ đào tạo nhưng biểu thức lọc thiếu trường HeDaoTao. Đã bổ sung và kiểm tra: “chính QUY” có khoảng trắng ngoài trả 90 lớp; “Văn bằng 2” trả 16 lớp; mã lớp trả đúng một lớp; từ khóa không khớp trả rỗng; xóa từ khóa trả 106 lớp.
4. Khối CSS cuối ghi đè bảng màu xanh lá bằng navy. Đã khôi phục màu xanh lá #1e513b / #3c8058 và đồng bộ biến màu Radzen, nền trang, sidebar, bảng và trang đăng nhập.
5. Đầu ra kiểm thử trùng thư mục DLL đang được ứng dụng sử dụng, gây lỗi sao chép MSB3026. Đã đổi đường dẫn đầu ra của ProjectReference kiểm thử sang bin/KiemTraTimKiem/ và chạy thành công, không dừng ứng dụng đang mở.

## Tệp đã sửa

- Components/Pages/Khoa.razor
- Components/Pages/ToChuyenMon.razor
- Components/Pages/Lop.razor
- Components/Pages/HocVien.razor
- Components/Pages/CanBoGiangVien.razor
- wwwroot/app.css
- KiemThu/Program.cs
- KiemThu/NghiepVu.csproj

## Tự kiểm tra giao diện

Khởi động lại ứng dụng bằng Visual Studio, mở trang Khoa, Tổ, Lớp, Học viên và Cán bộ. Gõ từ khóa vào ô tìm kiếm chính để xem kết quả ngay. Mở bộ lọc và gõ vào ô tìm kiếm trong danh sách. Tại trang Lớp, thử “Văn bằng 2”. Thử nhập từ khóa không khớp, xóa từ khóa và tìm từ một trang phân trang phía sau. Nhấn Ctrl+F5 trong trình duyệt nếu CSS cũ còn được lưu.

## Giới hạn

Chưa kiểm tra tương tác bàn phím, popup, độ trễ khi gõ hoặc bố cục trên trình duyệt thật; kiểm thử HTML không thay thế kiểm thử tương tác. NuGet cảnh báo NU1900 vì không truy cập được nguồn kiểm tra lỗ hổng; không thể kết luận về lỗ hổng gói từ lần chạy này.

Dữ liệu và đăng nhập chỉ phục vụ minh họa cục bộ. Dữ liệu tổng hợp nằm trong bộ nhớ và mất khi khởi động lại; không dùng cơ chế đăng nhập demo cho triển khai thực tế.