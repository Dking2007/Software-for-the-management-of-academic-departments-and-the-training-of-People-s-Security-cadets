using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Nhom2_BTL.Services;

public sealed class TaiKhoan : IdentityUser
{
    public string VaiTro { get; set; } = "Guest";
    public string PhamVi { get; set; } = "";
    public bool BiKhoa { get; set; }
}
public sealed class CoSoTaiKhoan(DbContextOptions<CoSoTaiKhoan> options) : IdentityDbContext<TaiKhoan>(options);

// Mỗi thao tác mở scope riêng để DbContext không bị chia sẻ giữa các sự kiện Blazor.
public sealed class DichVuTaiKhoan(IServiceScopeFactory scopes, IHostEnvironment? moiTruong = null)
{
    private readonly SemaphoreSlim cong = new(1, 1);
    public bool HopLe(ClaimsPrincipal principal)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoSoTaiKhoan>();
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var u = db.Users.AsNoTracking().SingleOrDefault(x => x.Id == id);
        return u is not null && DuocDung(u) && !u.BiKhoa && u.SecurityStamp == principal.FindFirstValue("DauBaoMat") && u.VaiTro == principal.FindFirstValue(ClaimTypes.Role) && u.PhamVi == (principal.FindFirstValue("PhamVi") ?? "");
    }
    private bool DuocDung(TaiKhoan u)
        => !KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u) || moiTruong?.IsDevelopment() == true;
    public async Task<TaiKhoan?> DangNhap(string ten, string matKhau)
    {
        await cong.WaitAsync();
        try {
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            var u = await users.FindByNameAsync(ten.Trim());
            if (u is null || !DuocDung(u) || u.BiKhoa || await users.IsLockedOutAsync(u)) return null;
            if (!await users.CheckPasswordAsync(u, matKhau)) { await users.AccessFailedAsync(u); return null; }
            await users.ResetAccessFailedCountAsync(u);
            return u;
        } finally { cong.Release(); }
    }
    public async Task<bool> DangKy(string ten, string matKhau)
    {
        if (ten.Length > 64 || KhoiTaoTaiKhoanMau.LaTenDanhRieng(ten)) return false;
        using var scope = scopes.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
        return (await users.CreateAsync(new TaiKhoan { UserName = ten.Trim(), LockoutEnabled = true }, matKhau)).Succeeded;
    }
    public async Task<List<TaiKhoan>> DanhSach(ClaimsPrincipal actor)
    {
        if (!HopLe(actor) || !actor.IsInRole("Admin")) throw new UnauthorizedAccessException();
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CoSoTaiKhoan>().Users.AsNoTracking().OrderBy(x => x.UserName).Select(x => new TaiKhoan { Id = x.Id, UserName = x.UserName, VaiTro = x.VaiTro, PhamVi = x.PhamVi, BiKhoa = x.BiKhoa, SecurityStamp = null, ConcurrencyStamp = null, PasswordHash = null }).ToListAsync();
    }
    public async Task<string> CapNhat(ClaimsPrincipal actor, string id, string vaiTro, bool khoa, string phamVi = "")
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor) || !actor.IsInRole("Admin")) return "Bạn không có quyền thực hiện.";
            if (!VaiTroHopLe(vaiTro, phamVi)) return "Vai trò không hợp lệ.";
            if (id == actor.FindFirstValue(ClaimTypes.NameIdentifier)) return "Không được tự khóa hoặc thay đổi vai trò của mình.";
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            var u = await users.FindByIdAsync(id);
            if (u is null) return "Tài khoản không tồn tại.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Tài khoản kiểm tra cố định không được đổi vai trò hoặc khóa.";
            u.VaiTro = vaiTro; u.BiKhoa = khoa; u.PhamVi = vaiTro == "Manager" ? phamVi : "";
            u.SecurityStamp = Guid.NewGuid().ToString();
            return (await users.UpdateAsync(u)).Succeeded ? "Đã lưu. Các phiên cũ của tài khoản không còn quyền truy cập." : "Không lưu được. Hãy tải lại danh sách.";
        } finally { cong.Release(); }
    }
    private static bool VaiTroHopLe(string vaiTro, string phamVi)
        => vaiTro is "Admin" or "Guest" || (vaiTro == "Manager" && phamVi is "Khoa" or "Lop");

    public async Task<string> LuuTaiKhoan(ClaimsPrincipal actor, string? id, string ten, string matKhau, string vaiTro, string phamVi, bool khoa)
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor) || !actor.IsInRole("Admin")) return "Bạn không có quyền thực hiện.";
            if (!VaiTroHopLe(vaiTro, phamVi) || string.IsNullOrWhiteSpace(ten) || ten.Length > 64) return "Thông tin tài khoản không hợp lệ.";
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            if (id is null) {
                if (KhoiTaoTaiKhoanMau.LaTenDanhRieng(ten)) return "Tên dành riêng cho tài khoản mẫu.";
                var moi = new TaiKhoan { UserName = ten.Trim(), VaiTro = vaiTro, PhamVi = vaiTro == "Manager" ? phamVi : "", BiKhoa = khoa, LockoutEnabled = true };
                var tao = await users.CreateAsync(moi, matKhau);
                return tao.Succeeded ? "Đã tạo tài khoản." : "Không tạo được. Kiểm tra tên đăng nhập hợp lệ, không trùng và mật khẩu có cả chữ và số.";
            }
            var u = await users.FindByIdAsync(id);
            if (u is null) return "Tài khoản không tồn tại.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Không sửa tài khoản kiểm tra cố định.";
            if (id == actor.FindFirstValue(ClaimTypes.NameIdentifier)) return "Không tự sửa tài khoản đang đăng nhập.";
            if (KhoiTaoTaiKhoanMau.LaTenDanhRieng(ten)) return "Tên dành riêng cho tài khoản mẫu.";
            if (matKhau.Length > 0) {
                foreach (var validator in users.PasswordValidators)
                    if (!(await validator.ValidateAsync(users, u, matKhau)).Succeeded) return "Mật khẩu phải có cả chữ và số.";
                u.PasswordHash = users.PasswordHasher.HashPassword(u, matKhau);
            }
            u.UserName = ten.Trim(); u.VaiTro = vaiTro; u.PhamVi = vaiTro == "Manager" ? phamVi : ""; u.BiKhoa = khoa;
            u.SecurityStamp = Guid.NewGuid().ToString();
            return (await users.UpdateAsync(u)).Succeeded ? "Đã cập nhật. Phiên cũ đã bị vô hiệu hóa." : "Không cập nhật được. Kiểm tra tên trùng và tải lại danh sách.";
        } finally { cong.Release(); }
    }
    public async Task<string> Xoa(ClaimsPrincipal actor, string id)
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor) || !actor.IsInRole("Admin")) return "Bạn không có quyền thực hiện.";
            if (id == actor.FindFirstValue(ClaimTypes.NameIdentifier)) return "Không tự xóa tài khoản đang đăng nhập.";
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            var u = await users.FindByIdAsync(id);
            if (u is null) return "Tài khoản không tồn tại.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Không xóa tài khoản kiểm tra cố định.";
            return (await users.DeleteAsync(u)).Succeeded ? "Đã xóa tài khoản và vô hiệu hóa phiên cũ." : "Không xóa được tài khoản.";
        } finally { cong.Release(); }
    }}




