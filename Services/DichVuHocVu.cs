using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Mảng học vụ: điểm danh theo buổi, tự điểm danh trong khung giờ, điểm kiểm tra, bảng điểm 30/70.
/// Quyền (kiểm tra tại đây):
/// - Admin, Quản lý học viên: xem và sửa mọi lớp.
/// - Giảng viên: điểm danh buổi mình dạy; nhập điểm chuyên đề mình dạy cho lớp đó.
/// - Học viên: tự điểm danh trong khung giờ; chỉ xem điểm danh và điểm của mình.
/// - Quản lý khoa, Quản lý đào tạo: không xem điểm từng học viên (hàng rào 2).
/// </summary>
public sealed class DichVuHocVu(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen)
{
    public sealed record DongDiemDanh(string MaHocVien, string HoTen, string ChucVu, string? TrangThai, bool TuDiemDanh, DateTimeOffset? ThoiDiem);
    public sealed record KetQuaCaNhan(string MaChuyenDe, string TenChuyenDe, double? ChuyenCan, int SoBuoi, double? DiemKiemTra, double? TongKet, string XepLoai, string? LyDoThieu);
    public sealed record DongLichSuDiemDanh(BuoiHoc Buoi, string TenChuyenDe, string TrangThai, bool TuDiemDanh);

    /// <summary>Đồng hồ dùng cho cổng tự điểm danh; bộ kiểm thử có thể thay.</summary>
    public Func<DateTime> BayGio { get; set; } = () => DateTime.Now;

    private CoSoDuLieuNghiepVu MoCsdl(NguoiDungHienTai nd)
    {
        var db = csdl.CreateDbContext();
        db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro;
        return db;
    }

    private List<HocVien> HocVienCuaLop(string maLop)
    {
        lock (kho.KhoaDongBo) return kho.DanhSachHocVien.Where(h => h.MaLop == maLop).OrderBy(h => h.KhoaSapXepTen, StringComparer.Ordinal).Select(h => h.TaoBanSao()).ToList();
    }

    private string? LopCuaHocVien(string? maHocVien)
    {
        if (maHocVien is null) return null;
        lock (kho.KhoaDongBo) return kho.DanhSachHocVien.FirstOrDefault(h => h.MaHocVien == maHocVien)?.MaLop;
    }

    // ---------- Điểm danh ----------

    /// <summary>Được ghi điểm danh cho buổi này không: Admin, Quản lý học viên, hoặc giảng viên dạy buổi đó.</summary>
    public static bool DuocGhiDiemDanh(NguoiDungHienTai nd, BuoiHoc b) => nd.CoQuyenVung("Lop") || (nd.LaGiangVien && nd.MaCanBo == b.MaCanBo);

