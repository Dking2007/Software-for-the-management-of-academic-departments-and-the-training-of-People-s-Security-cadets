# Sửa chậm điều hướng và ẩn mã hồ sơ

Nguyên nhân xác định trong mã: LayDanhSachHocVien/LayDanhSachCanBo gọi DuocXemThongTinMat trong vòng lặp từng bản ghi. Hàm này gọi AuthenticationStateProvider, kiểm tra phiên rồi truy vấn SQLite. Việc đó còn nằm trong khóa kho dữ liệu chung nên các trang/phiên khác có thể chờ cùng khóa. URL thay đổi trước khi phần khởi tạo trang hoàn tất phù hợp với hiện tượng người dùng báo; chưa đo trực tiếp Chrome.

Sửa: kiểm tra quyền một lần cho mỗi lượt đọc danh sách, trước khi lấy khóa. Không cache quyền dài hạn; thao tác sau vẫn kiểm tra quyền mới. Tên khoa/tổ khi dựng danh sách cán bộ dùng dictionary trong mỗi lượt đọc, tránh tìm tuyến tính theo từng hồ sơ.

Mã cán bộ/học viên ngoài phạm vi được xóa trong bản sao dịch vụ và ẩn cột trên giao diện. Hồ sơ chi tiết không hiện trường rỗng. Tra cứu cán bộ để mở thẻ dùng khóa nội bộ chỉ trong bộ nhớ máy chủ, không hiển thị hoặc tuần tự hóa. Tìm kiếm mã không khớp với mã đã bị ẩn.

Kiểm tra: build 0 lỗi, 0 cảnh báo; 60 kiểm tra tự động PASS. Phép đo sau warm-up dùng SQLite và 4.240 học viên: tái hiện vòng lặp kiểm tra quyền cũ 1792,71 ms; đọc danh sách mới 0,63 ms. Đây là thời gian ở dịch vụ trong một lần đo, không phải số đo chuyển trang trên trình duyệt. Chưa kiểm thử trực quan Chrome.

Mật khẩu: trang Admin hiện demo123 cho tài khoản mẫu; tài khoản thông thường hiện Không thể xem lại và có thể đặt mật khẩu mới bằng Sửa. Không thêm passcode 123456 để xem tất cả mật khẩu: database chỉ chứa bản băm, không có mật khẩu gốc để mở khóa. Không lưu mật khẩu nguyên văn hoặc thêm cửa hậu phục hồi mật khẩu.
