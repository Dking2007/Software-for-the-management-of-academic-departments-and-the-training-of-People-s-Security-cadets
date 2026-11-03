using System.Collections.Concurrent;
using System.Security.Claims;
namespace Nhom2_BTL.Services;

// Kho phiên minh họa trong bộ nhớ; khởi động lại ứng dụng sẽ hủy mọi phiên.
public sealed class KhoPhienDemo(DichVuTaiKhoan? taiKhoan = null)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> cacPhien = new();
    public event Action? PhienThayDoi;
    public string TaoPhien()
    {
        foreach (var muc in cacPhien.Where(x => x.Value <= DateTimeOffset.UtcNow)) cacPhien.TryRemove(muc.Key, out _);
        var ma = Guid.NewGuid().ToString("N");
        cacPhien[ma] = DateTimeOffset.UtcNow.AddHours(8);
        return ma;
    }
    public bool ConHieuLuc(ClaimsPrincipal user) => user.FindFirst("MaPhien")?.Value is string ma && cacPhien.TryGetValue(ma, out var han) && han > DateTimeOffset.UtcNow && (taiKhoan is null || taiKhoan.HopLe(user));
    public void ThuHoi(ClaimsPrincipal user)
    {
        if (user.FindFirst("MaPhien")?.Value is string ma) cacPhien.TryRemove(ma, out _);
        PhienThayDoi?.Invoke();
    }
}


