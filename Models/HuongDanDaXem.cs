namespace Nhom2_BTL.Models;

/// <summary>
/// Đánh dấu một tài khoản đã xem (hoặc đã chủ động bỏ qua) một phần hướng dẫn nhập môn.
/// Lưu ở máy chủ theo tài khoản, nên đổi máy, đổi trình duyệt hay xoá dữ liệu trình duyệt cũng không hiện lại.
/// Khoa: "chung" = giới thiệu chung; "trang:dao-tao/lich" = hướng dẫn riêng của một trang.
/// Không phải dữ liệu nghiệp vụ, nên không ghi vào nhật ký thao tác.
/// </summary>
public sealed class HuongDanDaXem
{
    public string TenDangNhap { get; set; } = "";
    public string Khoa { get; set; } = "";
    public DateTimeOffset Luc { get; set; }
}
