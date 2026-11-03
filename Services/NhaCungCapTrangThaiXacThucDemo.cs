using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Cầu nối trạng thái Blazor với tài khoản Identity và cookie phía máy chủ.
/// Tên lớp cũ được giữ để tương thích các thành phần hiện có.
/// </summary>
public sealed class NhaCungCapTrangThaiXacThucDemo(KhoVeDangNhapDemo khoVe, KhoPhienDemo? khoPhien = null, DichVuTaiKhoan? taiKhoanDb = null) : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider, IDisposable
{
    // Mỗi phiên Interactive Server giữ một người dùng riêng và mặc định là khách chưa xác thực.
    private ClaimsPrincipal nguoiDungHienTai = TaoNguoiDungChuaDangNhap();
    private static readonly AuthenticationState TrangThaiChuaDangNhap =
        new(TaoNguoiDungChuaDangNhap());

    private Task<AuthenticationState>? trangThaiHttp;
    private bool daDangKy;
    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        trangThaiHttp = authenticationStateTask;
        if (!daDangKy && khoPhien is not null) { khoPhien.PhienThayDoi += KhiPhienDoi; daDangKy = true; }
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (trangThaiHttp is not null) nguoiDungHienTai = (await trangThaiHttp).User;
        if (khoPhien is not null && !khoPhien.ConHieuLuc(nguoiDungHienTai)) return TrangThaiChuaDangNhap;
        return new AuthenticationState(nguoiDungHienTai);
    }
    private void KhiPhienDoi() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    public void Dispose() { if (khoPhien is not null) khoPhien.PhienThayDoi -= KhiPhienDoi; }


    // Identity kiểm tra mật khẩu; claim được lấy từ cơ sở dữ liệu.
    public async Task<bool> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var tenTaiKhoan = tenDangNhap?.Trim() ?? string.Empty;
        var taiKhoan = taiKhoanDb is null ? null : await taiKhoanDb.DangNhap(tenTaiKhoan, matKhau);
        if (taiKhoan is null) return false;

        var cacThongTin = new[]
        {
            new Claim("PhamVi", taiKhoan.PhamVi),
            new Claim("DauBaoMat", taiKhoan.SecurityStamp!),
            new Claim("MaPhien", khoPhien?.TaoPhien() ?? string.Empty),
            new Claim(ClaimTypes.Name, taiKhoan.UserName!),
            new Claim(ClaimTypes.NameIdentifier, taiKhoan.Id),
            new Claim(ClaimTypes.Role, taiKhoan.VaiTro)
        };
        trangThaiHttp = null;
        nguoiDungHienTai = new ClaimsPrincipal(new ClaimsIdentity(cacThongTin, "Identity"));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return true;
    }

    // Tạo vé tab từ danh tính đã xác thực; URL không có quyền tự chọn vai trò.
    public string TaoVeMoTab() => nguoiDungHienTai.Identity?.IsAuthenticated == true ? khoVe.TaoVe(nguoiDungHienTai) : string.Empty;

    // Khôi phục danh tính từ vé máy chủ còn hạn; vé dùng lại được đến khi tự hết hạn.
    public Task<bool> DangNhapBangVeAsync(string maVe)
    {
        var nguoiDung = khoVe.DoiVe(maVe);
        if (nguoiDung is null) return Task.FromResult(false);
        nguoiDungHienTai = nguoiDung;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.FromResult(true);
    }

    // Xóa danh tính hiện tại và thông báo cho các component đang theo dõi trạng thái xác thực.
    public Task DangXuatAsync()
    {
        khoPhien?.ThuHoi(nguoiDungHienTai);
        trangThaiHttp = null;
        khoVe.ThuHoiVe(nguoiDungHienTai);
        nguoiDungHienTai = TrangThaiChuaDangNhap.User;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    // ClaimsIdentity rỗng khiến ASP.NET Core xem phiên này là người chưa đăng nhập.
    private static ClaimsPrincipal TaoNguoiDungChuaDangNhap()
        => new(new ClaimsIdentity());
}






