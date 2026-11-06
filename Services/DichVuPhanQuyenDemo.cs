using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Người dùng của phiên hiện tại, rút từ claim. Mỗi tài khoản một vai trò:
/// Admin; Manager với phạm vi Khoa / DaoTao / Lop (ba manager); GiangVien (phạm vi = mã cán bộ);
/// HocVien (phạm vi = mã học viên); Guest (tài khoản tự đăng ký, chưa được cấp vai trò).
/// </summary>
public sealed record NguoiDungHienTai(bool DaDangNhap, string Ten, string VaiTro, string PhamVi)
{
    public static readonly NguoiDungHienTai Khach = new(false, "", "", "");
    public bool LaAdmin => DaDangNhap && VaiTro == "Admin";
    public bool LaQuanLy(string vung) => DaDangNhap && VaiTro == "Manager" && PhamVi == vung;
    public bool LaGiangVien => DaDangNhap && VaiTro == "GiangVien" && PhamVi != "";
    public bool LaHocVien => DaDangNhap && VaiTro == "HocVien" && PhamVi != "";
    public string? MaCanBo => LaGiangVien ? PhamVi : null;
    public string? MaHocVien => LaHocVien ? PhamVi : null;
    /// <summary>Admin hoặc manager đúng vùng (Khoa, DaoTao, Lop).</summary>
    public bool CoQuyenVung(string vung) => LaAdmin || LaQuanLy(vung);
    public string TenVaiTro => TenHienThi(VaiTro, PhamVi);

    public static string TenHienThi(string vaiTro, string phamVi) => vaiTro switch
    {
        "Admin" => "Quản trị viên",
        "Manager" => phamVi switch { "Khoa" => "Quản lý khoa", "DaoTao" => "Quản lý đào tạo", "Lop" => "Quản lý học viên", _ => "Cán bộ quản lý" },
        "GiangVien" => "Giảng viên",
        "HocVien" => "Học viên",
        "Guest" => "Người xem",
        _ => "Chưa xác định"
    };

    public static NguoiDungHienTai Tu(ClaimsPrincipal u)
        => u.Identity?.IsAuthenticated == true
            ? new(true, u.Identity.Name ?? "", u.FindFirst(ClaimTypes.Role)?.Value ?? "", u.FindFirst("PhamVi")?.Value ?? "")
            : Khach;
}

/// <summary>Kiểm tra quyền tại một nơi dựa trên claim của phiên đăng nhập hiện tại.</summary>
public sealed class DichVuPhanQuyenDemo(AuthenticationStateProvider nhaCungCap, IAuthorizationService uyQuyen)
{
    public async Task<bool> CoQuyenAdminAsync()
    {
        var trangThai = await nhaCungCap.GetAuthenticationStateAsync();
        return (await uyQuyen.AuthorizeAsync(trangThai.User, null, nameof(VaiTroDemo.Admin))).Succeeded;
    }

    public async Task<NguoiDungHienTai> LayNguoiDungAsync()
        => NguoiDungHienTai.Tu((await nhaCungCap.GetAuthenticationStateAsync()).User);

    /// <summary>Luồng đọc đồng bộ: nếu danh tính chưa sẵn sàng thì coi như khách (mặc định từ chối).</summary>
    public NguoiDungHienTai LayNguoiDung()
    {
        var task = nhaCungCap.GetAuthenticationStateAsync();
        return task.IsCompletedSuccessfully ? NguoiDungHienTai.Tu(task.Result.User) : NguoiDungHienTai.Khach;
    }

    public async Task<bool> CoQuyenVungAsync(string vung) => (await LayNguoiDungAsync()).CoQuyenVung(vung);

    public bool DuocXemThongTinMat(string vung) => LayNguoiDung().CoQuyenVung(vung);
}
