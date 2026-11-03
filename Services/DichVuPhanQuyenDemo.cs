using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Kiểm tra quyền tại một nơi dựa trên claim của phiên đăng nhập hiện tại.</summary>
public sealed class DichVuPhanQuyenDemo(AuthenticationStateProvider nhaCungCap, IAuthorizationService uyQuyen)
{
    public async Task<bool> CoQuyenAdminAsync()
    {
        var trangThai = await nhaCungCap.GetAuthenticationStateAsync();
        return (await uyQuyen.AuthorizeAsync(trangThai.User, null, nameof(VaiTroDemo.Admin))).Succeeded;
    }
    public async Task<bool> CoQuyenVungAsync(string vung)
    {
        var u = (await nhaCungCap.GetAuthenticationStateAsync()).User;
        return u.Identity?.IsAuthenticated == true && (u.IsInRole("Admin") || (u.IsInRole("Manager") && u.FindFirst("PhamVi")?.Value == vung));
    }
    public bool DuocXemThongTinMat(string vung)
    {
        // Luồng đọc đồng bộ mặc định che dữ liệu nếu danh tính chưa sẵn sàng.
        var task = nhaCungCap.GetAuthenticationStateAsync();
        if (!task.IsCompletedSuccessfully) return false;
        var u = task.Result.User;
        return u.Identity?.IsAuthenticated == true && (u.IsInRole("Admin") || (u.IsInRole("Manager") && u.FindFirst("PhamVi")?.Value == vung));
    }}


