// Vai trò tệp: Trường dữ liệu mở rộng (do người dùng tự định nghĩa) cho các đối tượng có sẵn, ví dụ Khoa thêm "Sáp nhập", "Ngày sáp nhập".
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Trường dữ liệu mở rộng (do người dùng tự định nghĩa) cho các đối tượng có sẵn, ví dụ Khoa thêm "Sáp nhập", "Ngày sáp nhập".
/// Không đổi cấu trúc bảng gốc: định nghĩa nằm ở bảng TruongTuyChinh, giá trị ở bảng GiaTriTuyChinh (khoá: trường + mã đối tượng).
/// Ai định nghĩa: Admin và quản lý sở hữu mảng của đối tượng (Khoa/Tổ/Cán bộ – QL khoa; Lớp/Học viên – QLHV; Chuyên đề/Phòng – QL đào tạo).
/// Ai nhập giá trị: người được sửa đối tượng đó. Trường "chỉ chủ sở hữu" ẩn với vai trò khác.
/// Mọi thay đổi đi qua SaveChanges nên được ghi nhật ký như dữ liệu gốc.
/// </summary>
public sealed class DichVuTruongTuyChinh(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, DichVuPhanQuyenDemo phanQuyen, KhoDuLieuDemo kho)
{
    public const int SoTruongToiDa = 20;
    public sealed record LoaiDoiTuong(string Ma, string Ten, string Vung, string BieuTuong);
    public static readonly LoaiDoiTuong[] CacLoai =
    [
        new("Khoa", "Khoa", "Khoa", "account_balance"), new("ToChuyenMon", "Tổ chuyên môn", "Khoa", "groups"), new("CanBo", "Cán bộ, giảng viên", "Khoa", "badge"),
        new("Lop", "Lớp đào tạo", "Lop", "school"), new("HocVien", "Học viên", "Lop", "person"),
        new("ChuyenDe", "Chuyên đề", "DaoTao", "menu_book"), new("Phong", "Phòng học", "DaoTao", "meeting_room"),
    ];
    public static string TenLoai(string ma) => CacLoai.FirstOrDefault(l => l.Ma == ma)?.Ten ?? ma;
    private static string? VungCua(string loai) => CacLoai.FirstOrDefault(l => l.Ma == loai)?.Vung;

    public sealed record BangMoRong(List<TruongTuyChinh> Truong, Dictionary<(int TruongId, string Ma), string> GiaTri)
    {
        public static readonly BangMoRong Rong = new([], []);
        public string Lay(TruongTuyChinh t, string? ma) => ma is null ? "" : GiaTri.GetValueOrDefault((t.Id, ma), "");
        /// <summary>Chuỗi hiển thị: Có/Không thay cho true/false, ngày dạng dd/MM/yyyy.</summary>
        public string HienThi(TruongTuyChinh t, string? ma) => DinhDang(t, Lay(t, ma));
    }

