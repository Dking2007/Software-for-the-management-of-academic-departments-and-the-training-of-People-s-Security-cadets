using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Trực ban theo khoa (thiết kế mục 6.6). Quyền trực ban là quyền hẹp có thời hạn, tính lại ở mỗi thao tác từ bảng CaTrucBan:
/// - Quản lý khoa/Admin lập lịch trực (phân công tay hoặc xếp tự động, luân phiên công bằng).
/// - Đúng ngày trực, người trực (giảng viên của khoa) được điểm danh đi làm cho cán bộ toàn khoa,
///   xem quân số cán bộ từng người và nắm quân số học viên học các chuyên đề của khoa ở mức số liệu (hàng rào 2).
/// - Người trực ghi sổ trực ban rồi chốt báo cáo; chốt xong hoặc qua ngày thì quyền tự hết, báo cáo giữ số liệu lúc chốt.
/// Không đổi vai trò tài khoản, không cấp thêm quyền nào khác (không xem hồ sơ cá nhân, không sửa dữ liệu khoa).
/// </summary>
public sealed class DichVuTrucBan(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen, DichVuDaoTao daoTao)
{
    /// <summary>Đang ở trạng thái này trong ngày thì không nhận trực ban được.</summary>
    public static readonly string[] TrangThaiKhongTrucDuoc = ["Nghỉ phép", "Công tác"];

    public sealed record DongCanBoTruc(CanBoGiangVien CanBo, string TenTo, ChamCong? Cong, string LichDay);
    public sealed record DongBuoiKhoa(int BuoiHocId, string MaLop, string TenChuyenDe, string TenGiangVien, string TenPhong, int TietBatDau, int TietKetThuc,
        int SiSo, int CoMat, int DiMuon, int VangCoPhep, int VangKhongPhep)
    {
        public bool DaDiemDanh => CoMat + DiMuon + VangCoPhep + VangKhongPhep > 0;
    }
    /// <summary>Toàn bộ bảng trực của một khoa trong một ngày, kèm quyền của người đang xem.</summary>
    public sealed record BangTruc(string MaKhoa, string TenKhoa, DateTime Ngay, CaTrucBan? Ca, string TenNguoiTruc, bool LaNguoiTruc, bool DuocGhi, bool DuocSuaSo,
        List<DongCanBoTruc> CanBo, QuanSoCanBo QuanSoCanBo, List<DongBuoiKhoa> Buoi, QuanSoHocVien QuanSoHocVien);
    public sealed record OLich(DateTime Ngay, CaTrucBan? Ca, string TenNguoiTruc);

    private CoSoDuLieuNghiepVu MoCsdl(NguoiDungHienTai nd) { var db = csdl.CreateDbContext(); db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro; return db; }
    /// <summary>Mặt cắt tối thiểu của cán bộ cho bảng trực: tên, cấp bậc, chức vụ, khoa, tổ. Không mang dữ liệu cá nhân (mục 6.4).</summary>
    private static CanBoGiangVien CongKhai(CanBoGiangVien c) => new()
    {
        MaCanBo = c.MaCanBo, MaTraCuuNoiBo = c.MaCanBo, HoTen = c.HoTen, CapBacHam = c.CapBacHam, ChucVu = c.ChucVu, MaKhoa = c.MaKhoa, MaTo = c.MaTo
    };
    private CanBoGiangVien? CanBo(string? ma) { if (ma is null) return null; lock (kho.KhoaDongBo) return kho.DanhSachCanBo.FirstOrDefault(c => c.MaCanBo == ma)?.TaoBanSao(); }
    private string? KhoaCua(NguoiDungHienTai nd) => CanBo(nd.MaCanBo)?.MaKhoa;
    private string TenKhoa(string maKhoa) { lock (kho.KhoaDongBo) return kho.DanhSachKhoa.FirstOrDefault(k => k.MaKhoa == maKhoa)?.TenKhoa ?? maKhoa; }
    private string TenNguoi(string? maCanBo) => CanBo(maCanBo) is { } c ? $"{c.CapBacHam} {c.HoTen}".Trim() : "";
    private static string DanNhan(NguoiDungHienTai nd, string maKhoa) => nd.LaGiangVien ? $"{nd.Ten} (trực ban {maKhoa})" : nd.Ten;

    // ---------- Quy tắc quyền: một nơi duy nhất ----------

    /// <summary>Quản lý khoa và Admin lập lịch, xem và ghi bảng trực của mọi khoa.</summary>
    private static bool LaQuanLyTruc(NguoiDungHienTai nd) => nd.CoQuyenVung("Khoa");

