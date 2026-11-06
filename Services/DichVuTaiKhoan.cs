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
    // ---- Hàng rào 6: cấp tài khoản chỉ đi xuống ----
    // Admin quản lý mọi vai trò; Quản lý đào tạo chỉ quản lý tài khoản Giảng viên; Quản lý học viên chỉ quản lý tài khoản Học viên.
    // Không ai cấp được vai trò ngang hoặc cao hơn mình.
    public static IReadOnlyList<string> VaiTroDuocQuanLy(ClaimsPrincipal actor)
    {
        if (actor.IsInRole("Admin")) return ["Admin", "Manager", "GiangVien", "HocVien", "Guest"];
        if (actor.IsInRole("Manager") && actor.FindFirstValue("PhamVi") == "DaoTao") return ["GiangVien"];
        if (actor.IsInRole("Manager") && actor.FindFirstValue("PhamVi") == "Lop") return ["HocVien"];
        return [];
    }
    private static bool DuocQuanLy(ClaimsPrincipal actor, string vaiTro) => VaiTroDuocQuanLy(actor).Contains(vaiTro);

    public async Task<List<TaiKhoan>> DanhSach(ClaimsPrincipal actor)
    {
        var duoc = VaiTroDuocQuanLy(actor);
        if (!HopLe(actor) || duoc.Count == 0) throw new UnauthorizedAccessException();
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<CoSoTaiKhoan>().Users.AsNoTracking().Where(x => duoc.Contains(x.VaiTro)).OrderBy(x => x.UserName).Select(x => new TaiKhoan { Id = x.Id, UserName = x.UserName, VaiTro = x.VaiTro, PhamVi = x.PhamVi, BiKhoa = x.BiKhoa, SecurityStamp = null, ConcurrencyStamp = null, PasswordHash = null }).ToListAsync();
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
            u.VaiTro = vaiTro; u.BiKhoa = khoa; u.PhamVi = PhamViLuu(vaiTro, phamVi);
            u.SecurityStamp = Guid.NewGuid().ToString();
            return (await users.UpdateAsync(u)).Succeeded ? "Đã lưu. Các phiên cũ của tài khoản không còn quyền truy cập." : "Không lưu được. Hãy tải lại danh sách.";
        } finally { cong.Release(); }
    }
    // Manager phải có phạm vi Khoa/DaoTao/Lop; Giảng viên gắn mã cán bộ có thật; Học viên gắn mã học viên có thật.
    private bool VaiTroHopLe(string vaiTro, string phamVi)
    {
        if (vaiTro is "Admin" or "Guest") return true;
        if (vaiTro == "Manager") return phamVi is "Khoa" or "DaoTao" or "Lop";
        using var scope = scopes.CreateScope();
        var kho = scope.ServiceProvider.GetService<KhoDuLieuDemo>();
        if (kho is null) return false;
        lock (kho.KhoaDongBo)
            return vaiTro switch
            {
                "GiangVien" => kho.DanhSachCanBo.Any(c => c.MaCanBo == PhamViLuu(vaiTro, phamVi)),
                "HocVien" => kho.DanhSachHocVien.Any(h => h.MaHocVien == PhamViLuu(vaiTro, phamVi)),
                _ => false
            };
    }
    private static string PhamViLuu(string vaiTro, string phamVi)
        => vaiTro == "Manager" ? phamVi.Trim() : vaiTro is "GiangVien" or "HocVien" ? phamVi.Trim().ToUpperInvariant() : "";

    public async Task<string> LuuTaiKhoan(ClaimsPrincipal actor, string? id, string ten, string matKhau, string vaiTro, string phamVi, bool khoa)
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor) || !DuocQuanLy(actor, vaiTro)) return "Bạn không có quyền cấp vai trò này.";
            if (string.IsNullOrWhiteSpace(ten) || ten.Length > 64) return "Tên đăng nhập không hợp lệ (1–64 ký tự).";
            if (!VaiTroHopLe(vaiTro, phamVi)) return vaiTro switch
            {
                "Manager" => "Mảng quản lý không hợp lệ. Hãy chọn Khoa, Đào tạo hoặc Học viên.",
                "GiangVien" => "Mã cán bộ không tồn tại. Tài khoản giảng viên phải gắn với một cán bộ có trong danh sách.",
                "HocVien" => "Mã học viên không tồn tại. Tài khoản học viên phải gắn với một học viên có trong danh sách.",
                _ => "Vai trò không hợp lệ."
            };
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            if (id is null) {
                if (KhoiTaoTaiKhoanMau.LaTenDanhRieng(ten)) return "Tên dành riêng cho tài khoản mẫu.";
                var moi = new TaiKhoan { UserName = ten.Trim(), VaiTro = vaiTro, PhamVi = PhamViLuu(vaiTro, phamVi), BiKhoa = khoa, LockoutEnabled = true };
                var tao = await users.CreateAsync(moi, matKhau);
                return tao.Succeeded ? "Đã tạo tài khoản." : "Không tạo được. Kiểm tra tên đăng nhập hợp lệ, không trùng và mật khẩu có cả chữ và số.";
            }
            var u = await users.FindByIdAsync(id);
            if (u is null) return "Tài khoản không tồn tại.";
            if (!DuocQuanLy(actor, u.VaiTro)) return "Bạn không có quyền sửa tài khoản này.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Không sửa tài khoản kiểm tra cố định.";
            if (id == actor.FindFirstValue(ClaimTypes.NameIdentifier)) return "Không tự sửa tài khoản đang đăng nhập.";
            if (KhoiTaoTaiKhoanMau.LaTenDanhRieng(ten)) return "Tên dành riêng cho tài khoản mẫu.";
            if (matKhau.Length > 0) {
                foreach (var validator in users.PasswordValidators)
                    if (!(await validator.ValidateAsync(users, u, matKhau)).Succeeded) return "Mật khẩu phải có cả chữ và số.";
                u.PasswordHash = users.PasswordHasher.HashPassword(u, matKhau);
            }
            u.UserName = ten.Trim(); u.VaiTro = vaiTro; u.PhamVi = PhamViLuu(vaiTro, phamVi); u.BiKhoa = khoa;
            u.SecurityStamp = Guid.NewGuid().ToString();
            return (await users.UpdateAsync(u)).Succeeded ? "Đã cập nhật. Phiên cũ đã bị vô hiệu hóa." : "Không cập nhật được. Kiểm tra tên trùng và tải lại danh sách.";
        } finally { cong.Release(); }
    }
    /// <summary>Người dùng tự đổi mật khẩu: phải nhập đúng mật khẩu hiện tại; đổi xong mọi phiên cũ hết hiệu lực.</summary>
    public async Task<string> DoiMatKhau(ClaimsPrincipal actor, string matKhauCu, string matKhauMoi)
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor)) return "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.";
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            var u = await users.FindByIdAsync(actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");
            if (u is null) return "Tài khoản không tồn tại.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Tài khoản mẫu dùng chung mật khẩu demo123, không đổi được.";
            if (!await users.CheckPasswordAsync(u, matKhauCu)) { await users.AccessFailedAsync(u); return "Mật khẩu hiện tại chưa đúng."; }
            if (matKhauMoi == matKhauCu) return "Mật khẩu mới phải khác mật khẩu hiện tại.";
            foreach (var validator in users.PasswordValidators)
                if (!(await validator.ValidateAsync(users, u, matKhauMoi)).Succeeded) return "Mật khẩu mới chưa đạt yêu cầu: có cả chữ và số.";
            u.PasswordHash = users.PasswordHasher.HashPassword(u, matKhauMoi);
            u.SecurityStamp = Guid.NewGuid().ToString();
            return (await users.UpdateAsync(u)).Succeeded ? "Đã đổi mật khẩu. Vui lòng đăng nhập lại bằng mật khẩu mới." : "Không đổi được mật khẩu. Vui lòng thử lại.";
        } finally { cong.Release(); }
    }

    public async Task<string> Xoa(ClaimsPrincipal actor, string id)
    {
        await cong.WaitAsync();
        try {
            if (!HopLe(actor) || VaiTroDuocQuanLy(actor).Count == 0) return "Bạn không có quyền thực hiện.";
            if (id == actor.FindFirstValue(ClaimTypes.NameIdentifier)) return "Không tự xóa tài khoản đang đăng nhập.";
            using var scope = scopes.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
            var u = await users.FindByIdAsync(id);
            if (u is null) return "Tài khoản không tồn tại.";
            if (!DuocQuanLy(actor, u.VaiTro)) return "Bạn không có quyền xoá tài khoản này.";
            if (KhoiTaoTaiKhoanMau.LaTaiKhoanMau(u)) return "Không xóa tài khoản kiểm tra cố định.";
            return (await users.DeleteAsync(u)).Succeeded ? "Đã xóa tài khoản và vô hiệu hóa phiên cũ." : "Không xóa được tài khoản.";
        } finally { cong.Release(); }
    }}