    public static string DinhDang(TruongTuyChinh t, string giaTri) => t.KieuDuLieu switch
    {
        _ when giaTri == "" => "",
        "Có/Không" => giaTri == "true" ? "Có" : "Không",
        "Ngày" => DateTime.TryParseExact(giaTri, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd/MM/yyyy") : giaTri,
        "Số" => double.TryParse(giaTri, NumberStyles.Float, CultureInfo.InvariantCulture, out var so) ? so.ToString("#,##0.##", CultureInfo.GetCultureInfo("vi-VN")) : giaTri,
        _ => giaTri
    };

    /// <summary>Được định nghĩa / sửa cấu trúc trường của loại đối tượng này không.</summary>
    public async Task<bool> DuocQuanTriAsync(string loai) => VungCua(loai) is { } v && await phanQuyen.CoQuyenVungAsync(v);

    /// <summary>Các trường người đang đăng nhập được xem, kèm giá trị của mọi đối tượng thuộc loại.</summary>
    public async Task<BangMoRong> LayBangAsync(string loai)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || VungCua(loai) is not { } vung) return BangMoRong.Rong;
        var laChu = nd.CoQuyenVung(vung);
        await using var db = csdl.CreateDbContext();
        var truong = (await db.TruongTuyChinh.AsNoTracking().Where(t => t.LoaiDoiTuong == loai).ToListAsync())
            .Where(t => laChu || !t.ChiChuSoHuu).OrderBy(t => t.ThuTu).ThenBy(t => t.Id).ToList();
        if (truong.Count == 0) return BangMoRong.Rong;
        var ids = truong.Select(t => t.Id).ToList();
        var gt = await db.GiaTriTuyChinh.AsNoTracking().Where(g => ids.Contains(g.TruongId)).ToListAsync();
        return new(truong, gt.ToDictionary(g => (g.TruongId, g.MaDoiTuong), g => g.GiaTri));
    }

    public async Task<KetQuaThaoTac> LuuTruongAsync(TruongTuyChinh t)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (VungCua(t.LoaiDoiTuong) is not { } vung) return new(false, "Loại đối tượng không hợp lệ.");
        if (!nd.CoQuyenVung(vung)) return new(false, $"Chỉ Admin và người quản lý mảng {TenLoai(t.LoaiDoiTuong).ToLowerInvariant()} được định nghĩa trường dữ liệu.");
        t.Ten = (t.Ten ?? "").Trim(); t.MoTa = (t.MoTa ?? "").Trim();
        t.LuaChon = string.Join("\n", (t.LuaChon ?? "").Split(['\n', ';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct());
        if (t.Ten.Length is 0 or > 60) return new(false, "Tên trường cần 1–60 ký tự.");
        if (t.MoTa.Length > 200) return new(false, "Mô tả tối đa 200 ký tự.");
        if (!TruongTuyChinh.CacKieu.Contains(t.KieuDuLieu)) return new(false, "Kiểu dữ liệu không hợp lệ.");
        if (t.KieuDuLieu == "Danh sách" && t.LuaChon.Split('\n').Length < 2) return new(false, "Kiểu danh sách cần ít nhất 2 lựa chọn (mỗi dòng hoặc cách nhau bằng dấu phẩy).");
        await using var db = MoCsdl(nd);
        var cung = await db.TruongTuyChinh.Where(x => x.LoaiDoiTuong == t.LoaiDoiTuong).ToListAsync();
        if (cung.Any(x => x.Id != t.Id && string.Equals(x.Ten, t.Ten, StringComparison.OrdinalIgnoreCase))) return new(false, $"Đã có trường \"{t.Ten}\" cho {TenLoai(t.LoaiDoiTuong).ToLowerInvariant()}.");
        if (t.Id == 0)
        {
            if (cung.Count >= SoTruongToiDa) return new(false, $"Mỗi loại đối tượng tối đa {SoTruongToiDa} trường mở rộng.");
            t.ThuTu = cung.Select(x => x.ThuTu).DefaultIfEmpty(0).Max() + 1; t.NguoiTao = nd.Ten; t.TaoLuc = DateTimeOffset.Now;
            db.TruongTuyChinh.Add(t);
        }
        else
        {
            var cu = cung.FirstOrDefault(x => x.Id == t.Id);
            if (cu is null) return new(false, "Không tìm thấy trường cần sửa.");
            if (cu.KieuDuLieu != t.KieuDuLieu && await db.GiaTriTuyChinh.AnyAsync(g => g.TruongId == t.Id && g.GiaTri != ""))
                return new(false, "Trường đã có dữ liệu, không đổi kiểu được. Hãy tạo trường mới.");
            cu.Ten = t.Ten; cu.MoTa = t.MoTa; cu.KieuDuLieu = t.KieuDuLieu; cu.LuaChon = t.LuaChon; cu.BatBuoc = t.BatBuoc; cu.HienTrenBang = t.HienTrenBang; cu.ChiChuSoHuu = t.ChiChuSoHuu;
        }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, t.Id == 0 ? "Đã thêm trường dữ liệu." : "Đã cập nhật trường dữ liệu.", t.Id.ToString(CultureInfo.InvariantCulture));
    }

    public async Task<KetQuaThaoTac> XoaTruongAsync(int id)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var t = await db.TruongTuyChinh.FindAsync(id);
        if (t is null) return new(false, "Không tìm thấy trường.");
        if (!nd.CoQuyenVung(VungCua(t.LoaiDoiTuong) ?? "-")) return new(false, "Bạn không quản lý loại đối tượng này.");
        db.GiaTriTuyChinh.RemoveRange(db.GiaTriTuyChinh.Where(g => g.TruongId == id));
        db.TruongTuyChinh.Remove(t);
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã xoá trường \"{t.Ten}\" và dữ liệu của trường.");
    }

    public async Task<KetQuaThaoTac> DoiThuTuAsync(int id, int huong)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var t = await db.TruongTuyChinh.FindAsync(id);
        if (t is null || !nd.CoQuyenVung(VungCua(t.LoaiDoiTuong) ?? "-")) return new(false, "Không đổi được thứ tự.");
        var ds = await db.TruongTuyChinh.Where(x => x.LoaiDoiTuong == t.LoaiDoiTuong).OrderBy(x => x.ThuTu).ThenBy(x => x.Id).ToListAsync();
        var i = ds.IndexOf(ds.First(x => x.Id == id)); var j = i + huong;
        if (j < 0 || j >= ds.Count) return new(true, "");
        (ds[i], ds[j]) = (ds[j], ds[i]);
        for (var k = 0; k < ds.Count; k++) ds[k].ThuTu = k + 1;
        db.BoQuaNhatKy = true;
        await db.SaveChangesAsync();
        return new(true, "");
    }

    /// <summary>Kiểm tra một giá trị theo kiểu của trường; trả lỗi hoặc null, và giá trị đã chuẩn hoá.</summary>
    public static (string? Loi, string GiaTri) ChuanHoa(TruongTuyChinh t, string? giaTri)
    {
        var v = (giaTri ?? "").Trim();
        if (v == "") return (t.BatBuoc ? $"\"{t.Ten}\" là bắt buộc." : null, "");
        if (v.Length > 500) return ($"\"{t.Ten}\" tối đa 500 ký tự.", v);
        switch (t.KieuDuLieu)
        {
            case "Số":
                return double.TryParse(v.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var so) ? (null, so.ToString(CultureInfo.InvariantCulture)) : ($"\"{t.Ten}\" phải là số.", v);
            case "Ngày":
                return DateTime.TryParseExact(v, ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? (null, d.ToString("yyyy-MM-dd")) : ($"\"{t.Ten}\" phải là ngày (dd/MM/yyyy).", v);
            case "Có/Không":
                return v is "true" or "false" ? (null, v) : ($"\"{t.Ten}\" chỉ nhận Có hoặc Không.", v);
            case "Danh sách":
                return t.CacLuaChon.Contains(v) ? (null, v) : ($"\"{t.Ten}\" phải chọn trong danh sách.", v);
            default:
                return (null, v);
        }
    }

    /// <summary>Lưu giá trị mở rộng của một đối tượng (người được sửa đối tượng đó).</summary>
    public async Task<KetQuaThaoTac> LuuGiaTriAsync(string loai, string ma, IReadOnlyDictionary<int, string?> giaTri)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (VungCua(loai) is not { } vung || !nd.CoQuyenVung(vung)) return new(false, "Bạn không được sửa dữ liệu của đối tượng này.");
        if (string.IsNullOrWhiteSpace(ma)) return new(false, "Thiếu mã đối tượng.");
        await using var db = MoCsdl(nd);
        var truong = await db.TruongTuyChinh.Where(t => t.LoaiDoiTuong == loai).ToDictionaryAsync(t => t.Id);
        var loi = new List<string>();
        var chuan = new Dictionary<int, string>();
        foreach (var (id, v) in giaTri)
        {
            if (!truong.TryGetValue(id, out var t)) continue;
            var (l, g) = ChuanHoa(t, v);
            if (l is not null) loi.Add(l); else chuan[id] = g;
        }
        if (loi.Count > 0) return new(false, string.Join(" ", loi));
        var ids = chuan.Keys.ToList();
        var hienCo = await db.GiaTriTuyChinh.Where(g => g.MaDoiTuong == ma && ids.Contains(g.TruongId)).ToDictionaryAsync(g => g.TruongId);
        foreach (var (id, g) in chuan)
        {
            if (hienCo.TryGetValue(id, out var cu)) { if (g == "") db.GiaTriTuyChinh.Remove(cu); else if (cu.GiaTri != g) cu.GiaTri = g; }
            else if (g != "") db.GiaTriTuyChinh.Add(new GiaTriTuyChinh { TruongId = id, MaDoiTuong = ma, GiaTri = g });
        }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, "Đã lưu thông tin mở rộng.");
    }

    private CoSoDuLieuNghiepVu MoCsdl(NguoiDungHienTai nd) { var db = csdl.CreateDbContext(); db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro; return db; }
}
