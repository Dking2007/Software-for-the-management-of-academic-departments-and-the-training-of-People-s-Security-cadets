using Microsoft.AspNetCore.Identity;

namespace Nhom2_BTL.Services;

// Thông tin giả công khai chỉ để kiểm tra cục bộ trong Development.
public static class KhoiTaoTaiKhoanMau
{
    public static bool LaTaiKhoanMau(TaiKhoan u)
        => u.Id is "demo-co-dinh-admin" or "demo-co-dinh-guest" or "demo-manager-khoa" or "demo-manager-lop";

    public static bool LaTenDanhRieng(string ten)
        => ten.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("nguoixem", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("quanlykhoa", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("quanlyhocvien", StringComparison.OrdinalIgnoreCase);

    public static async Task TaoAsync(UserManager<TaiKhoan> users)
    {
        foreach (var (id, ten, vaiTro, phamVi) in new[] {
            ("demo-co-dinh-admin", "admin", "Admin", ""),
            ("demo-co-dinh-guest", "nguoixem", "Guest", ""),
            ("demo-manager-khoa", "quanlykhoa", "Manager", "Khoa"),
            ("demo-manager-lop", "quanlyhocvien", "Manager", "Lop") })
        {
            var theoMa = await users.FindByIdAsync(id);
            if (theoMa is not null && theoMa.UserName != ten)
            {
                // Đổi tên đúng tài khoản mẫu theo mã ổn định; không đụng tài khoản trùng tên.
                var trungTen = await users.FindByNameAsync(ten);
                if (trungTen is not null && trungTen.Id != id)
                    throw new InvalidOperationException($"Tên '{ten}' đã được sử dụng. Không ghi đè tài khoản hiện có.");
                var doiTen = await users.SetUserNameAsync(theoMa, ten);
                if (!doiTen.Succeeded) throw new InvalidOperationException($"Không đổi được tên tài khoản mẫu '{ten}'.");
                await users.UpdateSecurityStampAsync(theoMa);
            }
            var coSan = await users.FindByNameAsync(ten);
            if (coSan is not null)
            {
                // Không nâng quyền hoặc đổi mật khẩu của tài khoản người dùng trùng tên.
                if (coSan.Id != id)
                    throw new InvalidOperationException($"Tên tài khoản mẫu '{ten}' đã được sử dụng. Không ghi đè tài khoản hiện có.");
                continue;
            }

            var u = new TaiKhoan { Id = id, UserName = ten, VaiTro = vaiTro, PhamVi = phamVi, LockoutEnabled = true };
            // Ngoại lệ mật khẩu yếu chỉ dành cho hai tài khoản mẫu; không đổi chính sách đăng ký.
            u.PasswordHash = users.PasswordHasher.HashPassword(u, "demo123");
            var ketQua = await users.CreateAsync(u);
            if (!ketQua.Succeeded)
                throw new InvalidOperationException($"Không tạo được tài khoản mẫu '{ten}'.");
        }
    }
}




