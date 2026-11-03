# Kiểm thử lãnh đạo, hồ sơ trùng và sĩ số

## Kết quả

- 216 kiểm tra tự động đạt qua `dotnet run --project KiemThu/NghiepVu.csproj --no-restore`.
- Biên dịch thành công trong quá trình chạy bộ kiểm thử. Có cảnh báo NU1900 do không truy cập được dịch vụ kiểm tra lỗ hổng NuGet; không có lỗi biên dịch.
- Đã chạy trình duyệt với kho tổng hợp riêng trên localhost:5192: bảng khoa có hai phó khoa, rê chuột vào trưởng khoa và tổ trưởng mở đúng hồ sơ.
- Kho dùng trong kiểm thử độc lập với phiên người dùng. Không dùng thông tin người thật.

## Những lỗi đã sửa

- Khoa chỉ có một phó khoa: thêm vị trí thứ hai, dữ liệu mẫu có đúng 1 trưởng và 2 phó; chặn bổ nhiệm phó thứ ba.
- Thêm/sửa cán bộ chưa kiểm tra trùng SĐT; học viên chỉ kiểm tra CCCD: kiểm tra cả CCCD và SĐT ở máy chủ, trong cùng khóa ghi dữ liệu, trên cả hai danh sách.
- Hồ sơ cán bộ chưa có CCCD: thêm trường CCCD 12 chữ số, dữ liệu giả ổn định, biểu mẫu và cột hiển thị.
- Bảng khoa và tổ chưa có cột sĩ số cán bộ: tính từ hồ sơ hiện có khi đọc, không lưu bộ đếm độc lập.

## Các trường hợp đã kiểm tra

- Trùng toàn bộ các trường nghiệp vụ, kể cả dùng mã mới hoặc khác hoa/thường, khoảng trắng: từ chối với thông báo nhập lặp hồ sơ.
- Trùng họ tên + CCCD + SĐT nhưng khác ngày sinh/học vị hoặc thông tin khác: thông báo hồ sơ mâu thuẫn.
- Chỉ trùng CCCD, chỉ trùng SĐT, trùng cả hai nhưng khác tên, hai định danh thuộc hai hồ sơ khác nhau: từ chối và chỉ rõ trường trùng.
- Trùng họ tên nhưng CCCD/SĐT khác: cho phép.
- Sửa hồ sơ giữ định danh của chính nó: cho phép. Sửa sang định danh của người khác: từ chối.
- Trùng định danh giữa cán bộ và học viên: từ chối ở cả hai chiều.
- CCCD/SĐT sai định dạng, dữ liệu không hợp lệ, không đủ quyền: từ chối.
- Lưu bị từ chối: hồ sơ, sĩ số và sự kiện thay đổi đều không đổi.
- Thêm, xóa, chuyển học viên: sĩ số lớp, khóa, hệ và tổng cập nhật đúng; không đổi sĩ số cán bộ.
- Thêm, xóa, chuyển cán bộ: cột sĩ số khoa và tổ cũ/mới cập nhật đúng; không đổi sĩ số học viên.
- Hai yêu cầu lưu đồng thời dùng chung định danh: chỉ thêm một hồ sơ.
- Sửa khoa giữ cả hai phân công phó; sửa/xóa/đổi chức vụ cán bộ cập nhật tham chiếu lãnh đạo, giữ nguyên người còn lại.
- HTML bảng khoa/tổ có cột sĩ số và tên mở dialog; HTML hồ sơ có đúng CCCD, SĐT, tên, chức vụ, email, giới tính, khoa và tổ từ bảng cán bộ.

## Tệp đã thay đổi

- Models/DuLieuQuanLy.cs
- Services/KhoDuLieuDemo.cs
- Services/DichVuQuanLyDuLieuDemo.cs
- Services/KiemTraTrungHoSo.cs (mới)
- Components/Layout/BieuMauCanBo.razor
- Components/Layout/TenCanBo.razor
- Components/Layout/TenCanBo.razor.css
- wwwroot/ho-so-noi.js
- Components/Layout/HoSoCanBo.razor (mới)
- Components/Layout/HoSoCanBo.razor.css (mới)
- Components/Pages/Khoa.razor
- Components/Pages/ToChuyenMon.razor
- Components/Pages/CanBoGiangVien.razor
- wwwroot/app.css
- KiemThu/Program.cs
- KiemThu/KiemThuHoSoVaSiSo.cs (mới)
- Nhom2-BTL.csproj: loại thư mục tạm .tmp khỏi các tệp đầu vào/đóng gói để kết quả build kiểm thử không bị sao chép lồng nhau.

## Tự xem

1. Dừng và chạy lại ứng dụng để tạo kho mẫu mới với hai phó khoa; tải CSS bằng Ctrl + F5.
2. Vào Khoa: xem hai tên phó khoa, rê chuột khoảng 250 ms lên tên lãnh đạo; thẻ hồ sơ nổi xuất hiện cạnh tên và tự ẩn sau khi rời khỏi tên/thẻ. Có thể bấm tên hoặc dùng Tab rồi Enter để mở hồ sơ.
3. Vào Tổ chuyên môn và kiểm tra tương tự với tổ trưởng/tổ phó.
4. Dùng tài khoản demo quản trị để thêm cán bộ: bắt buộc nhập CCCD và SĐT riêng. Thông báo lỗi hiển thị ngay trong biểu mẫu.
5. Trong thư mục dự án chạy `dotnet run --project KiemThu/NghiepVu.csproj --no-restore` để kiểm thử lại trên kho riêng.

Dữ liệu và phân quyền chỉ để minh họa. Khởi động lại ứng dụng sẽ đặt lại hồ sơ. Khoa mới hoặc vị trí lãnh đạo bị xóa sẽ để trống để quản trị viên phân công, không tự chọn người thay thế. Không có cơ sở dữ liệu.

## Cập nhật thẻ hồ sơ nổi và căn giữa cột

- Các cột Số tổ, Sĩ số cán bộ, Trạng thái của Khoa, sĩ số Tổ chuyên môn và Lớp đào tạo đều căn giữa tiêu đề/nội dung bằng TextAlign.Center.
- Tên lãnh đạo mở thẻ hồ sơ bên cạnh sau 250 ms, rời khỏi tên và thẻ sẽ tự đóng sau 150 ms. Có thể đi chuột vào thẻ để đọc/cuộn, dùng Tab/Enter để mở và Escape để đóng.
- Không có nền tối, không cần bấm X. Thẻ tự đổi bên và giới hạn trong khung nhìn, không bị vùng cuộn của bảng cắt mất.
- 216 kiểm tra tự động tiếp tục đạt. Đã xác minh trên trình duyệt: mở/ẩn khi rê chuột ở Khoa và Tổ chuyên môn, căn giữa tiêu đề/nội dung của đủ 5 cột yêu cầu.
