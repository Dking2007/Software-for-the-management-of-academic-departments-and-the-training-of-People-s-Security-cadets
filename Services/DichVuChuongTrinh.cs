using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Theo ghi chú nghiệp vụ của nhóm:
/// - Quản lý chương trình: khoa nào phụ trách giảng dạy lớp nào, trong thời gian nào; lớp đang học gì; phân công giảng dạy (lớp mới → phân công).
/// - Quản lý học viên: lớp đi học / lớp nghỉ hôm nay; buổi học, buổi nghỉ của từng học viên.
/// - Xét tư cách thi: Quản lý học viên đề xuất → Khoa xác nhận → Phòng đào tạo duyệt; lịch thi do Phòng đào tạo sắp xếp.
/// - Điểm danh đi làm (chấm công) của cán bộ.
/// </summary>
public sealed class DichVuChuongTrinh(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen, DichVuDaoTao daoTao, DichVuThongBao thongBao)
{
    /// <summary>Đủ tư cách thi khi số buổi vắng (có phép + không phép) không quá 20% số buổi đã điểm danh.</summary>
    public const double TiLeVangToiDa = 0.2;

    public sealed record DongChuongTrinh(string MaLop, string MaChuyenDe, string TenChuyenDe, string MaKhoa, string TenKhoa, string? MaCanBo, string TenGiangVien,
        DateTime? TuNgay, DateTime? DenNgay, int SoBuoiDaHoc, int TongBuoi, string TrangThai);
    public sealed record DongChuyenCan(string MaHocVien, string HoTen, int SoBuoi, int CoMat, int DiMuon, int VangCoPhep, int VangKhongPhep)
    {
        public int SoVang => VangCoPhep + VangKhongPhep;
        public double TiLeVang => SoBuoi == 0 ? 0 : (double)SoVang / SoBuoi;
    }
    public sealed record DongTuCach(string MaHocVien, string HoTen, int SoBuoi, int SoVang, bool Du, string? LyDo, string? DieuChinh = null);
    public sealed record HoSoTuCach(XetTuCachThi Ho, List<DongTuCach> Dong, bool DuocXemTungNguoi, bool DuocGui, bool DuocXacNhan, bool DuocDuyet, bool DuocTraLai, bool DuocXepLichThi, LichThi? Lich,
        BaiThi? BaiThi = null, List<LichSuXetTuCach>? LichSu = null, bool DuocDieuChinh = false, bool DuocGuiThongBao = false);
    public sealed record LichThiCuaToi(LichThi Lich, string TenChuyenDe, bool DuTuCach, string? LyDo, BaiThi? BaiThi, string TenPhong);

    private CoSoDuLieuNghiepVu MoCsdl(NguoiDungHienTai nd) { var db = csdl.CreateDbContext(); db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro; return db; }
    private Dictionary<string, string> TenKhoa() { lock (kho.KhoaDongBo) return kho.DanhSachKhoa.ToDictionary(k => k.MaKhoa, k => k.TenKhoa); }
    private List<HocVien> HocVienLop(string maLop) { lock (kho.KhoaDongBo) return kho.DanhSachHocVien.Where(h => h.MaLop == maLop).OrderBy(h => h.KhoaSapXepTen, StringComparer.Ordinal).Select(h => h.TaoBanSao()).ToList(); }

    // ---------- Chương trình & phân công ----------

    /// <summary>Sinh phân công ban đầu từ lịch đã có (giảng viên dạy nhiều buổi nhất cho mỗi lớp – chuyên đề).</summary>
    public static void KhoiTaoPhanCong(CoSoDuLieuNghiepVu db)
    {
        if (db.PhanCong.Any()) return;
        var cap = db.BuoiHoc.AsNoTracking().Select(b => new { b.MaLop, b.MaChuyenDe, b.MaCanBo }).ToList()
            .GroupBy(x => (x.MaLop, x.MaChuyenDe))
            .Select(g => new PhanCongGiangDay { MaLop = g.Key.MaLop, MaChuyenDe = g.Key.MaChuyenDe, MaCanBo = g.GroupBy(x => x.MaCanBo).OrderByDescending(x => x.Count()).First().Key });
        db.BoQuaNhatKy = true;
        db.PhanCong.AddRange(cap);
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }

