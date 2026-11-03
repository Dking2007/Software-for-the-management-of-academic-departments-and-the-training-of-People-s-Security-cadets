# Bàn giao Giai đoạn 7–8 — 29/10/2026

## Đã triển khai

- Đọc tài liệu Word tại `D:\MinhChau\SƠ ĐỒ CẤU TRÚC CỦA TỪNG ĐỐI TƯỢNG QUẢN LÝ.docx` và AGENTS.md trước khi sửa nghiệp vụ.
- Giữ hai nhánh độc lập: khoa → tổ → cán bộ; hệ → khóa → lớp → học viên. Không thêm quan hệ khoa–lớp/học viên.
- Mã định danh ổn định; kiểm tra tham chiếu, trùng mã/CCCD, định dạng điện thoại/email, ngày sinh và quyền Admin tại service. Từ chối xóa đơn vị còn dữ liệu phụ thuộc.
- Sĩ số và thống kê tính từ dữ liệu nguồn, không nhập tay sĩ số. Không giới hạn lớp ở 40 học viên hoặc Văn bằng 2 ở đúng 4 lớp.
- Kiểm tra chuyên ngành Chính quy theo số lớp; chức vụ học viên không vượt cơ cấu. Chuyển/xóa lãnh đạo cập nhật tham chiếu, không tự phân công người thay thế.
- Tên tiếng Việt đa dạng, sinh giới tính trước tên. Không tự sửa giới tính hồ sơ người dùng nhập.
- Nhãn Mới trong 15 phút, Vừa cập nhật trong 2 phút; hết nhãn không xóa hồ sơ. Sau thêm, lọc theo mã mới để thấy ngay; dùng Bỏ bộ lọc để xem lại toàn bộ.
- Bộ lọc khoa/tổ kết hợp, lọc lớp, tìm kiếm, sắp xếp tên theo tiếng Việt, STT theo View của bảng Radzen. Form giữ bản nháp khi lưu thất bại và chặn bấm lưu lặp.
- Trang chủ có tiêu đề, 5 thống kê hiện hành và 3 thẻ mở phân hệ. Sidebar navy, giữ logo hiện có; mục lớn mở tab mới, mục con ở tab hiện tại. Sửa CSS mở sidebar trên màn hình nhỏ.
- Thông báo cập nhật dữ liệu giữa các tab; thu hồi vé mở tab đã cấp khi đăng xuất. Làm mới liên kết định kỳ để tránh vé hết hạn khi để trang mở lâu.

## Tệp chính

- `Models/DuLieuQuanLy.cs`: mô hình, kiểm tra định dạng, thời điểm tạo/cập nhật và kết quả thao tác.
- `Services/QuyTacDuLieu.cs`, `KhoDuLieuDemo.cs`, `DichVuQuanLyDuLieuDemo.cs`: danh mục, khởi tạo, CRUD, quan hệ và thống kê.
- `Services/KhoVeDangNhapDemo.cs`, `NhaCungCapTrangThaiXacThucDemo.cs`: vòng đời vé mở tab.
- `Components/Pages/`: trang chủ và các bảng quản lý.
- `Components/Layout/BieuMau*.razor`, `NhanBanGhi.razor`, `ChiTietHoSo.razor`, `NavMenu.razor`: form, nhãn, chi tiết và điều hướng.
- `Components/App.razor`, `Program.cs`, `wwwroot/app.css`: Radzen theme, văn hóa tiếng Việt, bố cục.
- `KiemThu/`: chương trình kiểm thử độc lập, không gọi vào dữ liệu phiên web đang chạy.

## Kết quả thực sự đã kiểm tra