    /// <summary>
    /// Ca trực đang hiệu lực của người đang đăng nhập với khoa này: đúng ngày hôm nay, đúng người, chưa chốt.
    /// Đây là toàn bộ "quyền trực ban"; qua 0 giờ hoặc chốt báo cáo là hết, không có gì phải thu hồi.
    /// </summary>
    private static async Task<CaTrucBan?> CaHieuLucAsync(CoSoDuLieuNghiepVu db, NguoiDungHienTai nd, string maKhoa, DateTime homNay)
    {
        if (!nd.LaGiangVien) return null;
        var ca = await db.CaTrucBan.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhoa == maKhoa && x.Ngay == homNay.Date);
        return ca is not null && ca.MaCanBo == nd.MaCanBo && ca.ChotLuc is null ? ca : null;
    }

    /// <summary>Ca trực hôm nay của chính giảng viên đang đăng nhập (để hiện lối vào nhanh), kèm ca kế tiếp.</summary>
    public async Task<(CaTrucBan? HomNay, CaTrucBan? KeTiep)> LayCaCuaToiAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaGiangVien) return (null, null);
        await using var db = await csdl.CreateDbContextAsync();
        var homNay = DateTime.Today;
        var ds = await db.CaTrucBan.AsNoTracking().Where(x => x.MaCanBo == nd.MaCanBo && x.Ngay >= homNay).OrderBy(x => x.Ngay).Take(2).ToListAsync();
        var hn = ds.FirstOrDefault(x => x.Ngay == homNay);
        return (hn, ds.FirstOrDefault(x => x.Ngay > homNay));
    }

    /// <summary>Khoa người đang xem được mở trang trực ban: quản lý thấy mọi khoa, giảng viên chỉ khoa mình.</summary>
    public async Task<List<Khoa>> LayKhoaDuocXemAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        List<Khoa> ds; lock (kho.KhoaDongBo) ds = kho.DanhSachKhoa.Select(k => k.TaoBanSao()).ToList();
        if (LaQuanLyTruc(nd)) return ds;
        var cuaToi = KhoaCua(nd);
        return cuaToi is null ? [] : ds.Where(k => k.MaKhoa == cuaToi).ToList();
    }

    // ---------- Bảng trực trong ngày ----------

    public async Task<(BangTruc? Bang, string? Loi)> LayBangTrucAsync(string maKhoa, DateTime? ngay = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var d = (ngay ?? DateTime.Today).Date;
        var quanLy = LaQuanLyTruc(nd);
        if (!quanLy && !(nd.LaGiangVien && KhoaCua(nd) == maKhoa)) return (null, "Bảng trực ban chỉ dành cho cán bộ của khoa, Quản lý khoa và Admin.");
        await using var db = await csdl.CreateDbContextAsync();
        var ca = await db.CaTrucBan.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhoa == maKhoa && x.Ngay == d);
        var laNguoiTruc = nd.LaGiangVien && ca?.MaCanBo == nd.MaCanBo;
        var hieuLuc = laNguoiTruc && ca!.ChotLuc is null && d == DateTime.Today;
        // Giảng viên khác của khoa chỉ xem lịch và báo cáo đã chốt; bảng chi tiết trong ngày thuộc người trực và quản lý.
        if (!quanLy && !laNguoiTruc)
            return (new BangTruc(maKhoa, TenKhoa(maKhoa), d, ca, TenNguoi(ca?.MaCanBo), false, false, false, [], new(0, 0, 0, 0, 0, 0, 0), [], new(0, 0, 0, 0, 0, 0, 0, 0)), null);
        var (dongCb, qsCb, buoi, qsHv) = await TinhTongHopAsync(db, maKhoa, d);
        var duocGhi = d <= DateTime.Today && d.DayOfWeek != DayOfWeek.Sunday && (hieuLuc || quanLy);
        return (new BangTruc(maKhoa, TenKhoa(maKhoa), d, ca, TenNguoi(ca?.MaCanBo), laNguoiTruc, duocGhi, hieuLuc, dongCb, qsCb, buoi, qsHv), null);
    }

    /// <summary>Tính quân số cán bộ (từng người) và học viên (chỉ số liệu) của khoa trong một ngày.</summary>
    private async Task<(List<DongCanBoTruc>, QuanSoCanBo, List<DongBuoiKhoa>, QuanSoHocVien)> TinhTongHopAsync(CoSoDuLieuNghiepVu db, string maKhoa, DateTime d)
    {
        List<CanBoGiangVien> cb; Dictionary<string, string> tenTo; Dictionary<string, int> siSo;
        lock (kho.KhoaDongBo)
        {
            cb = kho.DanhSachCanBo.Where(c => c.MaKhoa == maKhoa).Select(CongKhai).ToList();
            tenTo = kho.DanhSachTo.ToDictionary(t => t.MaTo, t => t.TenTo);
            siSo = kho.DanhSachHocVien.GroupBy(h => h.MaLop).ToDictionary(g => g.Key, g => g.Count());
        }
        var maCb = cb.Select(c => c.MaCanBo).ToList();
        var den = d.AddDays(1);
        var cong = await db.ChamCong.AsNoTracking().Where(x => x.Ngay == d && maCb.Contains(x.MaCanBo)).ToDictionaryAsync(x => x.MaCanBo);
        var cdKhoa = await db.ChuyenDe.AsNoTracking().Where(c => c.MaKhoa == maKhoa).ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var maCd = cdKhoa.Keys.ToList();
        // Buổi trong ngày: của chuyên đề khoa phụ trách (để nắm học viên) hoặc do cán bộ khoa dạy (để biết ai đang lên lớp).
        var buoi = await db.BuoiHoc.AsNoTracking().Where(b => b.Ngay >= d && b.Ngay < den && (maCd.Contains(b.MaChuyenDe) || maCb.Contains(b.MaCanBo))).ToListAsync();
        var idBuoi = buoi.Select(b => b.Id).ToList();
        var dd = (await db.DiemDanh.AsNoTracking().Where(x => idBuoi.Contains(x.BuoiHocId)).Select(x => new { x.BuoiHocId, x.TrangThai }).ToListAsync())
            .GroupBy(x => x.BuoiHocId).ToDictionary(g => g.Key, g => g.Select(x => x.TrangThai).ToList());
        var phong = await db.Phong.AsNoTracking().ToDictionaryAsync(p => p.MaPhong, p => p.TenPhong);

        string[] thuTuChucVu = ["Trưởng khoa", "Phó khoa", "Tổ trưởng", "Tổ phó", "Cán bộ giảng viên"];
        var dongCb = cb.OrderBy(c => c.MaTo).ThenBy(c => Array.IndexOf(thuTuChucVu, c.ChucVu)).ThenBy(c => c.KhoaSapXepTen, StringComparer.Ordinal)
            .Select(c => new DongCanBoTruc(c, tenTo.GetValueOrDefault(c.MaTo ?? "", ""), cong.GetValueOrDefault(c.MaCanBo),
                string.Join(", ", buoi.Where(b => b.MaCanBo == c.MaCanBo).OrderBy(b => b.TietBatDau).Select(b => $"T{b.TietBatDau}–{b.TietKetThuc} {b.MaLop}")))).ToList();
        var tt = dongCb.Select(x => x.Cong?.TrangThai).ToList();
        var qsCb = new QuanSoCanBo(tt.Count, tt.Count(x => x == "Có mặt"), tt.Count(x => x == "Đi muộn"), tt.Count(x => x == "Công tác"), tt.Count(x => x == "Nghỉ phép"), tt.Count(x => x == "Vắng"), tt.Count(x => x is null));

        var buoiKhoa = buoi.Where(b => cdKhoa.ContainsKey(b.MaChuyenDe)).OrderBy(b => b.TietBatDau).ThenBy(b => b.MaLop.Length).ThenBy(b => b.MaLop)
            .Select(b =>
            {
                var t = dd.GetValueOrDefault(b.Id, []);
                return new DongBuoiKhoa(b.Id, b.MaLop, cdKhoa[b.MaChuyenDe], daoTao.TenCanBo(b.MaCanBo), phong.GetValueOrDefault(b.MaPhong, b.MaPhong), b.TietBatDau, b.TietKetThuc,
                    siSo.GetValueOrDefault(b.MaLop), t.Count(x => x == TrangThaiDiemDanh.CoMat), t.Count(x => x == TrangThaiDiemDanh.DiMuon),
                    t.Count(x => x == TrangThaiDiemDanh.VangCoPhep), t.Count(x => x == TrangThaiDiemDanh.VangKhongPhep));
            }).ToList();
        var qsHv = new QuanSoHocVien(buoiKhoa.Count, buoiKhoa.Select(b => b.MaLop).Distinct().Count(), buoiKhoa.Sum(b => b.SiSo), buoiKhoa.Sum(b => b.CoMat), buoiKhoa.Sum(b => b.DiMuon),
            buoiKhoa.Sum(b => b.VangCoPhep), buoiKhoa.Sum(b => b.VangKhongPhep), buoiKhoa.Count(b => !b.DaDiemDanh));
        return (dongCb, qsCb, buoiKhoa, qsHv);
    }

    // ---------- Người trực điểm danh đi làm cho cán bộ toàn khoa ----------

    /// <summary>
    /// Ghi điểm danh đi làm cho một cán bộ trong ngày. Được ghi khi: là Quản lý khoa/Admin, hoặc là người trực ban của đúng khoa,
    /// đúng hôm nay, ca chưa chốt. Bản ghi ghi rõ "(trực ban KH-xx)" để biết ai ghi theo quyền nào.
    /// </summary>
    public async Task<KetQuaThaoTac> ChamCongAsync(string maCanBo, string trangThai, DateTime? ngay = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var d = (ngay ?? DateTime.Today).Date;
        if (!ChamCong.CacTrangThai.Contains(trangThai)) return new(false, "Trạng thái không hợp lệ.");
        var cb = CanBo(maCanBo);
        if (cb is null) return new(false, "Không tìm thấy cán bộ.");
        await using var db = MoCsdl(nd);
        var loi = await KiemTraQuyenGhiAsync(db, nd, cb.MaKhoa, d);
        if (loi is not null) return new(false, loi);
        var cu = await db.ChamCong.FindAsync(maCanBo, d);
        if (cu is null) db.ChamCong.Add(new ChamCong { MaCanBo = maCanBo, Ngay = d, TrangThai = trangThai, ThoiDiem = DateTimeOffset.Now, NguoiGhi = DanNhan(nd, cb.MaKhoa) });
        else { cu.TrangThai = trangThai; cu.ThoiDiem = DateTimeOffset.Now; cu.NguoiGhi = DanNhan(nd, cb.MaKhoa); }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã ghi {cb.HoTen}: {trangThai}.");
    }

    /// <summary>Ghi "Có mặt" cho mọi cán bộ của khoa chưa được điểm danh trong ngày (thao tác nhanh đầu giờ).</summary>
    public async Task<KetQuaThaoTac> GhiCoMatChoNguoiChuaDiemDanhAsync(string maKhoa, DateTime? ngay = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var d = (ngay ?? DateTime.Today).Date;
        await using var db = MoCsdl(nd);
        var loi = await KiemTraQuyenGhiAsync(db, nd, maKhoa, d);
        if (loi is not null) return new(false, loi);
        List<string> maCb; lock (kho.KhoaDongBo) maCb = kho.DanhSachCanBo.Where(c => c.MaKhoa == maKhoa).Select(c => c.MaCanBo).ToList();
        var daCo = (await db.ChamCong.Where(x => x.Ngay == d && maCb.Contains(x.MaCanBo)).Select(x => x.MaCanBo).ToListAsync()).ToHashSet();
        var moi = maCb.Where(m => !daCo.Contains(m)).ToList();
        if (moi.Count == 0) return new(false, "Mọi cán bộ trong khoa đã được điểm danh.");
        db.ChamCong.AddRange(moi.Select(m => new ChamCong { MaCanBo = m, Ngay = d, TrangThai = "Có mặt", ThoiDiem = DateTimeOffset.Now, NguoiGhi = DanNhan(nd, maKhoa) }));
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã ghi \"Có mặt\" cho {moi.Count} cán bộ chưa điểm danh.");
    }

    private static async Task<string?> KiemTraQuyenGhiAsync(CoSoDuLieuNghiepVu db, NguoiDungHienTai nd, string maKhoa, DateTime d)
    {
        if (d > DateTime.Today) return "Không ghi điểm danh cho ngày chưa tới.";
        if (d.DayOfWeek == DayOfWeek.Sunday) return "Chủ nhật không điểm danh đi làm.";
        if (LaQuanLyTruc(nd)) return null;
        if (d != DateTime.Today) return "Quyền trực ban chỉ có hiệu lực trong ngày trực; ngày đã qua do Quản lý khoa điều chỉnh.";
        if (await CaHieuLucAsync(db, nd, maKhoa, d) is null)
        {
            var ca = nd.LaGiangVien ? await db.CaTrucBan.AsNoTracking().FirstOrDefaultAsync(x => x.MaKhoa == maKhoa && x.Ngay == d) : null;
            return ca is not null && ca.MaCanBo == nd.MaCanBo && ca.ChotLuc is not null
                ? "Ca trực đã chốt báo cáo; quyền trực ban đã kết thúc."
                : "Chỉ người trực ban của khoa trong ngày hôm nay (hoặc Quản lý khoa, Admin) được điểm danh cho cán bộ.";
        }
        return null;
    }

    // ---------- Sổ trực ban ----------

    public async Task<KetQuaThaoTac> LuuSoTrucAsync(string maKhoa, string tinhHinh, string banGiao)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var homNay = DateTime.Today;
        if (await CaHieuLucAsync(db, nd, maKhoa, homNay) is null) return new(false, "Chỉ người trực ban hôm nay, khi ca chưa chốt, được ghi sổ trực ban.");
        var ca = await db.CaTrucBan.FirstAsync(x => x.MaKhoa == maKhoa && x.Ngay == homNay);
        ca.TinhHinh = (tinhHinh ?? "").Trim();
        ca.BanGiao = (banGiao ?? "").Trim();
        if (ca.TinhHinh.Length > 2000 || ca.BanGiao.Length > 2000) return new(false, "Mỗi mục sổ trực ban không quá 2.000 ký tự.");
        await db.SaveChangesAsync();
        return new(true, "Đã lưu sổ trực ban.");
    }

    /// <summary>Chốt báo cáo: chụp số liệu cán bộ và học viên lúc chốt, khoá ca. Sau khi chốt, người trực không còn quyền ghi.</summary>
    public async Task<KetQuaThaoTac> ChotCaAsync(string maKhoa, string tinhHinh, string banGiao)
    {
        var luu = await LuuSoTrucAsync(maKhoa, tinhHinh, banGiao);
        if (!luu.ThanhCong) return luu;
        var nd = await phanQuyen.LayNguoiDungAsync();
        await using var db = MoCsdl(nd);
        var homNay = DateTime.Today;
        var ca = await db.CaTrucBan.FirstAsync(x => x.MaKhoa == maKhoa && x.Ngay == homNay);
        if (ca.TinhHinh == "") return new(false, "Ghi tình hình chung trong ca trước khi chốt báo cáo.");
        var (_, qsCb, _, qsHv) = await TinhTongHopAsync(db, maKhoa, homNay);
        if (qsCb.ChuaDiemDanh > 0) return new(false, $"Còn {qsCb.ChuaDiemDanh} cán bộ chưa điểm danh. Điểm danh đủ quân số trước khi chốt.");
        ca.TongHopCanBo = qsCb.ToString();
        ca.TongHopHocVien = qsHv.ToString();
        ca.ChotLuc = DateTimeOffset.Now;
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã chốt báo cáo trực ban lúc {DateTime.Now:HH:mm}. Quyền trực ban của bạn kết thúc.");
    }

    /// <summary>Báo cáo trực ban đã chốt của khoa (mới nhất trước). Cán bộ của khoa và quản lý xem được.</summary>
    public async Task<List<(CaTrucBan Ca, string TenNguoiTruc)>> LayBaoCaoAsync(string maKhoa, int soNgay = 30)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!LaQuanLyTruc(nd) && KhoaCua(nd) != maKhoa) return [];
        await using var db = await csdl.CreateDbContextAsync();
        var tu = DateTime.Today.AddDays(-soNgay);
        var ds = await db.CaTrucBan.AsNoTracking().Where(x => x.MaKhoa == maKhoa && x.Ngay >= tu && x.Ngay <= DateTime.Today).OrderByDescending(x => x.Ngay).ToListAsync();
        return ds.Select(x => (x, TenNguoi(x.MaCanBo))).ToList();
    }

    // ---------- Lịch trực ----------

    public async Task<(List<OLich> Lich, string? Loi)> LayLichTrucAsync(string maKhoa, DateTime tuNgay, DateTime denNgay)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!LaQuanLyTruc(nd) && KhoaCua(nd) != maKhoa) return ([], "Lịch trực ban chỉ dành cho cán bộ của khoa, Quản lý khoa và Admin.");
        await using var db = await csdl.CreateDbContextAsync();
        var tu = tuNgay.Date; var den = denNgay.Date;
        var ca = await db.CaTrucBan.AsNoTracking().Where(x => x.MaKhoa == maKhoa && x.Ngay >= tu && x.Ngay <= den).ToDictionaryAsync(x => x.Ngay);
        var ds = new List<OLich>();
        for (var d = tu; d <= den; d = d.AddDays(1)) { var c = ca.GetValueOrDefault(d); ds.Add(new(d, c, TenNguoi(c?.MaCanBo))); }
        return (ds, null);
    }

    /// <summary>Một ngày làm việc của một khoa trong lịch trực, kèm người trực (mặt cắt tối thiểu) để xuất Excel.</summary>
    public sealed record DongLichXuat(DateTime Ngay, string MaKhoa, string TenKhoa, CaTrucBan? Ca, CanBoGiangVien? NguoiTruc, string TenTo)
    {
        public string TrangThai => Ca is null ? "Chưa phân công" : Ca.ChotLuc is not null ? "Đã chốt" : Ngay > DateTime.Today ? "Đã phân công" : Ngay == DateTime.Today ? "Đang trực" : "Không chốt";
        public string TenNguoiTruc => NguoiTruc is null ? "" : $"{NguoiTruc.CapBacHam} {NguoiTruc.HoTen}".Trim();
    }

    /// <summary>
    /// Lịch trực để xuất: một khoa (cán bộ của khoa đó, Quản lý khoa, Admin) hoặc mọi khoa khi maKhoa rỗng (chỉ Quản lý khoa, Admin).
    /// Mỗi ngày trừ Chủ nhật một dòng cho mỗi khoa, kể cả ngày chưa phân công.
    /// </summary>
    public async Task<(List<DongLichXuat> Dong, string? Loi)> LayLichXuatAsync(string? maKhoa, DateTime tuNgay, DateTime denNgay)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (maKhoa is null ? !LaQuanLyTruc(nd) : !LaQuanLyTruc(nd) && KhoaCua(nd) != maKhoa) return ([], "Lịch trực ban chỉ dành cho cán bộ của khoa, Quản lý khoa và Admin.");
        var tu = tuNgay.Date; var den = denNgay.Date;
        List<Khoa> khoa; Dictionary<string, CanBoGiangVien> cb; Dictionary<string, string> tenTo;
        lock (kho.KhoaDongBo)
        {
            khoa = kho.DanhSachKhoa.Where(k => maKhoa is null || k.MaKhoa == maKhoa).OrderBy(k => k.MaKhoa).Select(k => k.TaoBanSao()).ToList();
            cb = kho.DanhSachCanBo.Where(c => maKhoa is null || c.MaKhoa == maKhoa).ToDictionary(c => c.MaCanBo, CongKhai);
            tenTo = kho.DanhSachTo.ToDictionary(t => t.MaTo, t => t.TenTo);
        }
        await using var db = await csdl.CreateDbContextAsync();
        var ca = (await db.CaTrucBan.AsNoTracking().Where(x => (maKhoa == null || x.MaKhoa == maKhoa) && x.Ngay >= tu && x.Ngay <= den).ToListAsync())
            .ToDictionary(x => (x.MaKhoa, x.Ngay.Date));
        var ds = new List<DongLichXuat>();
        for (var d = tu; d <= den; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Sunday) continue;
            foreach (var k in khoa)
            {
                var c = ca.GetValueOrDefault((k.MaKhoa, d));
                var nguoi = c is null ? null : cb.GetValueOrDefault(c.MaCanBo);
                ds.Add(new(d, k.MaKhoa, k.TenKhoa, c, nguoi, nguoi?.MaTo is { } mt ? tenTo.GetValueOrDefault(mt, "") : ""));
            }
        }
        return (ds, null);
    }

    /// <summary>Cán bộ có thể nhận trực ban: thuộc khoa, trừ Trưởng khoa (trực chỉ huy, không trực ban).</summary>
    public List<CanBoGiangVien> LayNguoiCoTheTruc(string maKhoa)
    {
        lock (kho.KhoaDongBo)
        {
            var ds = kho.DanhSachCanBo.Where(c => c.MaKhoa == maKhoa).ToList();
            var tru = ds.Where(c => c.ChucVu != "Trưởng khoa").ToList();
            return (tru.Count > 0 ? tru : ds).OrderBy(c => c.KhoaSapXepTen, StringComparer.Ordinal).Select(CongKhai).ToList();
        }
    }

    /// <summary>Phân công (hoặc bỏ phân công khi maCanBo rỗng) một ngày. Chỉ ngày từ hôm nay trở đi và ca chưa chốt.</summary>
    public async Task<KetQuaThaoTac> PhanCongAsync(string maKhoa, DateTime ngay, string? maCanBo)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!LaQuanLyTruc(nd)) return new(false, "Chỉ Quản lý khoa và Admin lập lịch trực ban.");
        var d = ngay.Date;
        if (d < DateTime.Today) return new(false, "Không sửa lịch trực của ngày đã qua.");
        if (d.DayOfWeek == DayOfWeek.Sunday) return new(false, "Chủ nhật không xếp trực ban (không điểm danh đi làm).");
        await using var db = MoCsdl(nd);
        var cu = await db.CaTrucBan.FirstOrDefaultAsync(x => x.MaKhoa == maKhoa && x.Ngay == d);
        if (cu?.ChotLuc is not null) return new(false, "Ca trực đã chốt báo cáo, không đổi người trực được.");
        if (string.IsNullOrEmpty(maCanBo))
        {
            if (cu is null) return new(true, "Ngày này chưa có người trực.");
            db.CaTrucBan.Remove(cu);
            await db.SaveChangesAsync();
            kho.BaoThayDoiNgoai();
            return new(true, $"Đã bỏ phân công trực ban ngày {d:dd/MM}.");
        }
        var cb = CanBo(maCanBo);
        if (cb is null || cb.MaKhoa != maKhoa) return new(false, "Người trực phải là cán bộ của khoa.");
        var cong = await db.ChamCong.AsNoTracking().FirstOrDefaultAsync(x => x.MaCanBo == maCanBo && x.Ngay == d);
        if (cong is not null && TrangThaiKhongTrucDuoc.Contains(cong.TrangThai)) return new(false, $"{cb.HoTen} đang {cong.TrangThai.ToLowerInvariant()} ngày {d:dd/MM}, không nhận trực ban được.");
        if (cu is null) db.CaTrucBan.Add(new CaTrucBan { MaKhoa = maKhoa, Ngay = d, MaCanBo = maCanBo, NguoiPhanCong = nd.Ten, PhanCongLuc = DateTimeOffset.Now });
        else { cu.MaCanBo = maCanBo; cu.NguoiPhanCong = nd.Ten; cu.PhanCongLuc = DateTimeOffset.Now; }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã phân công {cb.HoTen} trực ban ngày {d:dd/MM}.");
    }

    /// <summary>
    /// Xếp tự động các ngày còn trống (từ hôm nay, trừ Chủ nhật) trong khoảng, luân phiên công bằng:
    /// ưu tiên người trực ít nhất trong 60 ngày gần đây, rồi người ít tiết dạy hôm đó, rồi người lâu chưa trực;
    /// bỏ qua người nghỉ phép/công tác hôm đó và tránh trực hai ngày liền nếu còn người khác.
    /// </summary>
    public async Task<KetQuaThaoTac> XepTuDongAsync(string maKhoa, DateTime tuNgay, DateTime denNgay)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!LaQuanLyTruc(nd)) return new(false, "Chỉ Quản lý khoa và Admin lập lịch trực ban.");
        var tu = tuNgay.Date < DateTime.Today ? DateTime.Today : tuNgay.Date;
        var den = denNgay.Date;
        if (den < tu) return new(false, "Khoảng ngày đã qua, không còn ngày để xếp.");
        await using var db = MoCsdl(nd);
        var soCa = XepLich(db, maKhoa, LayNguoiCoTheTruc(maKhoa), tu, den, nd.Ten);
        if (soCa == 0) return new(false, "Không còn ngày trống để xếp trong khoảng này.");
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, $"Đã xếp {soCa} ca trực, luân phiên đều giữa {LayNguoiCoTheTruc(maKhoa).Count} cán bộ.");
    }

    /// <summary>Thuật toán xếp lịch dùng chung cho dịch vụ và dữ liệu mẫu. Chỉ thêm vào context, không lưu.</summary>
    internal static int XepLich(CoSoDuLieuNghiepVu db, string maKhoa, List<CanBoGiangVien> ungVien, DateTime tu, DateTime den, string nguoiPhanCong, string? uuTienHomNay = null)
    {
        if (ungVien.Count == 0) return 0;
        var maUv = ungVien.Select(c => c.MaCanBo).ToList();
        var daCo = db.CaTrucBan.AsNoTracking().Where(x => x.MaKhoa == maKhoa && x.Ngay >= tu.AddDays(-60) && x.Ngay <= den).Select(x => new { x.Ngay, x.MaCanBo }).ToList();
        var soLan = maUv.ToDictionary(m => m, m => daCo.Count(x => x.MaCanBo == m));
        var lanCuoi = maUv.ToDictionary(m => m, m => daCo.Where(x => x.MaCanBo == m).Select(x => x.Ngay).DefaultIfEmpty(DateTime.MinValue).Max());
        var ngayDaCo = daCo.Select(x => x.Ngay.Date).ToHashSet();
        var vang = db.ChamCong.AsNoTracking().Where(x => x.Ngay >= tu && x.Ngay <= den && maUv.Contains(x.MaCanBo) && TrangThaiKhongTrucDuoc.Contains(x.TrangThai))
            .Select(x => new { x.MaCanBo, x.Ngay }).ToList().Select(x => (x.MaCanBo, x.Ngay.Date)).ToHashSet();
        var tiet = db.BuoiHoc.AsNoTracking().Where(b => b.Ngay >= tu && b.Ngay < den.AddDays(1) && maUv.Contains(b.MaCanBo)).Select(b => new { b.MaCanBo, b.Ngay, b.SoTiet }).ToList()
            .GroupBy(b => (b.MaCanBo, b.Ngay.Date)).ToDictionary(g => g.Key, g => g.Sum(x => x.SoTiet));
        var dem = 0;
        for (var d = tu; d <= den; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Sunday || ngayDaCo.Contains(d)) continue;
            var duoc = ungVien.Where(c => !vang.Contains((c.MaCanBo, d))).ToList();
            if (duoc.Count == 0) continue;
            var chon = uuTienHomNay is not null && d == DateTime.Today ? duoc.FirstOrDefault(c => c.MaCanBo == uuTienHomNay) : null;
            var khongLienTiep = duoc.Where(c => lanCuoi[c.MaCanBo] < d.AddDays(-1)).ToList();
            if (khongLienTiep.Count > 0) duoc = khongLienTiep;
            chon ??= duoc.OrderBy(c => soLan[c.MaCanBo]).ThenBy(c => tiet.GetValueOrDefault((c.MaCanBo, d))).ThenBy(c => lanCuoi[c.MaCanBo]).ThenBy(c => c.MaCanBo, StringComparer.Ordinal).First();
            db.CaTrucBan.Add(new CaTrucBan { MaKhoa = maKhoa, Ngay = d, MaCanBo = chon.MaCanBo, NguoiPhanCong = nguoiPhanCong, PhanCongLuc = DateTimeOffset.Now });
            soLan[chon.MaCanBo]++; lanCuoi[chon.MaCanBo] = d; ngayDaCo.Add(d); dem++;
        }
        return dem;
    }

    /// <summary>
    /// Dữ liệu mẫu: lịch trực mỗi khoa từ 14 ngày trước tới 14 ngày sau; giảng viên mẫu trực khoa mình hôm nay để demo được.
    /// Các ca đã qua được chốt với số liệu cán bộ lấy từ chấm công mẫu.
    /// </summary>
    public static void KhoiTaoTrucBan(CoSoDuLieuNghiepVu db, IEnumerable<Khoa> cacKhoa, IEnumerable<CanBoGiangVien> cacCanBo, DateTime homNay, string maGiangVienMau)
    {
        if (db.CaTrucBan.Any()) return;
        var cb = cacCanBo.ToList();
        db.BoQuaNhatKy = true;
        foreach (var k in cacKhoa)
        {
            var cua = cb.Where(c => c.MaKhoa == k.MaKhoa).ToList();
            var uv = cua.Where(c => c.ChucVu != "Trưởng khoa").ToList();
            if (uv.Count == 0) uv = cua;
            XepLich(db, k.MaKhoa, uv, homNay.Date.AddDays(-14), homNay.Date.AddDays(14), "Dữ liệu mẫu", cua.Any(c => c.MaCanBo == maGiangVienMau) ? maGiangVienMau : null);
        }
        var caCu = db.ChangeTracker.Entries<CaTrucBan>().Select(e => e.Entity).Where(c => c.Ngay < homNay.Date).ToList();
        var cong = db.ChamCong.AsNoTracking().Where(x => x.Ngay < homNay.Date && x.Ngay >= homNay.Date.AddDays(-14)).ToList().GroupBy(x => x.Ngay.Date).ToDictionary(g => g.Key, g => g.ToDictionary(x => x.MaCanBo, x => x.TrangThai));
        foreach (var c in caCu)
        {
            var tt = cb.Where(x => x.MaKhoa == c.MaKhoa).Select(x => cong.GetValueOrDefault(c.Ngay.Date)?.GetValueOrDefault(x.MaCanBo)).ToList();
            c.TongHopCanBo = new QuanSoCanBo(tt.Count, tt.Count(x => x == "Có mặt"), tt.Count(x => x == "Đi muộn"), tt.Count(x => x == "Công tác"), tt.Count(x => x == "Nghỉ phép"), tt.Count(x => x == "Vắng"), tt.Count(x => x is null)).ToString();
            c.TinhHinh = "Đơn vị duy trì nghiêm chế độ trực ban, quân số cán bộ đảm bảo, các lớp lên lớp đúng giờ. An ninh trật tự khu vực khoa ổn định.";
            c.BanGiao = "Bàn giao đủ sổ trực, chìa khoá phòng làm việc và trang thiết bị cho ca sau.";
            c.ChotLuc = new DateTimeOffset(c.Ngay.Date.AddHours(17).AddMinutes(15));
        }
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }
}
