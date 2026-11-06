using Microsoft.AspNetCore.Identity;

namespace Nhom2_BTL.Services;

// Thông tin giả công khai chỉ để kiểm tra cục bộ trong Development.
public static class KhoiTaoTaiKhoanMau
{
    public static bool LaTaiKhoanMau(TaiKhoan u)
        => u.Id is "demo-co-dinh-admin" or "demo-co-dinh-guest" or "demo-manager-khoa" or "demo-manager-lop"
            or "demo-manager-daotao" or "demo-giang-vien" or "demo-hoc-vien";

    public static bool LaTenDanhRieng(string ten)
        => ten.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("nguoixem", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("quanlykhoa", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("quanlyhocvien", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("quanlydaotao", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("giangvien", StringComparison.OrdinalIgnoreCase)
        || ten.Trim().Equals("hocvien", StringComparison.OrdinalIgnoreCase);

    /// <param name="maGiangVien">Mã cán bộ gắn với tài khoản mẫu "giangvien".</param>
    /// <param name="maHocVien">Mã học viên gắn với tài khoản mẫu "hocvien".</param>
    public static async Task TaoAsync(UserManager<TaiKhoan> users, string maGiangVien = KhoiTaoDaoTaoMau.MaGiangVienMau, string maHocVien = "HV00030")
    {
        // Bỏ tài khoản "hocvien4" của bản trước: mỗi học viên chỉ một tài khoản (tài khoản "hocvien" đã có đủ các năm học).
        if (await users.FindByIdAsync("demo-hoc-vien-4") is { } cu) await users.DeleteAsync(cu);
        foreach (var (id, ten, vaiTro, phamVi) in new[] {
            ("demo-co-dinh-admin", "admin", "Admin", ""),
            ("demo-co-dinh-guest", "nguoixem", "Guest", ""),
            ("demo-manager-khoa", "quanlykhoa", "Manager", "Khoa"),
            ("demo-manager-lop", "quanlyhocvien", "Manager", "Lop"),
            ("demo-manager-daotao", "quanlydaotao", "Manager", "DaoTao"),
            ("demo-giang-vien", "giangvien", "GiangVien", maGiangVien),
            ("demo-hoc-vien", "hocvien", "HocVien", maHocVien) })
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
                // Tài khoản mẫu (đúng mã cố định) đổi hồ sơ gắn kèm khi dữ liệu mẫu đổi; phiên cũ mất hiệu lực.
                if (coSan.PhamVi != phamVi && vaiTro is "GiangVien" or "HocVien")
                {
                    coSan.PhamVi = phamVi;
                    await users.UpdateAsync(coSan);
                    await users.UpdateSecurityStampAsync(coSan);
                }
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




