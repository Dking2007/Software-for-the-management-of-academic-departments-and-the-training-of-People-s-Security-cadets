namespace Nhom2_BTL.Models;

/// <summary>Vai trò dùng trong luồng đăng nhập minh họa.</summary>
public enum VaiTroDemo
{
    Admin,
    Manager,
    GiangVien,
    HocVien,
    Guest
}

/// <summary>Thông tin hiển thị của tài khoản thử nghiệm; không phải hồ sơ người thật.</summary>
public sealed record NguoiDungDemo(string TenDangNhap, string HoTen, VaiTroDemo VaiTro);