    public async Task<(BuoiHoc? Buoi, List<DongDiemDanh> Dong, bool DuocGhi, string? Loi)> LayDiemDanhAsync(int buoiHocId)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = csdl.CreateDbContext();
        var b = await db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buoiHocId);
        if (b is null) return (null, [], false, "Không tìm thấy buổi học.");
        var duocGhi = DuocGhiDiemDanh(nd, b);
        if (!duocGhi) return (b, [], false, "Chỉ giảng viên dạy buổi này, Quản lý học viên hoặc Admin được xem và ghi điểm danh.");
        var daGhi = await db.DiemDanh.AsNoTracking().Where(d => d.BuoiHocId == buoiHocId).ToDictionaryAsync(d => d.MaHocVien);
        var dong = HocVienCuaLop(b.MaLop).Select(h => daGhi.TryGetValue(h.MaHocVien, out var d)
            ? new DongDiemDanh(h.MaHocVien, h.HoTen, h.ChucVu, d.TrangThai, d.TuDiemDanh, d.ThoiDiem)
            : new DongDiemDanh(h.MaHocVien, h.HoTen, h.ChucVu, null, false, null)).ToList();
        return (b, dong, true, null);
    }

    public async Task<KetQuaThaoTac> LuuDiemDanhAsync(int buoiHocId, IReadOnlyDictionary<string, string> trangThai)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var b = await db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buoiHocId);
        if (b is null) return new(false, "Không tìm thấy buổi học.");
        if (!DuocGhiDiemDanh(nd, b)) return new(false, "Chỉ giảng viên dạy buổi này, Quản lý học viên hoặc Admin được ghi điểm danh.");
        if (b.Ngay.Date > BayGio().Date) return new(false, $"Buổi học ngày {b.Ngay:dd/MM/yyyy} chưa diễn ra, chưa thể điểm danh.");
        var trongLop = HocVienCuaLop(b.MaLop).Select(h => h.MaHocVien).ToHashSet();
        if (trangThai.Keys.FirstOrDefault(m => !trongLop.Contains(m)) is { } ngoai) return new(false, $"Học viên {ngoai} không thuộc lớp {b.MaLop}.");
        if (trangThai.Values.FirstOrDefault(t => !TrangThaiDiemDanh.TatCa.Contains(t)) is { } sai) return new(false, $"Trạng thái \"{sai}\" không hợp lệ.");
        var hienCo = await db.DiemDanh.Where(d => d.BuoiHocId == buoiHocId).ToDictionaryAsync(d => d.MaHocVien);
        var soDoi = 0;
        foreach (var (ma, tt) in trangThai)
        {
            if (hienCo.TryGetValue(ma, out var d))
            {
                if (d.TrangThai == tt) continue;
                d.TrangThai = tt; d.TuDiemDanh = false; d.NguoiGhi = nd.Ten; d.ThoiDiem = DateTimeOffset.Now;
            }
            else db.DiemDanh.Add(new DiemDanh { BuoiHocId = buoiHocId, MaHocVien = ma, TrangThai = tt, NguoiGhi = nd.Ten, ThoiDiem = DateTimeOffset.Now });
            soDoi++;
        }
        if (soDoi == 0) return new(true, "Không có thay đổi nào cần lưu.");
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã lưu điểm danh lớp {b.MaLop} ngày {b.Ngay:dd/MM} ({soDoi} học viên thay đổi).");
    }

    /// <summary>Buổi học của học viên đang mở cổng tự điểm danh (nếu có).</summary>
    public async Task<BuoiHoc?> LayBuoiDangMoTuDiemDanhAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var maLop = LopCuaHocVien(nd.MaHocVien);
        if (maLop is null) return null;
        var bayGio = BayGio();
        await using var db = csdl.CreateDbContext();
        var homNay = await db.BuoiHoc.AsNoTracking().Where(b => b.MaLop == maLop && b.Ngay == bayGio.Date).ToListAsync();
        return homNay.FirstOrDefault(b => QuyTacHocVu.KiemTraTuDiemDanh(b, bayGio) is null);
    }

    public async Task<KetQuaThaoTac> TuDiemDanhAsync(int buoiHocId)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaHocVien) return new(false, "Chỉ tài khoản học viên được tự điểm danh.");
        await using var db = MoCsdl(nd);
        var b = await db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buoiHocId);
        if (b is null) return new(false, "Không tìm thấy buổi học.");
        if (LopCuaHocVien(nd.MaHocVien) != b.MaLop) return new(false, "Buổi học này không thuộc lớp của bạn.");
        var bayGio = BayGio();
        if (QuyTacHocVu.KiemTraTuDiemDanh(b, bayGio) is { } loi) return new(false, loi);
        var cu = await db.DiemDanh.AsNoTracking().FirstOrDefaultAsync(d => d.BuoiHocId == buoiHocId && d.MaHocVien == nd.MaHocVien);
        if (cu is not null) return new(false, $"Bạn đã được ghi \"{cu.TrangThai}\" lúc {cu.ThoiDiem.ToLocalTime():HH:mm}. Không cần điểm danh lại.");
        var tt = QuyTacHocVu.TrangThaiTuDiemDanh(b, bayGio);
        db.DiemDanh.Add(new DiemDanh { BuoiHocId = buoiHocId, MaHocVien = nd.MaHocVien!, TrangThai = tt, TuDiemDanh = true, NguoiGhi = nd.Ten, ThoiDiem = new DateTimeOffset(bayGio) });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return new(false, "Bạn vừa được điểm danh ở nơi khác. Hãy tải lại trang."); }
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã điểm danh: {tt} lúc {bayGio:HH:mm}.");
    }

    public async Task<List<DongLichSuDiemDanh>> LayLichSuDiemDanhCuaToiAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaHocVien) return [];
        await using var db = csdl.CreateDbContext();
        var rows = await (from d in db.DiemDanh.AsNoTracking()
                          join b in db.BuoiHoc.AsNoTracking() on d.BuoiHocId equals b.Id
                          join c in db.ChuyenDe.AsNoTracking() on b.MaChuyenDe equals c.MaChuyenDe
                          where d.MaHocVien == nd.MaHocVien
                          select new { b, c.TenChuyenDe, d.TrangThai, d.TuDiemDanh }).ToListAsync();
        return rows.OrderByDescending(x => x.b.Ngay).ThenByDescending(x => x.b.TietBatDau).Select(x => new DongLichSuDiemDanh(x.b, x.TenChuyenDe, x.TrangThai, x.TuDiemDanh)).ToList();
    }

    // ---------- Điểm ----------

    /// <summary>Các chuyên đề lớp đã có trong lịch (để chọn bảng điểm).</summary>
    public async Task<List<ChuyenDe>> LayChuyenDeCuaLopAsync(string maLop)
    {
        await using var db = csdl.CreateDbContext();
        var ma = await db.BuoiHoc.Where(b => b.MaLop == maLop).Select(b => b.MaChuyenDe).Distinct().ToListAsync();
        return await db.ChuyenDe.AsNoTracking().Where(c => ma.Contains(c.MaChuyenDe)).OrderBy(c => c.MaChuyenDe).ToListAsync();
    }

    /// <summary>Giảng viên được nhập điểm cho (lớp, chuyên đề) khi có buổi dạy chuyên đề đó cho lớp đó.</summary>
    private static async Task<bool> GiangVienDayAsync(CoSoDuLieuNghiepVu db, string maCanBo, string maLop, string maChuyenDe)
        => await db.BuoiHoc.AnyAsync(b => b.MaCanBo == maCanBo && b.MaLop == maLop && b.MaChuyenDe == maChuyenDe);

    public async Task<(List<DongBangDiem> Dong, bool DuocSua, string? Loi)> LayBangDiemAsync(string maLop, string maChuyenDe)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = csdl.CreateDbContext();
        var duocSua = nd.CoQuyenVung("Lop") || (nd.LaGiangVien && await GiangVienDayAsync(db, nd.MaCanBo!, maLop, maChuyenDe));
        var laHocVienLopNay = nd.LaHocVien && LopCuaHocVien(nd.MaHocVien) == maLop;
        if (!duocSua && !laHocVienLopNay)
            return ([], false, nd.LaQuanLy("Khoa") || nd.LaQuanLy("DaoTao")
                ? "Vai trò của bạn chỉ xem số liệu tổng hợp, không xem điểm từng học viên."
                : "Chỉ giảng viên dạy chuyên đề này, Quản lý học viên hoặc Admin được xem bảng điểm của lớp.");
        var dong = await TinhBangDiemAsync(db, maLop, maChuyenDe);
        if (!duocSua) dong = dong.Where(d => d.MaHocVien == nd.MaHocVien).ToList();
        return (dong, duocSua, null);
    }

    private async Task<List<DongBangDiem>> TinhBangDiemAsync(CoSoDuLieuNghiepVu db, string maLop, string maChuyenDe)
    {
        var homNay = BayGio().Date;
        var buoi = await db.BuoiHoc.Where(b => b.MaLop == maLop && b.MaChuyenDe == maChuyenDe && b.Ngay <= homNay).Select(b => b.Id).ToListAsync();
        var diemDanh = await db.DiemDanh.AsNoTracking().Where(d => buoi.Contains(d.BuoiHocId)).ToListAsync();
        var soBuoiDaDiemDanh = diemDanh.Select(d => d.BuoiHocId).Distinct().Count();
        var theoHv = diemDanh.GroupBy(d => d.MaHocVien).ToDictionary(g => g.Key, g => g.Select(d => d.TrangThai).ToList());
        var hocVien = HocVienCuaLop(maLop);
        var maHv = hocVien.Select(h => h.MaHocVien).ToList();
        var diem = await db.KetQua.AsNoTracking().Where(k => k.MaChuyenDe == maChuyenDe && maHv.Contains(k.MaHocVien)).ToDictionaryAsync(k => k.MaHocVien, k => k.DiemKiemTra);
        return hocVien.Select(h =>
        {
            var cc = QuyTacHocVu.TinhChuyenCan(theoHv.GetValueOrDefault(h.MaHocVien, []));
            var kt = diem.GetValueOrDefault(h.MaHocVien);
            var (tk, lyDo) = QuyTacHocVu.TinhTongKet(cc, kt);
            return new DongBangDiem(h.MaHocVien, h.HoTen, cc, theoHv.GetValueOrDefault(h.MaHocVien)?.Count ?? 0, kt, tk, QuyTacHocVu.XepLoai(tk), QuyTacHocVu.Dat(tk), lyDo);
        }).ToList();
    }

    public async Task<KetQuaThaoTac> LuuDiemKiemTraAsync(string maLop, string maChuyenDe, IReadOnlyDictionary<string, double?> diem)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        if (!(nd.CoQuyenVung("Lop") || (nd.LaGiangVien && await GiangVienDayAsync(db, nd.MaCanBo!, maLop, maChuyenDe))))
            return new(false, "Chỉ giảng viên dạy chuyên đề này cho lớp, Quản lý học viên hoặc Admin được nhập điểm.");
        var trongLop = HocVienCuaLop(maLop).Select(h => h.MaHocVien).ToHashSet();
        foreach (var (ma, d) in diem)
        {
            if (!trongLop.Contains(ma)) return new(false, $"Học viên {ma} không thuộc lớp {maLop}.");
            if (QuyTacHocVu.KiemTraDiem(d) is { } loi) return new(false, $"{ma}: {loi}");
        }
        var hienCo = await db.KetQua.Where(k => k.MaChuyenDe == maChuyenDe && diem.Keys.Contains(k.MaHocVien)).ToDictionaryAsync(k => k.MaHocVien);
        var soDoi = 0;
        foreach (var (ma, d) in diem)
        {
            var lamTron = d is null ? (double?)null : Math.Round(d.Value, 1, MidpointRounding.AwayFromZero);
            if (hienCo.TryGetValue(ma, out var k))
            {
                if (k.DiemKiemTra == lamTron) continue;
                k.DiemKiemTra = lamTron; k.NguoiNhap = nd.Ten; k.CapNhatLuc = DateTimeOffset.Now;
            }
            else if (lamTron is not null)
                db.KetQua.Add(new KetQua { MaHocVien = ma, MaChuyenDe = maChuyenDe, DiemKiemTra = lamTron, NguoiNhap = nd.Ten, CapNhatLuc = DateTimeOffset.Now });
            else continue;
            soDoi++;
        }
        if (soDoi == 0) return new(true, "Không có điểm nào thay đổi.");
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã lưu điểm kiểm tra cho {soDoi} học viên.");
    }

    /// <param name="ThuTu">Học kỳ thứ mấy của khoá học (1–8), tính từ năm nhập học.</param>
    /// <param name="SoTiet">Số tiết của từng chuyên đề (để tính khối lượng tích luỹ).</param>
    public sealed record HocKyCaNhan(string NamHoc, int HocKy, bool HienTai, List<KetQuaCaNhan> MonHoc, Dictionary<string, DateTime?> NgayThi, int ThuTu = 0, Dictionary<string, int>? SoTiet = null)
    {
        public int TongTiet => MonHoc.Sum(m => SoTiet?.GetValueOrDefault(m.MaChuyenDe) ?? 0);
        public int TietDat => MonHoc.Where(m => m.TongKet is >= 5).Sum(m => SoTiet?.GetValueOrDefault(m.MaChuyenDe) ?? 0);
        public double? DiemTrungBinh => MonHoc.Count(m => m.TongKet is not null) == 0 ? null : Math.Round(MonHoc.Where(m => m.TongKet is not null).Average(m => m.TongKet!.Value), 2);
    }

    /// <summary>
    /// Bảng điểm toàn khoá của học viên đang đăng nhập: các học kỳ đã qua (lưu trữ) và học kỳ hiện tại
    /// (tính từ điểm danh, điểm kiểm tra), xếp theo năm học → học kỳ, mới nhất trước.
    /// </summary>
    public async Task<List<HocKyCaNhan>> LayBangDiemToanKhoaCuaToiAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaHocVien) return [];
        await using var db = csdl.CreateDbContext();
        var tenCd = await db.ChuyenDe.ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var soTiet = await db.ChuyenDe.ToDictionaryAsync(c => c.MaChuyenDe, c => c.SoTiet);
        int namNhap; lock (kho.KhoaDongBo) namNhap = int.TryParse(kho.DanhSachLop.FirstOrDefault(l => l.MaLop == LopCuaHocVien(nd.MaHocVien))?.NienKhoa.Split('–')[0], out var n) ? n : 0;
        int ThuTu(string namHoc, int hk) => namNhap == 0 || !int.TryParse(namHoc[..4], out var d) ? 0 : (d - namNhap) * 2 + hk;
        var luuTru = await db.KetQuaLuuTru.AsNoTracking().Where(k => k.MaHocVien == nd.MaHocVien).ToListAsync();
        var ketQua = luuTru.GroupBy(k => (k.NamHoc, k.HocKy)).Select(g => new HocKyCaNhan(g.Key.NamHoc, g.Key.HocKy, false,
            g.Select(k => new KetQuaCaNhan(k.MaChuyenDe, tenCd.GetValueOrDefault(k.MaChuyenDe, k.MaChuyenDe), k.ChuyenCan, 0, k.DiemKiemTra, k.TongKet, QuyTacHocVu.XepLoai(k.TongKet),
                k.TongKet is null ? "Thiếu điểm kiểm tra (vắng thi, chờ thi lại)." : null)).ToList(),
            g.ToDictionary(k => k.MaChuyenDe, k => k.NgayThi), ThuTu(g.Key.NamHoc, g.Key.HocKy), soTiet)).ToList();
        var hienTai = await LayKetQuaCuaToiAsync();
        if (hienTai.Count > 0)
        {
            var (namHoc, hk) = QuyTacHocVu.NamHocHocKy(BayGio());
            var lich = await db.LichThi.AsNoTracking().ToListAsync();
            ketQua.Add(new HocKyCaNhan(namHoc, hk, true, hienTai, hienTai.ToDictionary(m => m.MaChuyenDe, m => lich.FirstOrDefault(l => l.MaChuyenDe == m.MaChuyenDe && l.MaLop == LopCuaHocVien(nd.MaHocVien))?.Ngay), ThuTu(namHoc, hk), soTiet));
        }
        return ketQua.OrderByDescending(h => h.NamHoc).ThenByDescending(h => h.HocKy).ToList();
    }

    /// <summary>Kết quả mọi chuyên đề của học viên đang đăng nhập.</summary>
    public async Task<List<KetQuaCaNhan>> LayKetQuaCuaToiAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var maLop = LopCuaHocVien(nd.MaHocVien);
        if (!nd.LaHocVien || maLop is null) return [];
        await using var db = csdl.CreateDbContext();
        var ketQua = new List<KetQuaCaNhan>();
        foreach (var cd in await LayChuyenDeCuaLopAsync(maLop))
        {
            var dong = (await TinhBangDiemAsync(db, maLop, cd.MaChuyenDe)).FirstOrDefault(d => d.MaHocVien == nd.MaHocVien);
            if (dong is not null) ketQua.Add(new(cd.MaChuyenDe, cd.TenChuyenDe, dong.ChuyenCan, dong.SoBuoiDaDiemDanh, dong.DiemKiemTra, dong.TongKet, dong.XepLoai, dong.LyDoThieu));
        }
        return ketQua;
    }

    // ---------- Sổ đầu bài online ----------

    public sealed record DongSoDauBai(BuoiHoc Buoi, string TenChuyenDe, string TenGiangVien, int SiSo, int CoMat, SoDauBai? Ghi, bool DuocGhi);

    /// <summary>
    /// Sổ đầu bài của lớp: mỗi buổi đã diễn ra một dòng (nội dung, nhận xét, xếp loại tiết, sĩ số có mặt từ điểm danh).
    /// Xem: Admin, Quản lý học viên (theo dõi lớp), Quản lý đào tạo (tiến độ chương trình), Quản lý khoa (chất lượng giảng dạy), giảng viên dạy lớp.
    /// Ghi: giảng viên dạy buổi đó hoặc Admin. Không chứa dữ liệu cá nhân học viên.
    /// </summary>
    public async Task<(List<DongSoDauBai> Dong, string? Loi)> LaySoDauBaiAsync(string maLop)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var duocXem = nd.LaAdmin || nd.LaQuanLy("Lop") || nd.LaQuanLy("DaoTao") || nd.LaQuanLy("Khoa") || (nd.LaGiangVien && kho.LopGiangVienDay(nd.MaCanBo!).Contains(maLop));
        if (!duocXem) return ([], "Vai trò của bạn không xem sổ đầu bài của lớp này.");
        await using var db = csdl.CreateDbContext();
        var homNay = BayGio().Date;
        var buoi = await db.BuoiHoc.AsNoTracking().Where(b => b.MaLop == maLop && b.Ngay <= homNay).OrderByDescending(b => b.Ngay).ThenByDescending(b => b.TietBatDau).ToListAsync();
        var ids = buoi.Select(b => b.Id).ToList();
        var ghi = await db.SoDauBai.AsNoTracking().Where(s => ids.Contains(s.BuoiHocId)).ToDictionaryAsync(s => s.BuoiHocId);
        var coMat = (await db.DiemDanh.Where(d => ids.Contains(d.BuoiHocId) && (d.TrangThai == TrangThaiDiemDanh.CoMat || d.TrangThai == TrangThaiDiemDanh.DiMuon)).Select(d => d.BuoiHocId).ToListAsync())
            .GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var tenCd = await db.ChuyenDe.ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var siSo = HocVienCuaLop(maLop).Count;
        string TenGv(string ma) { lock (kho.KhoaDongBo) return kho.DanhSachCanBo.FirstOrDefault(c => c.MaCanBo == ma) is { } c ? $"{c.CapBacHam} {c.HoTen}" : ma; }
        return (buoi.Select(b => new DongSoDauBai(b, tenCd.GetValueOrDefault(b.MaChuyenDe, b.MaChuyenDe), TenGv(b.MaCanBo), siSo, coMat.GetValueOrDefault(b.Id),
            ghi.GetValueOrDefault(b.Id), nd.LaAdmin || (nd.LaGiangVien && nd.MaCanBo == b.MaCanBo))).ToList(), null);
    }

    public async Task<KetQuaThaoTac> GhiSoDauBaiAsync(int buoiHocId, string noiDung, string nhanXet, string xepLoai)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var b = await db.BuoiHoc.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buoiHocId);
        if (b is null) return new(false, "Không tìm thấy buổi học.");
        if (!(nd.LaAdmin || (nd.LaGiangVien && nd.MaCanBo == b.MaCanBo))) return new(false, "Chỉ giảng viên dạy buổi này (hoặc Admin) được ghi sổ đầu bài.");
        if (b.Ngay.Date > BayGio().Date) return new(false, "Buổi học chưa diễn ra.");
        noiDung = (noiDung ?? "").Trim(); nhanXet = (nhanXet ?? "").Trim();
        if (noiDung.Length is 0 or > 500) return new(false, "Nội dung bài giảng cần 1–500 ký tự.");
        if (nhanXet.Length > 500) return new(false, "Nhận xét tối đa 500 ký tự.");
        if (!SoDauBai.CacXepLoai.Contains(xepLoai)) return new(false, "Xếp loại tiết học phải là A, B, C hoặc D.");
        var cu = await db.SoDauBai.FindAsync(buoiHocId);
        if (cu is null) db.SoDauBai.Add(new SoDauBai { BuoiHocId = buoiHocId, NoiDung = noiDung, NhanXet = nhanXet, XepLoaiTiet = xepLoai, NguoiGhi = nd.Ten, ThoiDiem = DateTimeOffset.Now });
        else { cu.NoiDung = noiDung; cu.NhanXet = nhanXet; cu.XepLoaiTiet = xepLoai; cu.NguoiGhi = nd.Ten; cu.ThoiDiem = DateTimeOffset.Now; }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, "Đã ghi sổ đầu bài.");
    }

    /// <summary>Sinh sổ đầu bài mẫu cho các buổi đã qua (nội dung giả lập).</summary>
    public static void KhoiTaoSoDauBai(CoSoDuLieuNghiepVu db, DateTime homNay)
    {
        if (db.SoDauBai.Any()) return;
        var tenCd = db.ChuyenDe.ToDictionary(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var buoi = db.BuoiHoc.AsNoTracking().Where(b => b.Ngay < homNay).OrderBy(b => b.Ngay).ToList();
        var thuTu = new Dictionary<(string, string), int>();
        string[] nhanXet = ["Lớp chú ý, phát biểu xây dựng bài tốt.", "Lớp nghiêm túc, cần chuẩn bị bài trước khi lên lớp.", "Một số học viên đi muộn, đã nhắc nhở.", "Thảo luận nhóm sôi nổi, đạt mục tiêu bài học."];
        db.BoQuaNhatKy = true;
        foreach (var b in buoi)
        {
            var khoa = (b.MaLop, b.MaChuyenDe);
            thuTu[khoa] = thuTu.GetValueOrDefault(khoa) + 1;
            var h = (b.Id * 7 + b.MaLop.Length) % 10;
            db.SoDauBai.Add(new SoDauBai
            {
                BuoiHocId = b.Id, NoiDung = $"Bài {thuTu[khoa]}: {tenCd.GetValueOrDefault(b.MaChuyenDe)} – nội dung giảng dạy mẫu.",
                NhanXet = nhanXet[h % nhanXet.Length], XepLoaiTiet = h < 6 ? "A" : h < 9 ? "B" : "C", NguoiGhi = b.MaCanBo, ThoiDiem = new DateTimeOffset(QuyTacHocVu.KetThuc(b))
            });
        }
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }

    /// <summary>Số liệu tổng hợp học vụ cho trang chủ (không chứa dữ liệu từng người): buổi chưa điểm danh, học viên thiếu điểm.</summary>
    public async Task<(int BuoiHomNay, int BuoiChuaDiemDanh, int SoBuoiTuanNay)> LayTongHopAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap) return (0, 0, 0);
        var homNay = BayGio().Date;
        var dauTuan = QuyTacHocVu.DauTuan(homNay);
        await using var db = csdl.CreateDbContext();
        var q = db.BuoiHoc.AsQueryable();
        if (nd.LaGiangVien) q = q.Where(b => b.MaCanBo == nd.MaCanBo);
        if (nd.LaHocVien) { var lop = LopCuaHocVien(nd.MaHocVien); q = q.Where(b => b.MaLop == lop); }
        var homNaySo = await q.CountAsync(b => b.Ngay == homNay);
        var tuanSo = await q.CountAsync(b => b.Ngay >= dauTuan && b.Ngay < dauTuan.AddDays(7));
        var chua = await q.Where(b => b.Ngay < homNay && b.Ngay >= dauTuan.AddDays(-7)).CountAsync(b => !db.DiemDanh.Any(d => d.BuoiHocId == b.Id));
        return (homNaySo, chua, tuanSo);
    }
}
