using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Đọc nhật ký (chỉ Admin) và ghi lượt xem dữ liệu cá nhân ("Hiện đủ").
/// Thao tác thêm/sửa/xoá được ghi tự động trong CoSoDuLieuNghiepVu.SaveChanges, không qua lớp này.
/// Không có hàm sửa hay xoá nhật ký; CSDL cũng có trigger chặn.
/// </summary>
public sealed class DichVuNhatKy(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, DichVuPhanQuyenDemo phanQuyen)
{
    private static readonly string[] TruongTen = ["TieuDe", "HoTen", "TenLop", "TenKhoa", "TenTo", "TenChuyenDe", "TenPhong", "Ten", "MaLop", "TieuDeTin"];

    /// <summary>
    /// Nhãn dễ đọc cho một dòng nhật ký, dùng ở "Hoạt động gần đây": ưu tiên tiêu đề/họ tên lấy từ giá trị mới (hoặc cũ khi xoá),
    /// vì mã nhiều đối tượng là chuỗi GUID dài (ví dụ thông báo). Không có tên thì giữ mã, mã GUID thì rút gọn.
    /// </summary>
    public static string TenHienThi(NhatKy x)
    {
        foreach (var json in new[] { x.GiaTriMoi, x.GiaTriCu })
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            try
            {
                using var tl = System.Text.Json.JsonDocument.Parse(json);
                if (tl.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
                foreach (var ten in TruongTen)
                    if (tl.RootElement.TryGetProperty(ten, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String && v.GetString() is { Length: > 0 } giaTri)
                        return $"{x.DoiTuong}: {giaTri}";
            }
            catch (System.Text.Json.JsonException) { }
        }
        if (Guid.TryParse(x.MaDoiTuong, out _)) return x.DoiTuong;
        return string.IsNullOrWhiteSpace(x.MaDoiTuong) ? x.DoiTuong : $"{x.DoiTuong} {x.MaDoiTuong}";
    }

    public async Task<List<NhatKy>> LayAsync(DateTime? tuNgay = null, DateTime? denNgay = null, string? tuKhoa = null, int toiDa = 1000)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaAdmin) throw new UnauthorizedAccessException("Chỉ Admin được xem nhật ký.");
        await using var db = csdl.CreateDbContext();
        var q = db.NhatKy.AsNoTracking();
        if (tuNgay is not null) { var tu = new DateTimeOffset(tuNgay.Value.Date); q = q.Where(x => x.ThoiDiem >= tu); }
        if (denNgay is not null) { var den = new DateTimeOffset(denNgay.Value.Date.AddDays(1)); q = q.Where(x => x.ThoiDiem < den); }
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = tuKhoa.Trim();
            q = q.Where(x => x.NguoiThucHien.Contains(k) || x.DoiTuong.Contains(k) || x.MaDoiTuong.Contains(k) || x.HanhDong.Contains(k));
        }
        return await q.OrderByDescending(x => x.Id).Take(toiDa).ToListAsync();
    }

    /// <summary>Ghi một thao tác không phải thêm/sửa/xoá (xuất, nhập Excel...).</summary>
    public async Task GhiAsync(string hanhDong, string doiTuong, string moTa)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap) return;
        await using var db = csdl.CreateDbContext();
        db.NhatKy.Add(new NhatKy { ThoiDiem = DateTimeOffset.Now, NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro, HanhDong = hanhDong, DoiTuong = doiTuong, MaDoiTuong = "", GiaTriMoi = moTa });
        await db.SaveChangesAsync();
    }

    /// <summary>Ghi "ai xem dữ liệu cá nhân của ai, lúc nào" khi người dùng bấm Hiện đủ.</summary>
    public async Task GhiXemDuLieuCaNhanAsync(string doiTuong, string maDoiTuong, string truong)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap) return;
        await using var db = csdl.CreateDbContext();
        db.NhatKy.Add(new NhatKy
        {
            ThoiDiem = DateTimeOffset.Now, NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro,
            HanhDong = "Xem dữ liệu cá nhân", DoiTuong = doiTuong, MaDoiTuong = maDoiTuong, GiaTriMoi = truong
        });
        await db.SaveChangesAsync();
    }
}
