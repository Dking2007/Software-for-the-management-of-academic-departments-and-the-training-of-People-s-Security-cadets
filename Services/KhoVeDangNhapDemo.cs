using System.Security.Claims;
using System.Security.Cryptography;

namespace Nhom2_BTL.Services;

/// <summary>Kho vé ngắn hạn để mở phân hệ demo ở tab mới mà không cho trình duyệt tự chọn vai trò.</summary>
public sealed class KhoVeDangNhapDemo
{
    private readonly object khoaDongBo = new();
    private readonly Dictionary<string, (ClaimsPrincipal NguoiDung, DateTime HetHan)> cacVe = new(StringComparer.Ordinal);

    // Tạo mã ngẫu nhiên ngắn hạn, chỉ lưu danh tính phía máy chủ và không nhận vai trò từ URL.
    public string TaoVe(ClaimsPrincipal nguoiDung)
    {
        var maNgauNhien = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        lock (khoaDongBo)
        {
            XoaVeHetHan();
            cacVe[maNgauNhien] = (nguoiDung, DateTime.UtcNow.AddMinutes(10));
        }
        return maNgauNhien;
    }

    // Vé có thể dùng lại trong mười phút để mở các tab bổ sung rồi tự hết hạn.
    public ClaimsPrincipal? DoiVe(string maVe)
    {
        lock (khoaDongBo)
        {
            if (!cacVe.TryGetValue(maVe, out var thongTin) || thongTin.HetHan <= DateTime.UtcNow) { cacVe.Remove(maVe); return null; }
            return thongTin.NguoiDung;
        }
    }

    private void XoaVeHetHan()
    {
        foreach (var maVe in cacVe.Where(ve => ve.Value.HetHan <= DateTime.UtcNow).Select(ve => ve.Key).ToArray()) cacVe.Remove(maVe);
    }

    // Đăng xuất phải vô hiệu hóa các liên kết mở tab đã cấp từ danh tính này.
    public void ThuHoiVe(ClaimsPrincipal nguoiDung)
    {
        lock (khoaDongBo)
            foreach (var ma in cacVe.Where(v => ReferenceEquals(v.Value.NguoiDung, nguoiDung)).Select(v => v.Key).ToArray()) cacVe.Remove(ma);
    }
}