- Build web thành công: 0 lỗi, 0 cảnh báo. Log: `KiemThu/build.txt`.
- 110 kiểm tra tự động đạt. Log: `KiemThu/ket-qua.txt`. Bao gồm CRUD học viên/cán bộ, chuyển lớp khác khóa/hệ, chuyển tổ khác khoa, thống kê độc lập, chức vụ, lãnh đạo, dữ liệu trùng/mồ côi, từ chối xóa dây chuyền, quyền Viewer và vé đăng nhập.
- Có kiểm tra dựng HTML phía máy chủ bằng HtmlRenderer: bảng theo quyền, form đúng nhánh, nhãn Mới, trang chủ, STT trang đầu 1–20. Đây không phải kiểm thử tương tác trình duyệt.
- 12 đường dẫn HTTP/tài nguyên trả 200 trên cổng 5092, gồm các route, logo, Radzen CSS/JS, Blazor và app.css. Log: `KiemThu/http.json`. HTTP 200 không chứng minh tương tác đã hoạt động.
- Restore dự án kiểm thử từng có cảnh báo NU1900 vì không truy cập được nguồn kiểm tra lỗ hổng NuGet; không nâng gói hoặc đổi nền tảng. Build web cuối không có cảnh báo.

## Chưa kiểm chứng và giới hạn

- Công cụ Chrome bị chặn tự động vì không xác định chắc chắn URL cửa sổ Windows. Chưa kiểm tra trực tiếp bản cuối: thao tác form, thông báo, bộ lọc/sắp xếp sau click, điều hướng nhiều tab, responsive desktop/tablet/điện thoại. Chưa mở bổ sung các tệp trong Visual Studio bằng UI trong lượt này.
- Dữ liệu dùng chung có chủ đích giữa các phiên trong cùng tiến trình singleton; trạng thái đăng nhập tách theo phiên Blazor. Chạy hai tiến trình có hai kho khác nhau. Khởi động lại mất các thay đổi trong bộ nhớ; không có lưu bền vững hoặc cơ sở dữ liệu.
- Đăng nhập chỉ minh họa. Đăng xuất xóa danh tính tab hiện tại và thu hồi vé đã cấp; các tab đã đăng nhập trước đó vẫn có phiên riêng. Tải lại toàn trang có thể yêu cầu đăng nhập lại.
- Toàn bộ hồ sơ sinh tự động là dữ liệu tổng hợp. CCCD/điện thoại chỉ kiểm tra định dạng, không xác minh người sở hữu hay việc cấp số. Email dùng miền example.invalid. Địa chỉ đường Số N là cấu trúc tổng hợp, không xác nhận địa chỉ thực và không ghép xã/phường chưa xác minh.
- Danh mục 34 tỉnh/thành đối chiếu nguồn Chính phủ: https://baochinhphu.vn/bang-danh-muc-va-ma-so-cua-34-tinh-thanh-moi-3321-don-vi-hanh-chinh-cap-xa-moi-102250704153652947.htm .
- Hai tổ mỗi khoa và phân công lãnh đạo khoa vào tổ đầu tiên là cấu hình dữ liệu minh họa; không khẳng định cơ cấu nhân sự thực tế.

## Chạy và kiểm tra

Bản kiểm tra đã khởi động tại http://127.0.0.1:5092. Không dừng phiên Visual Studio đang chạy của người dùng. Tài khoản cục bộ: `quantri / demo123`, `xem / demo123`.

Trong Visual Studio 2026, mở `Nhom2-BTL.slnx`, chọn dự án web và chạy F5/Ctrl+F5. Chỉ dừng phiên cũ khi đã chấp nhận mất dữ liệu đang nằm trong bộ nhớ của phiên đó. Nếu DLL bị khóa do phiên cũ, có thể build riêng từ thư mục dự án:

```powershell
dotnet build --no-restore -p:OutputPath=bin\GiaiDoan78\ -p:UseAppHost=false
dotnet run --project KiemThu\NghiepVu.csproj -p:UseAppHost=false
```

Lệnh đầu build ra thư mục riêng; lệnh sau chạy kho kiểm thử độc lập. Dừng bản QA 5092 trước khi rebuild cùng thư mục nếu DLL bị khóa. Cần kiểm tra trực tiếp trên Chrome các mục chưa kiểm chứng nêu trên trước khi xác nhận hoàn tất nghiệm thu giao diện.
