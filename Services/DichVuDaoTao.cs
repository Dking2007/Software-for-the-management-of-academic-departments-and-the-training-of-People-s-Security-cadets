using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Mảng đào tạo: chuyên đề, chuyên môn giảng dạy, phòng và lịch đào tạo.
/// Quyền kiểm tra tại đây (không dựa vào việc ẩn nút):
/// - Chuyên đề, phòng, lịch: Admin và Quản lý đào tạo được sửa.
/// - Chuyên môn giảng dạy của cán bộ: Admin và Quản lý khoa được sửa.
/// - Lịch: giảng viên chỉ thấy buổi mình dạy, học viên chỉ thấy lịch lớp mình.
/// </summary>
public sealed class DichVuDaoTao(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen)
{
    public sealed record DongChuyenDe(ChuyenDe ChuyenDe, string TenKhoa, int SoGiaoVien, int SoBuoi);

    private CoSoDuLieuNghiepVu MoCsdl(NguoiDungHienTai nd)
    {
        var db = csdl.CreateDbContext();
        db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro;
        return db;
    }

    private static KetQuaThaoTac TuChoi() => new(false, "Tài khoản hiện tại không có quyền thực hiện thao tác này.");

    private static string? KiemTraHopLe(object x)
    {
        var loi = new List<ValidationResult>();
        return Validator.TryValidateObject(x, new ValidationContext(x), loi, true) ? null : loi.First().ErrorMessage;
    }