    /// <summary>Chương trình của các lớp: mỗi dòng là lớp – chuyên đề với khoa phụ trách, giảng viên, thời gian, tiến độ, trạng thái.</summary>
    public async Task<List<DongChuongTrinh>> LayChuongTrinhAsync(string? maLop = null, string? maKhoa = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || nd.VaiTro == "Guest") return [];
        var lopDuocXem = (await Task.FromResult(new DichVuQuanLyDuLieuDemo(kho, phanQuyen).LayDanhSachLop())).Select(l => l.MaLop).ToHashSet();
        await using var db = await csdl.CreateDbContextAsync();
        var homNay = DateTime.Today;
        var buoi = await db.BuoiHoc.AsNoTracking().Where(b => maLop == null || b.MaLop == maLop).Select(b => new { b.MaLop, b.MaChuyenDe, b.MaCanBo, b.Ngay }).ToListAsync();
        var pc = await db.PhanCong.AsNoTracking().Where(p => maLop == null || p.MaLop == maLop).ToListAsync();
        var cd = await db.ChuyenDe.AsNoTracking().ToDictionaryAsync(c => c.MaChuyenDe);
        var tenKhoa = TenKhoa();
        var cap = buoi.Select(b => (b.MaLop, b.MaChuyenDe)).Concat(pc.Select(p => (p.MaLop, p.MaChuyenDe))).Distinct();
        var ketQua = new List<DongChuongTrinh>();
        foreach (var (l, c) in cap)
        {
            if (!lopDuocXem.Contains(l) || !cd.TryGetValue(c, out var chuyenDe)) continue;
            if (maKhoa is not null && chuyenDe.MaKhoa != maKhoa) continue;
            var cacBuoi = buoi.Where(b => b.MaLop == l && b.MaChuyenDe == c).ToList();
            var maCb = pc.FirstOrDefault(p => p.MaLop == l && p.MaChuyenDe == c)?.MaCanBo ?? cacBuoi.GroupBy(b => b.MaCanBo).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            DateTime? tu = cacBuoi.Count > 0 ? cacBuoi.Min(b => b.Ngay) : null, den = cacBuoi.Count > 0 ? cacBuoi.Max(b => b.Ngay) : null;
            var daHoc = cacBuoi.Count(b => b.Ngay <= homNay);
            var trangThai = cacBuoi.Count == 0 ? "Chưa xếp lịch" : den < homNay ? "Đã xong" : tu > homNay ? "Sắp học" : "Đang học";
            ketQua.Add(new(l, c, chuyenDe.TenChuyenDe, chuyenDe.MaKhoa, tenKhoa.GetValueOrDefault(chuyenDe.MaKhoa, chuyenDe.MaKhoa), maCb, maCb is null ? "Chưa phân công" : daoTao.TenCanBo(maCb), tu, den, daHoc, cacBuoi.Count, trangThai));
        }
        return ketQua.OrderBy(x => x.MaLop.Length).ThenBy(x => x.MaLop).ThenBy(x => x.TuNgay ?? DateTime.MaxValue).ToList();
    }

    public async Task<KetQuaThaoTac> LuuPhanCongAsync(string maLop, string maChuyenDe, string maCanBo)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return new(false, "Chỉ Admin và Quản lý đào tạo được phân công giảng dạy.");
        lock (kho.KhoaDongBo) if (!kho.DanhSachLop.Any(l => l.MaLop == maLop)) return new(false, $"Lớp {maLop} không tồn tại.");
        await using var db = MoCsdl(nd);
        if (!await db.ChuyenDe.AnyAsync(c => c.MaChuyenDe == maChuyenDe)) return new(false, "Chuyên đề không tồn tại.");
        if (!await db.ChuyenMonGiangDay.AnyAsync(m => m.MaCanBo == maCanBo && m.MaChuyenDe == maChuyenDe))
            return new(false, $"{daoTao.TenCanBo(maCanBo)} chưa có trong danh sách đủ điều kiện dạy chuyên đề này. Quản lý khoa cần bổ sung chuyên môn trước.");
        var cu = await db.PhanCong.FindAsync(maLop, maChuyenDe);
        if (cu is null) db.PhanCong.Add(new PhanCongGiangDay { MaLop = maLop, MaChuyenDe = maChuyenDe, MaCanBo = maCanBo });
        else cu.MaCanBo = maCanBo;
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã phân công {daoTao.TenCanBo(maCanBo)} dạy lớp {maLop}.");
    }

    public async Task<KetQuaThaoTac> XoaPhanCongAsync(string maLop, string maChuyenDe)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return new(false, "Chỉ Admin và Quản lý đào tạo được phân công giảng dạy.");
        await using var db = MoCsdl(nd);
        var cu = await db.PhanCong.FindAsync(maLop, maChuyenDe);
        if (cu is null) return new(false, "Không có phân công này.");
        db.PhanCong.Remove(cu);
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, "Đã bỏ phân công.");
    }

    public async Task<string?> LayGiangVienPhanCongAsync(string maLop, string maChuyenDe)
    {
        await using var db = await csdl.CreateDbContextAsync();
        return (await db.PhanCong.FindAsync(maLop, maChuyenDe))?.MaCanBo;
    }

    /// <summary>Lớp khoá mới (D58, K6, liên thông) chưa có phân công giảng dạy nào.</summary>
    public async Task<List<string>> LopChuaPhanCongAsync()
    {
        await using var db = await csdl.CreateDbContextAsync();
        var coPc = (await db.PhanCong.Select(p => p.MaLop).Distinct().ToListAsync()).ToHashSet();
        lock (kho.KhoaDongBo) return kho.DanhSachLop.Where(l => (l.KhoaHoc is "D58" or "K6" || l.HeDaoTao == "Liên thông") && !coPc.Contains(l.MaLop)).Select(l => l.MaLop).ToList();
    }

    // ---------- Lớp đi học / lớp nghỉ, chuyên cần ----------

    public async Task<(List<BuoiHocHienThi> CoLich, List<LopDaoTao> Nghi)> LayHomNayAsync(DateTime? ngay = null)
    {
        var d = (ngay ?? DateTime.Today).Date;
        var lich = await daoTao.LayLichAsync(d, d);
        var coLich = lich.Select(b => b.Buoi.MaLop).ToHashSet();
        var lop = new DichVuQuanLyDuLieuDemo(kho, phanQuyen).LayDanhSachLop();
        return (lich, lop.Where(l => !coLich.Contains(l.MaLop)).ToList());
    }

    /// <summary>Buổi học / buổi nghỉ của từng học viên trong lớp (mọi chuyên đề), tính từ điểm danh.</summary>
    public async Task<(List<DongChuyenCan> Dong, string? Loi)> LayChuyenCanLopAsync(string maLop)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var duoc = nd.CoQuyenVung("Lop") || (nd.LaGiangVien && kho.LopGiangVienDay(nd.MaCanBo!).Contains(maLop));
        if (!duoc) return ([], "Chỉ Quản lý học viên, Admin hoặc giảng viên dạy lớp được xem chuyên cần từng học viên.");
        await using var db = await csdl.CreateDbContextAsync();
        var idBuoi = await db.BuoiHoc.Where(b => b.MaLop == maLop && b.Ngay <= DateTime.Today).Select(b => b.Id).ToListAsync();
        var dd = await db.DiemDanh.AsNoTracking().Where(x => idBuoi.Contains(x.BuoiHocId)).Select(x => new { x.MaHocVien, x.TrangThai }).ToListAsync();
        var theoHv = dd.GroupBy(x => x.MaHocVien).ToDictionary(g => g.Key, g => g.Select(x => x.TrangThai).ToList());
        return (HocVienLop(maLop).Select(h =>
        {
            var tt = theoHv.GetValueOrDefault(h.MaHocVien, []);
            return new DongChuyenCan(h.MaHocVien, h.HoTen, tt.Count, tt.Count(x => x == TrangThaiDiemDanh.CoMat), tt.Count(x => x == TrangThaiDiemDanh.DiMuon),
                tt.Count(x => x == TrangThaiDiemDanh.VangCoPhep), tt.Count(x => x == TrangThaiDiemDanh.VangKhongPhep));
        }).ToList(), null);
    }

    // ---------- Xét tư cách thi ----------

    public static (bool Du, string? LyDo) XetTuCach(int soBuoi, int soVang)
    {
        if (soBuoi == 0) return (false, "Chưa có buổi học nào được điểm danh.");
        var tiLe = (double)soVang / soBuoi;
        return tiLe <= TiLeVangToiDa ? (true, null) : (false, $"Vắng {soVang}/{soBuoi} buổi ({tiLe:P0}), vượt mức tối đa {TiLeVangToiDa:P0}.");
    }

    public async Task<HoSoTuCach> LayHoSoTuCachAsync(string maLop, string maChuyenDe)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = await csdl.CreateDbContextAsync();
        var ho = await db.XetTuCach.AsNoTracking().FirstOrDefaultAsync(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe)
                 ?? new XetTuCachThi { MaLop = maLop, MaChuyenDe = maChuyenDe };
        var idBuoi = await db.BuoiHoc.Where(b => b.MaLop == maLop && b.MaChuyenDe == maChuyenDe && b.Ngay <= DateTime.Today).Select(b => b.Id).ToListAsync();
        var dd = await db.DiemDanh.AsNoTracking().Where(x => idBuoi.Contains(x.BuoiHocId)).Select(x => new { x.MaHocVien, x.TrangThai }).ToListAsync();
        var theoHv = dd.GroupBy(x => x.MaHocVien).ToDictionary(g => g.Key, g => g.ToList());
        var daChot = ho.TrangThai is not (TrangThaiXetTuCach.Nhap or TrangThaiXetTuCach.TraLai)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(ho.KhongDuTuCach) ?? [] : null;
        var dieuChinh = await db.DieuChinhTuCach.AsNoTracking().Where(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe).ToDictionaryAsync(x => x.MaHocVien);
        var dong = HocVienLop(maLop).Select(h =>
        {
            var tt = theoHv.GetValueOrDefault(h.MaHocVien, []);
            var soVang = tt.Count(x => x.TrangThai is TrangThaiDiemDanh.VangCoPhep or TrangThaiDiemDanh.VangKhongPhep);
            var (du, lyDo) = daChot is null ? XetTuCach(tt.Count, soVang) : (!daChot.ContainsKey(h.MaHocVien), daChot.GetValueOrDefault(h.MaHocVien));
            string? ghiChu = null;
            if (dieuChinh.TryGetValue(h.MaHocVien, out var dc))
            {
                ghiChu = $"{(dc.Du ? "Miễn trừ" : "Không đủ")} theo điều chỉnh của {dc.NguoiDieuChinh}: {dc.LyDo}";
                if (daChot is null) { du = dc.Du; lyDo = dc.Du ? null : dc.LyDo; }
            }
            return new DongTuCach(h.MaHocVien, h.HoTen, tt.Count, soVang, du, lyDo, ghiChu);
        }).ToList();
        // Quản lý học viên, Admin thấy cả lớp; khoa và phòng đào tạo chỉ thấy danh sách không đủ tư cách (tối thiểu cần để xác nhận, duyệt).
        var xemTungNguoi = nd.CoQuyenVung("Lop");
        if (!xemTungNguoi) dong = (nd.LaQuanLy("Khoa") || nd.LaQuanLy("DaoTao")) ? dong.Where(d => !d.Du).ToList() : [];
        var lich = await db.LichThi.AsNoTracking().FirstOrDefaultAsync(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe);
        var baiThi = await db.BaiThi.AsNoTracking().FirstOrDefaultAsync(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe);
        var lichSu = (await db.LichSuXetTuCach.AsNoTracking().Where(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe).ToListAsync()).OrderByDescending(x => x.ThoiDiem).ToList();
        return new(ho, dong, xemTungNguoi,
            DuocGui: nd.CoQuyenVung("Lop") && ho.TrangThai is TrangThaiXetTuCach.Nhap or TrangThaiXetTuCach.TraLai && idBuoi.Count > 0,
            DuocXacNhan: (nd.LaAdmin || nd.LaQuanLy("Khoa")) && ho.TrangThai == TrangThaiXetTuCach.ChoKhoa,
            DuocDuyet: nd.CoQuyenVung("DaoTao") && ho.TrangThai == TrangThaiXetTuCach.ChoDaoTao,
            DuocTraLai: ((nd.LaAdmin || nd.LaQuanLy("Khoa")) && ho.TrangThai == TrangThaiXetTuCach.ChoKhoa) || (nd.CoQuyenVung("DaoTao") && ho.TrangThai == TrangThaiXetTuCach.ChoDaoTao),
            DuocXepLichThi: nd.CoQuyenVung("DaoTao") && ho.TrangThai == TrangThaiXetTuCach.DaDuyet,
            lich, baiThi, lichSu,
            DuocDieuChinh: nd.CoQuyenVung("Lop") && ho.TrangThai is TrangThaiXetTuCach.Nhap or TrangThaiXetTuCach.TraLai,
            DuocGuiThongBao: nd.CoQuyenVung("DaoTao") && ho.TrangThai == TrangThaiXetTuCach.DaDuyet && lich is not null && baiThi is not null);
    }

    /// <summary>Danh sách lớp – chuyên đề đã học (có buổi đã qua) kèm trạng thái xét tư cách, cho màn hình tổng hợp.</summary>
    public async Task<List<(string MaLop, string MaChuyenDe, string TenChuyenDe, string TenKhoa, string TrangThai, bool CoLichThi)>> LayDanhSachXetAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || nd.VaiTro is "Guest" or "HocVien") return [];
        var lopDuocXem = new DichVuQuanLyDuLieuDemo(kho, phanQuyen).LayDanhSachLop().Select(l => l.MaLop).ToHashSet();
        await using var db = await csdl.CreateDbContextAsync();
        var cap = await db.BuoiHoc.Where(b => b.Ngay <= DateTime.Today).Select(b => new { b.MaLop, b.MaChuyenDe }).Distinct().ToListAsync();
        var ho = await db.XetTuCach.AsNoTracking().ToDictionaryAsync(x => (x.MaLop, x.MaChuyenDe));
        var coLich = (await db.LichThi.Select(x => new { x.MaLop, x.MaChuyenDe }).ToListAsync()).Select(x => (x.MaLop, x.MaChuyenDe)).ToHashSet();
        var cd = await db.ChuyenDe.AsNoTracking().ToDictionaryAsync(c => c.MaChuyenDe);
        var tenKhoa = TenKhoa();
        return cap.Where(c => lopDuocXem.Contains(c.MaLop) && cd.ContainsKey(c.MaChuyenDe))
            .Select(c => (c.MaLop, c.MaChuyenDe, cd[c.MaChuyenDe].TenChuyenDe, tenKhoa.GetValueOrDefault(cd[c.MaChuyenDe].MaKhoa, ""),
                ho.TryGetValue((c.MaLop, c.MaChuyenDe), out var h) ? h.TrangThai : TrangThaiXetTuCach.Nhap, coLich.Contains((c.MaLop, c.MaChuyenDe))))
            .OrderBy(x => x.MaLop.Length).ThenBy(x => x.MaLop).ThenBy(x => x.MaChuyenDe).ToList();
    }

    private async Task<KetQuaThaoTac> ChuyenTrangThaiAsync(string maLop, string maChuyenDe, Func<NguoiDungHienTai, XetTuCachThi, string?> kiemTra, Action<NguoiDungHienTai, XetTuCachThi> lam, string thanhCong, string hanhDong = "", string ghiChu = "")
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var ho = await db.XetTuCach.FindAsync(maLop, maChuyenDe);
        var moi = ho is null;
        ho ??= new XetTuCachThi { MaLop = maLop, MaChuyenDe = maChuyenDe };
        if (kiemTra(nd, ho) is { } loi) return new(false, loi);
        lam(nd, ho);
        ho.CapNhatLuc = DateTimeOffset.Now;
        if (moi) db.XetTuCach.Add(ho);
        if (hanhDong != "") db.LichSuXetTuCach.Add(new LichSuXetTuCach { MaLop = maLop, MaChuyenDe = maChuyenDe, HanhDong = hanhDong, NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro, ThoiDiem = DateTimeOffset.Now, GhiChu = ghiChu });
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, thanhCong);
    }

    public async Task<KetQuaThaoTac> GuiXetTuCachAsync(string maLop, string maChuyenDe)
    {
        var hs = await LayHoSoTuCachAsync(maLop, maChuyenDe);
        if (!hs.DuocGui) return new(false, "Chỉ Quản lý học viên/Admin gửi được hồ sơ chưa gửi hoặc bị trả lại, và chuyên đề phải có buổi đã học.");
        var khongDu = hs.Dong.Where(d => !d.Du).ToDictionary(d => d.MaHocVien, d => d.LyDo ?? "");
        return await ChuyenTrangThaiAsync(maLop, maChuyenDe, (_, _) => null, (nd, ho) =>
        {
            ho.TrangThai = TrangThaiXetTuCach.ChoKhoa; ho.KhongDuTuCach = JsonSerializer.Serialize(khongDu); ho.SoHocVien = hs.Dong.Count;
            ho.NguoiDeXuat = nd.Ten; ho.NguoiXacNhan = ""; ho.NguoiDuyet = ""; ho.GhiChu = "";
        }, $"Đã gửi khoa xác nhận: {hs.Dong.Count - khongDu.Count}/{hs.Dong.Count} học viên đủ tư cách thi.",
            "Gửi đề xuất", $"{hs.Dong.Count - khongDu.Count}/{hs.Dong.Count} đủ tư cách{(hs.Dong.Any(d => d.DieuChinh is not null) ? $", {hs.Dong.Count(d => d.DieuChinh is not null)} điều chỉnh" : "")}");
    }

    public Task<KetQuaThaoTac> XacNhanXetTuCachAsync(string maLop, string maChuyenDe)
        => ChuyenTrangThaiAsync(maLop, maChuyenDe,
            (nd, ho) => !(nd.LaAdmin || nd.LaQuanLy("Khoa")) ? "Chỉ Quản lý khoa/Admin xác nhận." : ho.TrangThai != TrangThaiXetTuCach.ChoKhoa ? "Hồ sơ không ở bước chờ khoa xác nhận." : null,
            (nd, ho) => { ho.TrangThai = TrangThaiXetTuCach.ChoDaoTao; ho.NguoiXacNhan = nd.Ten; }, "Khoa đã xác nhận, chuyển Phòng đào tạo duyệt.", "Khoa xác nhận");

    public Task<KetQuaThaoTac> DuyetXetTuCachAsync(string maLop, string maChuyenDe)
        => ChuyenTrangThaiAsync(maLop, maChuyenDe,
            (nd, ho) => !nd.CoQuyenVung("DaoTao") ? "Chỉ Phòng đào tạo (Quản lý đào tạo)/Admin duyệt." : ho.TrangThai != TrangThaiXetTuCach.ChoDaoTao ? "Hồ sơ không ở bước chờ đào tạo duyệt." : null,
            (nd, ho) => { ho.TrangThai = TrangThaiXetTuCach.DaDuyet; ho.NguoiDuyet = nd.Ten; }, "Đã duyệt danh sách đủ tư cách thi. Có thể xếp lịch thi.", "Đào tạo duyệt");

    public Task<KetQuaThaoTac> TraLaiXetTuCachAsync(string maLop, string maChuyenDe, string lyDo)
        => ChuyenTrangThaiAsync(maLop, maChuyenDe,
            (nd, ho) => string.IsNullOrWhiteSpace(lyDo) ? "Vui lòng ghi lý do trả lại."
                : ho.TrangThai == TrangThaiXetTuCach.ChoKhoa && (nd.LaAdmin || nd.LaQuanLy("Khoa")) ? null
                : ho.TrangThai == TrangThaiXetTuCach.ChoDaoTao && nd.CoQuyenVung("DaoTao") ? null
                : "Bạn không trả lại được hồ sơ ở bước này.",
            (nd, ho) => { ho.TrangThai = TrangThaiXetTuCach.TraLai; ho.GhiChu = $"{nd.TenVaiTro} trả lại: {lyDo.Trim()}"; }, "Đã trả lại hồ sơ cho Quản lý học viên.", "Trả lại", lyDo.Trim());

    /// <summary>Quản lý học viên điều chỉnh tư cách một học viên trước khi gửi (bắt buộc lý do); du = null để bỏ điều chỉnh.</summary>
    public async Task<KetQuaThaoTac> DieuChinhTuCachAsync(string maLop, string maChuyenDe, string maHocVien, bool? du, string lyDo)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("Lop")) return new(false, "Chỉ Quản lý học viên/Admin điều chỉnh tư cách thi.");
        await using var db = MoCsdl(nd);
        var ho = await db.XetTuCach.FindAsync(maLop, maChuyenDe);
        if (ho is not null && ho.TrangThai is not (TrangThaiXetTuCach.Nhap or TrangThaiXetTuCach.TraLai)) return new(false, "Hồ sơ đã gửi; chỉ điều chỉnh khi chưa gửi hoặc bị trả lại.");
        if (!HocVienLop(maLop).Any(h => h.MaHocVien == maHocVien)) return new(false, "Học viên không thuộc lớp.");
        var cu = await db.DieuChinhTuCach.FindAsync(maLop, maChuyenDe, maHocVien);
        if (du is null)
        {
            if (cu is null) return new(true, "Không có điều chỉnh để bỏ.");
            db.DieuChinhTuCach.Remove(cu);
        }
        else
        {
            lyDo = (lyDo ?? "").Trim();
            if (lyDo.Length is < 5 or > 300) return new(false, "Lý do điều chỉnh cần 5–300 ký tự (ví dụ: vắng do đi công tác theo quyết định số …).");
            if (cu is null) db.DieuChinhTuCach.Add(new DieuChinhTuCach { MaLop = maLop, MaChuyenDe = maChuyenDe, MaHocVien = maHocVien, Du = du.Value, LyDo = lyDo, NguoiDieuChinh = nd.Ten, ThoiDiem = DateTimeOffset.Now });
            else { cu.Du = du.Value; cu.LyDo = lyDo; cu.NguoiDieuChinh = nd.Ten; cu.ThoiDiem = DateTimeOffset.Now; }
        }
        db.LichSuXetTuCach.Add(new LichSuXetTuCach { MaLop = maLop, MaChuyenDe = maChuyenDe, HanhDong = du is null ? "Bỏ điều chỉnh" : du.Value ? "Miễn trừ tư cách" : "Đánh dấu không đủ", NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro, ThoiDiem = DateTimeOffset.Now, GhiChu = $"{maHocVien}{(du is null ? "" : ": " + lyDo)}" });
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, du is null ? "Đã bỏ điều chỉnh." : "Đã lưu điều chỉnh tư cách thi.");
    }

    /// <summary>Xử lý hàng loạt các hồ sơ đang lọc theo đúng bước của vai trò: "gui" (QLHV), "xac-nhan" (Khoa), "duyet" (Đào tạo).</summary>
    public async Task<(int ThanhCong, int BoQua)> XuLyHangLoatAsync(string buoc, IEnumerable<(string MaLop, string MaChuyenDe)> cacHoSo)
    {
        int ok = 0, bo = 0;
        foreach (var (lop, cd) in cacHoSo.Distinct().ToList())
        {
            var kq = buoc switch { "gui" => await GuiXetTuCachAsync(lop, cd), "xac-nhan" => await XacNhanXetTuCachAsync(lop, cd), "duyet" => await DuyetXetTuCachAsync(lop, cd), _ => new(false, "") };
            if (kq.ThanhCong) ok++; else bo++;
        }
        return (ok, bo);
    }

    // ---------- Thông tin bài thi và thông báo tới học viên ----------

    public async Task<KetQuaThaoTac> LuuBaiThiAsync(BaiThi b)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return new(false, "Thông tin bài thi do Phòng đào tạo (Quản lý đào tạo)/Admin nhập.");
        await using var db = MoCsdl(nd);
        var ho = await db.XetTuCach.FindAsync(b.MaLop, b.MaChuyenDe);
        if (ho?.TrangThai != TrangThaiXetTuCach.DaDuyet) return new(false, "Chỉ nhập thông tin bài thi khi danh sách tư cách thi đã được duyệt.");
        b.CauTrucDe = (b.CauTrucDe ?? "").Trim(); b.PhamViOnTap = (b.PhamViOnTap ?? "").Trim(); b.CanBoCoiThi = (b.CanBoCoiThi ?? "").Trim(); b.LuuY = (b.LuuY ?? "").Trim();
        if (b.ThoiGianLamBai is < 15 or > 240) return new(false, "Thời gian làm bài từ 15 đến 240 phút.");
        if (b.CauTrucDe.Length is 0 or > 500) return new(false, "Cấu trúc đề cần 1–500 ký tự (ví dụ: 3 câu tự luận, câu 1 – 3 điểm …).");
        if (b.PhamViOnTap.Length is 0 or > 1000) return new(false, "Phạm vi ôn tập cần 1–1.000 ký tự.");
        if (b.CanBoCoiThi.Length > 200 || b.LuuY.Length > 500) return new(false, "Cán bộ coi thi tối đa 200 ký tự, lưu ý tối đa 500 ký tự.");
        if (!BaiThi.CacQuyDinhTaiLieu.Contains(b.QuyDinhTaiLieu)) return new(false, "Quy định tài liệu không hợp lệ.");
        var cu = await db.BaiThi.FindAsync(b.MaLop, b.MaChuyenDe);
        if (cu is null) db.BaiThi.Add(b);
        else { cu.ThoiGianLamBai = b.ThoiGianLamBai; cu.CauTrucDe = b.CauTrucDe; cu.PhamViOnTap = b.PhamViOnTap; cu.QuyDinhTaiLieu = b.QuyDinhTaiLieu; cu.CanBoCoiThi = b.CanBoCoiThi; cu.LuuY = b.LuuY; }
        db.LichSuXetTuCach.Add(new LichSuXetTuCach { MaLop = b.MaLop, MaChuyenDe = b.MaChuyenDe, HanhDong = cu is null ? "Nhập thông tin bài thi" : "Sửa thông tin bài thi", NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro, ThoiDiem = DateTimeOffset.Now });
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, (cu?.DaGuiLuc is not null ? "Đã lưu. Thông báo đã gửi trước đó — bấm “Gửi lại thông báo” để học viên nhận bản cập nhật." : "Đã lưu thông tin bài thi."));
    }

    /// <summary>
    /// Gửi (hoặc cập nhật) thông báo thi tới học viên của lớp và giảng viên dạy lớp, qua mục Văn bản, thông báo (tag lop:{mã}).
    /// Nội dung chỉ gồm thông tin chung và số lượng đủ tư cách; tư cách từng người học viên tự xem ở "Học tập của tôi".
    /// </summary>
    public async Task<KetQuaThaoTac> GuiThongBaoThiAsync(string maLop, string maChuyenDe)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var hs = await LayHoSoTuCachAsync(maLop, maChuyenDe);
        if (!hs.DuocGuiThongBao || hs.Lich is not { } l || hs.BaiThi is not { } b) return new(false, "Cần duyệt tư cách, xếp lịch thi và nhập thông tin bài thi trước khi gửi thông báo (Phòng đào tạo/Admin).");
        await using var db = MoCsdl(nd);
        var tenCd = (await db.ChuyenDe.FindAsync(maChuyenDe))?.TenChuyenDe ?? maChuyenDe;
        var phong = (await db.Phong.FindAsync(l.MaPhong))?.TenPhong ?? l.MaPhong;
        var soDu = hs.Ho.SoHocVien - (JsonSerializer.Deserialize<Dictionary<string, string>>(hs.Ho.KhongDuTuCach)?.Count ?? 0);
        var gio = QuyTacHocVu.GioBatDauTiet(l.TietBatDau);
        var tieuDe = $"Thông báo lịch thi kết thúc học phần: {tenCd} – Lớp {maLop}";
        var noiDung = string.Join("\n", new[]
        {
            $"Phòng Đào tạo thông báo lịch thi kết thúc học phần “{tenCd}” của lớp {maLop} như sau:",
            "",
            $"1. Thời gian: {QuyTacHocVu.TenThu(l.Ngay)}, ngày {l.Ngay:dd/MM/yyyy}, bắt đầu {gio:hh\\:mm} (tiết {l.TietBatDau}–{l.TietKetThuc}). Thời gian làm bài: {b.ThoiGianLamBai} phút.",
            $"2. Địa điểm: {phong}.",
            $"3. Hình thức thi: {l.HinhThuc}. Cấu trúc đề: {b.CauTrucDe}",
            $"4. Phạm vi ôn tập: {b.PhamViOnTap}",
            $"5. Quy định tài liệu: {b.QuyDinhTaiLieu}.",
            b.CanBoCoiThi == "" ? "" : $"6. Cán bộ coi thi: {b.CanBoCoiThi}.",
            b.LuuY == "" ? "" : $"Lưu ý: {b.LuuY}",
            "",
            $"Danh sách dự thi: {soDu}/{hs.Ho.SoHocVien} học viên đủ tư cách (đã được Khoa xác nhận, Phòng Đào tạo duyệt). Học viên xem tư cách dự thi của mình tại mục Học tập của tôi → Lịch thi của tôi; học viên không đủ tư cách liên hệ Quản lý học viên.",
            "Học viên có mặt trước giờ thi 15 phút, mang thẻ học viên, trang phục đúng quy định.",
        }.Where(x => x is not null));
        var tb = new ThongBao
        {
            Ma = b.MaThongBao ?? Guid.Empty, TieuDe = tieuDe, NoiDung = noiDung, Tags = ["lop:" + maLop], LoaiVanBan = "Thông báo", MucDo = "Quan trọng",
            SoKyHieu = thongBao.GoiYSoKyHieu("Thông báo"), CoQuanBanHanh = "Phòng Đào tạo – Học viện An ninh nhân dân", NguoiKy = "Trưởng phòng Đào tạo",
            TrichYeu = $"Lịch thi {tenCd}, lớp {maLop}, ngày {l.Ngay:dd/MM/yyyy}", HieuLucDen = new DateTimeOffset(l.Ngay.Date.AddDays(1)),
            Anh = AnhMinhHoa.BannerVanBan("Thông báo", "Lịch thi", tieuDe),
        };
        var guiLai = b.DaGuiLuc is not null;
        var ma = thongBao.DangTuNghiepVu(tb, "Phòng Đào tạo");
        var cu = await db.BaiThi.FindAsync(maLop, maChuyenDe);
        cu!.DaGuiLuc = DateTimeOffset.Now; cu.NguoiGui = nd.Ten; cu.MaThongBao = ma;
        db.LichSuXetTuCach.Add(new LichSuXetTuCach { MaLop = maLop, MaChuyenDe = maChuyenDe, HanhDong = guiLai ? "Gửi lại thông báo thi" : "Gửi thông báo thi", NguoiThucHien = nd.Ten, VaiTro = nd.TenVaiTro, ThoiDiem = DateTimeOffset.Now, GhiChu = $"{hs.Ho.SoHocVien} học viên lớp {maLop}" });
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, guiLai ? $"Đã cập nhật thông báo thi tới lớp {maLop}." : $"Đã gửi thông báo thi tới {hs.Ho.SoHocVien} học viên lớp {maLop} và giảng viên dạy lớp.");
    }

    // ---------- Lịch thi ----------

    public async Task<List<string>> KiemTraLichThiAsync(LichThi l)
    {
        var loi = new List<string>();
        if (QuyTacHocVu.KiemTraKhungTiet(l.TietBatDau, l.SoTiet) is { } k) loi.Add(k);
        if (!LichThi.CacHinhThuc.Contains(l.HinhThuc)) loi.Add("Hình thức thi không hợp lệ.");
        if (l.Ngay.Date < DateTime.Today) loi.Add("Ngày thi phải từ hôm nay trở đi.");
        await using var db = await csdl.CreateDbContextAsync();
        var ho = await db.XetTuCach.FindAsync(l.MaLop, l.MaChuyenDe);
        if (ho?.TrangThai != TrangThaiXetTuCach.DaDuyet) loi.Add("Chỉ xếp lịch thi khi danh sách tư cách thi đã được Phòng đào tạo duyệt.");
        var soDuThi = ho is null ? 0 : ho.SoHocVien - (JsonSerializer.Deserialize<Dictionary<string, string>>(ho.KhongDuTuCach)?.Count ?? 0);
        var phong = await db.Phong.FindAsync(l.MaPhong);
        if (phong is null) loi.Add("Phòng thi không tồn tại.");
        else
        {
            if (phong.DangBaoTri) loi.Add($"{phong.TenPhong} đang bảo trì.");
            if (soDuThi > phong.SucChua) loi.Add($"{soDuThi} học viên dự thi, vượt sức chứa {phong.SucChua} chỗ của {phong.TenPhong}.");
        }
        if (loi.Count > 0) return loi;
        var gia = new BuoiHoc { Ngay = l.Ngay.Date, TietBatDau = l.TietBatDau, SoTiet = l.SoTiet };
        foreach (var b in await db.BuoiHoc.AsNoTracking().Where(b => b.Ngay == l.Ngay.Date && (b.MaPhong == l.MaPhong || b.MaLop == l.MaLop)).ToListAsync())
            if (QuyTacHocVu.TrungGio(b, gia)) loi.Add(b.MaLop == l.MaLop ? $"Lớp {l.MaLop} có buổi học tiết {b.TietBatDau}–{b.TietKetThuc} cùng ngày." : $"Phòng {l.MaPhong} có lớp {b.MaLop} học tiết {b.TietBatDau}–{b.TietKetThuc}.");
        foreach (var t in await db.LichThi.AsNoTracking().Where(t => t.Id != l.Id && t.Ngay == l.Ngay.Date && (t.MaPhong == l.MaPhong || t.MaLop == l.MaLop)).ToListAsync())
            if (QuyTacHocVu.TrungGio(new BuoiHoc { Ngay = t.Ngay, TietBatDau = t.TietBatDau, SoTiet = t.SoTiet }, gia))
                loi.Add(t.MaLop == l.MaLop ? $"Lớp {l.MaLop} đã có lịch thi khác tiết {t.TietBatDau}–{t.TietKetThuc}." : $"Phòng {l.MaPhong} đã có lớp {t.MaLop} thi tiết {t.TietBatDau}–{t.TietKetThuc}.");
        return loi;
    }

    public async Task<KetQuaThaoTac> LuuLichThiAsync(LichThi l)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return new(false, "Lịch thi do Phòng đào tạo (Quản lý đào tạo)/Admin sắp xếp.");
        l.Ngay = l.Ngay.Date;
        var loi = await KiemTraLichThiAsync(l);
        if (loi.Count > 0) return new(false, string.Join(" ", loi));
        await using var db = MoCsdl(nd);
        var cu = await db.LichThi.FirstOrDefaultAsync(x => x.MaLop == l.MaLop && x.MaChuyenDe == l.MaChuyenDe);
        if (cu is null) db.LichThi.Add(l);
        else { cu.Ngay = l.Ngay; cu.TietBatDau = l.TietBatDau; cu.SoTiet = l.SoTiet; cu.MaPhong = l.MaPhong; cu.HinhThuc = l.HinhThuc; cu.GhiChu = l.GhiChu; }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã xếp lịch thi lớp {l.MaLop} ngày {l.Ngay:dd/MM/yyyy}, tiết {l.TietBatDau}–{l.TietKetThuc}.");
    }

    /// <summary>Huỷ lịch thi (Phòng đào tạo/Admin), không cho huỷ khi ngày thi đã qua.</summary>
    public async Task<KetQuaThaoTac> XoaLichThiAsync(string maLop, string maChuyenDe)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("DaoTao")) return new(false, "Lịch thi do Phòng đào tạo (Quản lý đào tạo)/Admin sắp xếp.");
        await using var db = MoCsdl(nd);
        var cu = await db.LichThi.FirstOrDefaultAsync(x => x.MaLop == maLop && x.MaChuyenDe == maChuyenDe);
        if (cu is null) return new(false, "Chưa có lịch thi để huỷ.");
        if (cu.Ngay.Date < DateTime.Today) return new(false, "Ngày thi đã qua, không huỷ được lịch thi.");
        db.LichThi.Remove(cu);
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã huỷ lịch thi lớp {maLop}.");
    }

    /// <summary>Lịch thi của học viên đang đăng nhập (chỉ chuyên đề học viên đủ tư cách).</summary>
    public async Task<List<LichThiCuaToi>> LayLichThiCuaToiAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaHocVien) return [];
        string? maLop; lock (kho.KhoaDongBo) maLop = kho.DanhSachHocVien.FirstOrDefault(h => h.MaHocVien == nd.MaHocVien)?.MaLop;
        await using var db = await csdl.CreateDbContextAsync();
        var lich = await db.LichThi.AsNoTracking().Where(x => x.MaLop == maLop).ToListAsync();
        var cd = await db.ChuyenDe.AsNoTracking().ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var phong = await db.Phong.AsNoTracking().ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);
        var ho = await db.XetTuCach.AsNoTracking().Where(x => x.MaLop == maLop).ToDictionaryAsync(x => x.MaChuyenDe);
        var baiThi = await db.BaiThi.AsNoTracking().Where(x => x.MaLop == maLop).ToDictionaryAsync(x => x.MaChuyenDe);
        return lich.OrderBy(x => x.Ngay).Select(x =>
        {
            var khongDu = ho.TryGetValue(x.MaChuyenDe, out var h) ? JsonSerializer.Deserialize<Dictionary<string, string>>(h.KhongDuTuCach) ?? [] : [];
            return new LichThiCuaToi(x, cd.GetValueOrDefault(x.MaChuyenDe, x.MaChuyenDe), !khongDu.ContainsKey(nd.MaHocVien!), khongDu.GetValueOrDefault(nd.MaHocVien!),
                baiThi.GetValueOrDefault(x.MaChuyenDe), phong.GetValueOrDefault(x.MaPhong, x.MaPhong));
        }).ToList();
    }

    // ---------- Điểm danh đi làm (chấm công) ----------

    public async Task<ChamCong?> LayChamCongCuaToiAsync(DateTime? ngay = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaGiangVien) return null;
        await using var db = await csdl.CreateDbContextAsync();
        return await db.ChamCong.FindAsync(nd.MaCanBo, (ngay ?? DateTime.Today).Date);
    }

    /// <summary>Giảng viên tự điểm danh đi làm: trước 7:30 là có mặt, sau đó là đi muộn. Mỗi ngày một lần.</summary>
    public async Task<KetQuaThaoTac> TuChamCongAsync(DateTime? bayGio = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaGiangVien) return new(false, "Chỉ tài khoản giảng viên tự điểm danh đi làm.");
        var luc = bayGio ?? DateTime.Now;
        if (luc.DayOfWeek == DayOfWeek.Sunday) return new(false, "Chủ nhật không điểm danh đi làm.");
        await using var db = MoCsdl(nd);
        if (await db.ChamCong.FindAsync(nd.MaCanBo, luc.Date) is { } cu) return new(false, $"Hôm nay bạn đã được ghi \"{cu.TrangThai}\" lúc {cu.ThoiDiem.ToLocalTime():HH:mm}.");
        var tt = luc.TimeOfDay <= new TimeSpan(7, 30, 0) ? "Có mặt" : "Đi muộn";
        db.ChamCong.Add(new ChamCong { MaCanBo = nd.MaCanBo!, Ngay = luc.Date, TrangThai = tt, ThoiDiem = new DateTimeOffset(luc), NguoiGhi = nd.Ten });
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã điểm danh đi làm: {tt} lúc {luc:HH:mm}.");
    }

    public async Task<(List<(CanBoGiangVien CanBo, ChamCong? Cong)> Dong, string? Loi)> LayChamCongAsync(DateTime ngay, string? maKhoa)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("Khoa")) return ([], "Chỉ Quản lý khoa và Admin theo dõi điểm danh đi làm của cán bộ.");
        List<CanBoGiangVien> cb; lock (kho.KhoaDongBo) cb = kho.DanhSachCanBo.Where(c => maKhoa is null || c.MaKhoa == maKhoa).Select(c => c.TaoBanSao()).ToList();
        await using var db = await csdl.CreateDbContextAsync();
        var cong = await db.ChamCong.AsNoTracking().Where(x => x.Ngay == ngay.Date).ToDictionaryAsync(x => x.MaCanBo);
        return (cb.OrderBy(c => c.MaKhoa).ThenBy(c => c.KhoaSapXepTen, StringComparer.Ordinal).Select(c => (c, cong.GetValueOrDefault(c.MaCanBo))).ToList(), null);
    }

    public sealed record DongCongTuan(CanBoGiangVien CanBo, string TenTo, Dictionary<DateTime, ChamCong> Cong);

    /// <summary>Bảng công một tuần (thứ Hai – thứ Bảy) cho Quản lý khoa/Admin, sắp theo khoa → tổ → chức vụ.</summary>
    public async Task<(List<DongCongTuan> Dong, string? Loi)> LayBangCongTuanAsync(DateTime dauTuan, string? maKhoa = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("Khoa")) return ([], "Chỉ Quản lý khoa và Admin theo dõi điểm danh đi làm của cán bộ.");
        List<CanBoGiangVien> cb; Dictionary<string, string> tenTo;
        lock (kho.KhoaDongBo)
        {
            cb = kho.DanhSachCanBo.Where(c => maKhoa is null || c.MaKhoa == maKhoa).Select(c => c.TaoBanSao()).ToList();
            tenTo = kho.DanhSachTo.ToDictionary(t => t.MaTo, t => t.TenTo);
        }
        var den = dauTuan.Date.AddDays(6);
        await using var db = await csdl.CreateDbContextAsync();
        var cong = (await db.ChamCong.AsNoTracking().Where(x => x.Ngay >= dauTuan.Date && x.Ngay < den).ToListAsync()).GroupBy(x => x.MaCanBo).ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Ngay.Date));
        string[] thuTuChucVu = ["Trưởng khoa", "Phó khoa", "Tổ trưởng", "Tổ phó", "Cán bộ giảng viên"];
        return (cb.OrderBy(c => c.MaKhoa).ThenBy(c => c.MaTo).ThenBy(c => Array.IndexOf(thuTuChucVu, c.ChucVu)).ThenBy(c => c.KhoaSapXepTen, StringComparer.Ordinal)
            .Select(c => new DongCongTuan(c, tenTo.GetValueOrDefault(c.MaTo ?? "", ""), cong.GetValueOrDefault(c.MaCanBo, []))).ToList(), null);
    }

    /// <summary>Sinh chấm công mẫu 3 tuần gần đây (trừ hôm nay, Chủ nhật) để bảng công có dữ liệu minh hoạ.</summary>
    public static void KhoiTaoChamCong(CoSoDuLieuNghiepVu db, IEnumerable<CanBoGiangVien> cacCanBo, DateTime homNay)
    {
        if (db.ChamCong.Any()) return;
        var ds = new List<ChamCong>();
        foreach (var c in cacCanBo)
            for (var d = homNay.Date.AddDays(-21); d < homNay.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Sunday) continue;
                var h = Math.Abs((c.MaCanBo + d.ToString("yyyyMMdd")).Aggregate(17, (a, ch) => unchecked(a * 31 + ch))) % 100;
                var tt = h < 85 ? "Có mặt" : h < 91 ? "Đi muộn" : h < 96 ? "Công tác" : "Nghỉ phép";
                ds.Add(new ChamCong { MaCanBo = c.MaCanBo, Ngay = d, TrangThai = tt, ThoiDiem = new DateTimeOffset(d.AddHours(tt == "Đi muộn" ? 7.75 : 7.1)), NguoiGhi = "Dữ liệu mẫu" });
            }
        db.BoQuaNhatKy = true;
        db.ChamCong.AddRange(ds);
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }

    public async Task<KetQuaThaoTac> GhiChamCongAsync(string maCanBo, DateTime ngay, string trangThai)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.CoQuyenVung("Khoa")) return new(false, "Chỉ Quản lý khoa và Admin ghi điểm danh đi làm cho cán bộ.");
        if (!ChamCong.CacTrangThai.Contains(trangThai)) return new(false, "Trạng thái không hợp lệ.");
        if (ngay.Date > DateTime.Today) return new(false, "Không ghi điểm danh cho ngày chưa tới.");
        lock (kho.KhoaDongBo) if (!kho.DanhSachCanBo.Any(c => c.MaCanBo == maCanBo)) return new(false, "Không tìm thấy cán bộ.");
        await using var db = MoCsdl(nd);
        var cu = await db.ChamCong.FindAsync(maCanBo, ngay.Date);
        if (cu is null) db.ChamCong.Add(new ChamCong { MaCanBo = maCanBo, Ngay = ngay.Date, TrangThai = trangThai, ThoiDiem = DateTimeOffset.Now, NguoiGhi = nd.Ten });
        else { cu.TrangThai = trangThai; cu.NguoiGhi = nd.Ten; cu.ThoiDiem = DateTimeOffset.Now; }
        await db.SaveChangesAsync();
        return new(true, "Đã ghi.");
    }
}
