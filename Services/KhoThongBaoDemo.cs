using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

// Dữ liệu tổng hợp, chia sẻ giữa các phiên và chỉ tồn tại trong tiến trình hiện tại.
public sealed class KhoThongBaoDemo
{
    internal object Khoa { get; } = new();
    internal List<ThongBao> DanhSach { get; } = Enumerable.Range(0, 3).Select(i => new ThongBao
    {
        Ma = Guid.NewGuid(),
        TieuDe = new[] { "Thông báo sử dụng cổng thông tin đào tạo", "Hướng dẫn tra cứu thông tin khoa và lớp", "Lưu ý khi sử dụng dữ liệu minh họa" }[i],
        NoiDung = new[] {
            "Cổng thông tin hỗ trợ tra cứu khoa, lớp đào tạo và học viên. Người dùng lựa chọn một trong ba phân hệ bên dưới để bắt đầu.\n\nBảng thông báo giúp cập nhật nội dung chung và các nội dung hướng tới từng khoa, hệ đào tạo hoặc lớp cụ thể.",
            "Sử dụng ô tìm kiếm và các bộ lọc trong từng phân hệ để tìm thông tin cần thiết. Các tag trên bảng thông báo cho biết đối tượng được hướng tới.\n\nĐây là nội dung tổng hợp phục vụ thực hành, không phải thông báo nghiệp vụ thật.",
            "Các hồ sơ trong ứng dụng là dữ liệu tổng hợp, không đại diện người thật. Đăng nhập và phân quyền chỉ phục vụ minh họa học tập.\n\nThông báo và hình ảnh được lưu trong bộ nhớ, sẽ được đặt lại khi ứng dụng khởi động lại."
        }[i],
        TacGia = "Quản trị viên (mẫu)",
        DangLuc = new DateTimeOffset(2026, 11, 3 - i, 8, 0, 0, TimeSpan.FromHours(7))
    }).ToList();
    public event Action? DaThayDoi;
    internal void ThongBaoThayDoi() => DaThayDoi?.Invoke();
}