    private async Task<KetQuaThaoTac> LuuAsync(CoSoDuLieuNghiepVu db, string thanhCong)
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return new(false, "Không lưu được vì dữ liệu đang được tham chiếu hoặc trùng khoá. Hãy tải lại và thử lại."); }
        kho.BaoThayDoiNgoai();
        return new(true, thanhCong);
    }

    // ---------- Chuyên đề ----------

    public async Task<List<DongChuyenDe>> LayChuyenDeAsync()
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return [];
        await using var db = csdl.CreateDbContext();
        var soGv = await db.ChuyenMonGiangDay.GroupBy(x => x.MaChuyenDe).Select(g => new { g.Key, So = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.So);
        var soBuoi = await db.BuoiHoc.GroupBy(x => x.MaChuyenDe).Select(g => new { g.Key, So = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.So);
        var tenKhoa = TenKhoa();
        return (await db.ChuyenDe.AsNoTracking().OrderBy(x => x.MaChuyenDe).ToListAsync())
            .Select(x => new DongChuyenDe(x, tenKhoa.GetValueOrDefault(x.MaKhoa, x.MaKhoa), soGv.GetValueOrDefault(x.MaChuyenDe), soBuoi.GetValueOrDefault(x.MaChuyenDe)))
            .ToList();
    }

    public async Task<KetQuaThaoTac> LuuChuyenDeAsync(ChuyenDe doiTuong, string? maCu = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        var x = doiTuong.TaoBanSao();
        x.MaChuyenDe = (x.MaChuyenDe ?? "").Trim().ToUpperInvariant();
        x.TenChuyenDe = (x.TenChuyenDe ?? "").Trim();
        x.MoTa = (x.MoTa ?? "").Trim();
        if (maCu is not null && x.MaChuyenDe != maCu) return new(false, "Không được đổi mã chuyên đề khi sửa.");
        if (KiemTraHopLe(x) is { } loi) return new(false, loi);
        if (!TenKhoa().ContainsKey(x.MaKhoa)) return new(false, "Khoa phụ trách không tồn tại.");
        await using var db = MoCsdl(nd);
        if (maCu is null)
        {
            if (await db.ChuyenDe.AnyAsync(c => c.MaChuyenDe == x.MaChuyenDe)) return new(false, $"Mã chuyên đề {x.MaChuyenDe} đã tồn tại.");
            db.ChuyenDe.Add(x);
        }
        else
        {
            var cu = await db.ChuyenDe.FindAsync(maCu);
            if (cu is null) return new(false, "Không tìm thấy chuyên đề cần sửa.");
            db.Entry(cu).CurrentValues.SetValues(x);
        }
        return await LuuAsync(db, maCu is null ? "Đã thêm chuyên đề." : "Đã cập nhật chuyên đề.");
    }

    public async Task<KetQuaThaoTac> XoaChuyenDeAsync(string ma)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        await using var db = MoCsdl(nd);
        var x = await db.ChuyenDe.FindAsync(ma);
        if (x is null) return new(false, "Không tìm thấy chuyên đề.");
        var soBuoi = await db.BuoiHoc.CountAsync(b => b.MaChuyenDe == ma);
        var soDiem = await db.KetQua.CountAsync(k => k.MaChuyenDe == ma);
        if (soBuoi + soDiem > 0) return new(false, $"Chuyên đề còn {soBuoi:N0} buổi học trong lịch và {soDiem:N0} kết quả học tập. Không thể xoá để giữ lịch sử học vụ.");
        db.ChuyenDe.Remove(x);
        return await LuuAsync(db, "Đã xoá chuyên đề.");
    }

    // ---------- Chuyên môn giảng dạy ----------

    public async Task<List<string>> LayGiaoVienDuDieuKienAsync(string maChuyenDe)
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return [];
        await using var db = csdl.CreateDbContext();
        return await db.ChuyenMonGiangDay.Where(x => x.MaChuyenDe == maChuyenDe).Select(x => x.MaCanBo).OrderBy(x => x).ToListAsync();
    }

    public async Task<List<string>> LayChuyenDeCuaCanBoAsync(string maCanBo)
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return [];
        await using var db = csdl.CreateDbContext();
        return await db.ChuyenMonGiangDay.Where(x => x.MaCanBo == maCanBo).Select(x => x.MaChuyenDe).OrderBy(x => x).ToListAsync();
    }

    /// <summary>Đặt danh sách cán bộ đủ điều kiện dạy chuyên đề (dữ liệu của mảng khoa).</summary>
    public async Task<KetQuaThaoTac> DatGiaoVienDuDieuKienAsync(string maChuyenDe, IEnumerable<string> cacMaCanBo)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("Khoa")) return TuChoi();
        var moi = cacMaCanBo.Distinct().ToHashSet();
        lock (kho.KhoaDongBo)
            if (moi.FirstOrDefault(m => !kho.DanhSachCanBo.Any(c => c.MaCanBo == m)) is { } sai) return new(false, $"Không tìm thấy cán bộ {sai}.");
        await using var db = MoCsdl(nd);
        if (!await db.ChuyenDe.AnyAsync(c => c.MaChuyenDe == maChuyenDe)) return new(false, "Không tìm thấy chuyên đề.");
        // Không bỏ người đang có buổi dạy chuyên đề này trong lịch, tránh lịch mâu thuẫn với chuyên môn.
        var dangDay = await db.BuoiHoc.Where(b => b.MaChuyenDe == maChuyenDe && b.Ngay >= DateTime.Today).Select(b => b.MaCanBo).Distinct().ToListAsync();
        var biBo = dangDay.Where(m => !moi.Contains(m)).ToList();
        if (biBo.Count > 0) return new(false, $"{string.Join(", ", biBo.Select(TenCanBo))} đang có buổi dạy chuyên đề này từ hôm nay trở đi. Hãy đổi giảng viên trong lịch trước khi bỏ khỏi danh sách đủ điều kiện.");
        var hienCo = await db.ChuyenMonGiangDay.Where(x => x.MaChuyenDe == maChuyenDe).ToListAsync();
        db.ChuyenMonGiangDay.RemoveRange(hienCo.Where(x => !moi.Contains(x.MaCanBo)));
        db.ChuyenMonGiangDay.AddRange(moi.Where(m => !hienCo.Any(x => x.MaCanBo == m)).Select(m => new ChuyenMonGiangDay { MaCanBo = m, MaChuyenDe = maChuyenDe }));
        return await LuuAsync(db, $"Đã cập nhật: {moi.Count} cán bộ đủ điều kiện dạy chuyên đề.");
    }

    // ---------- Phòng ----------

    public async Task<List<Phong>> LayPhongAsync()
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return [];
        await using var db = csdl.CreateDbContext();
        return await db.Phong.AsNoTracking().OrderBy(x => x.MaPhong).ToListAsync();
    }

    public async Task<KetQuaThaoTac> LuuPhongAsync(Phong doiTuong, string? maCu = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        var x = doiTuong.TaoBanSao();
        x.MaPhong = (x.MaPhong ?? "").Trim().ToUpperInvariant();
        x.TenPhong = (x.TenPhong ?? "").Trim();
        if (maCu is not null && x.MaPhong != maCu) return new(false, "Không được đổi mã phòng khi sửa.");
        if (KiemTraHopLe(x) is { } loi) return new(false, loi);
        if (!LoaiPhong.Contains(x.LoaiPhong)) return new(false, "Loại phòng không hợp lệ.");
        await using var db = MoCsdl(nd);
        if (maCu is null)
        {
            if (await db.Phong.AnyAsync(p => p.MaPhong == x.MaPhong)) return new(false, $"Mã phòng {x.MaPhong} đã tồn tại.");
            db.Phong.Add(x);
        }
        else
        {
            var cu = await db.Phong.FindAsync(maCu);
            if (cu is null) return new(false, "Không tìm thấy phòng cần sửa.");
            // Giảm sức chứa hoặc chuyển sang bảo trì phải không làm hỏng lịch sắp tới.
            var lichToi = await db.BuoiHoc.Where(b => b.MaPhong == maCu && b.Ngay >= DateTime.Today).ToListAsync();
            if (x.DangBaoTri && lichToi.Count > 0) return new(false, $"Phòng còn {lichToi.Count} buổi học từ hôm nay trở đi. Hãy chuyển các buổi sang phòng khác trước khi đặt bảo trì.");
            var siSo = SiSoTheoLop();
            var vuot = lichToi.Select(b => b.MaLop).Distinct().Where(l => siSo.GetValueOrDefault(l) > x.SucChua).ToList();
            if (vuot.Count > 0) return new(false, $"Sức chứa {x.SucChua} nhỏ hơn sĩ số các lớp đang xếp tại phòng: {string.Join(", ", vuot)}.");
            db.Entry(cu).CurrentValues.SetValues(x);
        }
        return await LuuAsync(db, maCu is null ? "Đã thêm phòng." : "Đã cập nhật phòng.");
    }

    public async Task<KetQuaThaoTac> XoaPhongAsync(string ma)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        await using var db = MoCsdl(nd);
        var x = await db.Phong.FindAsync(ma);
        if (x is null) return new(false, "Không tìm thấy phòng.");
        var soBuoi = await db.BuoiHoc.CountAsync(b => b.MaPhong == ma);
        if (soBuoi > 0) return new(false, $"Phòng đã được dùng cho {soBuoi:N0} buổi học. Không thể xoá; có thể đặt trạng thái bảo trì.");
        db.Phong.Remove(x);
        return await LuuAsync(db, "Đã xoá phòng.");
    }

    public static readonly string[] LoaiPhong = ["Giảng đường", "Phòng máy", "Phòng thi", "Sân tập"];

    // ---------- Lịch đào tạo ----------

    /// <summary>Lịch trong khoảng ngày; giảng viên và học viên bị ép bộ lọc về phần của mình.</summary>
    public async Task<List<BuoiHocHienThi>> LayLichAsync(DateTime tuNgay, DateTime denNgay, string? maLop = null, string? maCanBo = null, string? maPhong = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || nd.VaiTro == "Guest") return [];
        if (nd.LaGiangVien) maCanBo = nd.MaCanBo;
        if (nd.LaHocVien) { lock (kho.KhoaDongBo) maLop = kho.DanhSachHocVien.FirstOrDefault(h => h.MaHocVien == nd.MaHocVien)?.MaLop ?? "—"; }
        await using var db = csdl.CreateDbContext();
        var q = db.BuoiHoc.AsNoTracking().Where(b => b.Ngay >= tuNgay.Date && b.Ngay <= denNgay.Date);
        if (!string.IsNullOrEmpty(maLop)) q = q.Where(b => b.MaLop == maLop);
        if (!string.IsNullOrEmpty(maCanBo)) q = q.Where(b => b.MaCanBo == maCanBo);
        if (!string.IsNullOrEmpty(maPhong)) q = q.Where(b => b.MaPhong == maPhong);
        var cacBuoi = await q.OrderBy(b => b.Ngay).ThenBy(b => b.TietBatDau).ThenBy(b => b.MaLop).ToListAsync();
        return await HienThiAsync(db, cacBuoi);
    }

    public async Task<BuoiHocHienThi?> LayBuoiAsync(int id)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap) return null;
        await using var db = csdl.CreateDbContext();
        var b = await db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return null;
        return (await HienThiAsync(db, [b])).Single();
    }

    private async Task<List<BuoiHocHienThi>> HienThiAsync(CoSoDuLieuNghiepVu db, List<BuoiHoc> cacBuoi)
    {
        var ids = cacBuoi.Select(b => b.Id).ToList();
        var daDiemDanh = (await db.DiemDanh.Where(d => ids.Contains(d.BuoiHocId)).Select(d => d.BuoiHocId).Distinct().ToListAsync()).ToHashSet();
        var tenCd = await db.ChuyenDe.ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var tenPhong = await db.Phong.ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);
        var siSo = SiSoTheoLop();
        return cacBuoi.Select(b => new BuoiHocHienThi(b, tenCd.GetValueOrDefault(b.MaChuyenDe, b.MaChuyenDe), TenCanBo(b.MaCanBo),
            tenPhong.GetValueOrDefault(b.MaPhong, b.MaPhong), siSo.GetValueOrDefault(b.MaLop), daDiemDanh.Contains(b.Id))).ToList();
    }

    /// <summary>Mọi lý do buổi học không xếp được (trống nếu hợp lệ). Không dừng ở lỗi đầu tiên để người dùng sửa một lần.</summary>
    public async Task<List<string>> KiemTraBuoiHocAsync(BuoiHoc b)
    {
        var loi = new List<string>();
        if (KiemTraHopLe(b) is { } loiTruong) { loi.Add(loiTruong); return loi; }
        if (QuyTacHocVu.KiemTraKhungTiet(b.TietBatDau, b.SoTiet) is { } loiTiet) loi.Add(loiTiet);
        if (b.Ngay.DayOfWeek == DayOfWeek.Sunday) loi.Add("Không xếp lịch vào Chủ nhật.");
        int siSo;
        lock (kho.KhoaDongBo)
        {
            if (!kho.DanhSachLop.Any(l => l.MaLop == b.MaLop)) loi.Add($"Lớp {b.MaLop} không tồn tại.");
            if (!kho.DanhSachCanBo.Any(c => c.MaCanBo == b.MaCanBo)) loi.Add("Giảng viên không tồn tại.");
            siSo = kho.DanhSachHocVien.Count(h => h.MaLop == b.MaLop);
        }
        await using var db = csdl.CreateDbContext();
        var cd = await db.ChuyenDe.FindAsync(b.MaChuyenDe);
        var phong = await db.Phong.FindAsync(b.MaPhong);
        if (cd is null) loi.Add("Chuyên đề không tồn tại.");
        if (phong is null) loi.Add("Phòng không tồn tại.");
        if (phong is { DangBaoTri: true }) loi.Add($"{phong.TenPhong} đang bảo trì, không xếp được.");
        if (phong is not null && siSo > phong.SucChua) loi.Add($"Lớp {b.MaLop} có {siSo} học viên, vượt sức chứa {phong.SucChua} chỗ của {phong.TenPhong}.");
        if (cd is not null && !await db.ChuyenMonGiangDay.AnyAsync(x => x.MaCanBo == b.MaCanBo && x.MaChuyenDe == b.MaChuyenDe))
            loi.Add($"{TenCanBo(b.MaCanBo)} chưa có trong danh sách đủ điều kiện dạy chuyên đề \"{cd.TenChuyenDe}\".");
        if (loi.Count > 0) return loi;

        var cungNgay = await db.BuoiHoc.AsNoTracking().Where(x => x.Ngay == b.Ngay.Date && x.Id != b.Id).ToListAsync();
        foreach (var x in cungNgay.Where(x => QuyTacHocVu.TrungGio(x, b)))
        {
            var moTa = $"tiết {x.TietBatDau}–{x.TietKetThuc} ngày {x.Ngay:dd/MM/yyyy}";
            if (x.MaCanBo == b.MaCanBo) loi.Add($"Giảng viên {TenCanBo(b.MaCanBo)} đã dạy lớp {x.MaLop} tại {x.MaPhong}, {moTa}.");
            if (x.MaPhong == b.MaPhong) loi.Add($"Phòng {b.MaPhong} đã được lớp {x.MaLop} sử dụng, {moTa}.");
            if (x.MaLop == b.MaLop) loi.Add($"Lớp {b.MaLop} đã có buổi học khác, {moTa}.");
        }
        return loi;
    }

    public async Task<KetQuaThaoTac> LuuBuoiHocAsync(BuoiHoc doiTuong)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        var b = doiTuong.TaoBanSao();
        b.Ngay = b.Ngay.Date;
        b.GhiChu = (b.GhiChu ?? "").Trim();
        var loi = await KiemTraBuoiHocAsync(b);
        if (loi.Count > 0) return new(false, string.Join(" ", loi));
        await using var db = MoCsdl(nd);
        if (b.Id == 0) db.BuoiHoc.Add(b);
        else
        {
            var cu = await db.BuoiHoc.FindAsync(b.Id);
            if (cu is null) return new(false, "Không tìm thấy buổi học cần sửa.");
            if (cu.MaLop != b.MaLop && await db.DiemDanh.AnyAsync(d => d.BuoiHocId == b.Id)) return new(false, "Buổi học đã có điểm danh, không thể đổi sang lớp khác.");
            db.Entry(cu).CurrentValues.SetValues(b);
        }
        return await LuuAsync(db, b.Id == 0 ? $"Đã xếp buổi học cho lớp {b.MaLop} ngày {b.Ngay:dd/MM}." : "Đã cập nhật buổi học.");
    }

    public async Task<KetQuaThaoTac> XoaBuoiHocAsync(int id)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return TuChoi();
        await using var db = MoCsdl(nd);
        var b = await db.BuoiHoc.FindAsync(id);
        if (b is null) return new(false, "Không tìm thấy buổi học.");
        var soDiemDanh = await db.DiemDanh.CountAsync(d => d.BuoiHocId == id);
        if (soDiemDanh > 0) return new(false, $"Buổi học đã có {soDiemDanh} dòng điểm danh. Không thể xoá để giữ lịch sử chuyên cần.");
        db.BuoiHoc.Remove(b);
        return await LuuAsync(db, "Đã xoá buổi học.");
    }

    // ---------- Tiện ích ----------

    private Dictionary<string, string> TenKhoa() { lock (kho.KhoaDongBo) return kho.DanhSachKhoa.ToDictionary(k => k.MaKhoa, k => k.TenKhoa); }
    private Dictionary<string, int> SiSoTheoLop() { lock (kho.KhoaDongBo) return kho.DanhSachHocVien.GroupBy(h => h.MaLop).ToDictionary(g => g.Key, g => g.Count()); }
    public string TenCanBo(string maCanBo)
    {
        lock (kho.KhoaDongBo)
            return kho.DanhSachCanBo.FirstOrDefault(c => c.MaCanBo == maCanBo) is { } c ? $"{c.CapBacHam} {c.HoTen}" : maCanBo;
    }

    /// <summary>Danh sách giảng viên (mã + tên) cho ô chọn; không chứa trường cá nhân.</summary>
    public List<(string Ma, string Ten, string MaKhoa)> CacGiangVien()
    {
        lock (kho.KhoaDongBo) return kho.DanhSachCanBo.Select(c => (c.MaCanBo, $"{c.HoTen} ({c.MaCanBo})", c.MaKhoa)).ToList();
    }
}
