// Vai trò tệp: Dựng cây tổ chức và đào tạo từ dữ liệu trong phạm vi được phép xem.
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Một nút trên sơ đồ (đơn vị, không vẽ từng người). Người (cán bộ, học viên) nằm ở panel chi tiết.</summary>
public sealed class NutSoDo
{
    public string Khoa { get; init; } = "";
    public string Loai { get; init; } = "";
    public string Ten { get; init; } = "";
    public string? Ma { get; init; }
    public string BieuTuong { get; init; } = "folder";
    public string? DuongDan { get; init; }
    public string? MoTa { get; init; }
    public string? CanhBao { get; init; }
    public int SoCanBo { get; init; }
    public int SoLop { get; init; }
    public int SoHocVien { get; init; }
    public int SoTo { get; init; }
    public NutSoDo? Cha { get; set; }
    /// <summary>Lãnh đạo đơn vị (trưởng/phó khoa, tổ trưởng/tổ phó) để panel hiện ngay "ai phụ trách".</summary>
    public List<NguoiLanhDao> LanhDao { get; } = [];
    public string? GioiThieu { get; init; }
    public Dictionary<string, int> CoCauHocVi { get; init; } = [];
    public List<string> ChuyenDe { get; init; } = [];
    public List<string> LopPhuTrach { get; init; } = [];
    public List<NutSoDo> Con { get; } = [];
    public int SoCanRaSoat => (CanhBao is null ? 0 : 1) + Con.Sum(c => c.SoCanRaSoat);
    public IEnumerable<NutSoDo> DuongDiTuGoc() { var ds = new List<NutSoDo>(); for (var n = this; n is not null; n = n.Cha) ds.Insert(0, n); return ds; }
    public IEnumerable<NutSoDo> TatCa() => Con.SelectMany(c => c.TatCa()).Prepend(this);
}

public sealed record NguoiLanhDao(string ChucVu, string HoTen, string PhuChu);

/// <summary>Toạ độ một thẻ trên canvas, đã tính sẵn ở máy chủ (cây ngang đơn giản).</summary>
public sealed record ViTriThe(NutSoDo Nut, double X, double Y, bool LaGom, int SoGom);

/// <summary>
/// Dựng hai sơ đồ từ các mặt cắt đã lọc quyền (nên mỗi vai trò chỉ thấy phần được phép):
/// Tổ chức – Cán bộ: Học viện → Khoa → Tổ chuyên môn (cán bộ ở panel);
/// Đào tạo: Học viện → Hệ → Khoá → Lớp (học viên ở panel).
/// Ý tưởng lấy từ cây tổ chức của repo LTMT: chỉ vẽ đơn vị, mỗi nhánh tối đa 8 thẻ, phần dư gom thành "+ N mục nữa".
/// </summary>
public sealed class DichVuSoDoToChuc(DichVuQuanLyDuLieuDemo duLieu, DichVuDaoTao daoTao)
{
    public const int SoConToiDa = 8;
    public const double RongThe = 232, CaoThe = 92, KhoangNgang = 72, KhoangDoc = 18;

    private static Dictionary<string, int> CoCau(IEnumerable<CanBoGiangVien> cb)
        => cb.GroupBy(c => c.HocVi).OrderBy(g => g.Key == "Tiến sĩ" ? 0 : g.Key == "Thạc sĩ" ? 1 : 2).ToDictionary(g => g.Key, g => g.Count());
    private static string MoTaCoCau(Dictionary<string, int> cc) => string.Join(", ", cc.Select(x => $"{x.Value} {x.Key.ToLowerInvariant()}"));

    public async Task<NutSoDo> LayCayToChucAsync()
    {
        var khoa = duLieu.LayDanhSachKhoa();
        var to = duLieu.LayDanhSachTo();
        var canBo = duLieu.LayDanhSachCanBo();
        var chuyenDe = await daoTao.LayChuyenDeAsync();
        var dauTuan = QuyTacHocVu.DauTuan(DateTime.Today);
        var lichTuan = await daoTao.LayLichAsync(dauTuan, dauTuan.AddDays(6));
        var maCdTheoKhoa = chuyenDe.GroupBy(c => c.ChuyenDe.MaKhoa).ToDictionary(g => g.Key, g => g.Select(c => c.ChuyenDe.MaChuyenDe).ToHashSet());
        NguoiLanhDao? LanhDao(string? ma, string chucVu) => canBo.FirstOrDefault(c => c.MaTraCuuNoiBo == ma) is { } c ? new(chucVu, $"{c.CapBacHam} {c.HoTen}", $"{c.HocVi}{(c.HocHam != "" ? ", " + c.HocHam : "")}") : null;
        var goc = new NutSoDo { Khoa = "goc", Loai = "Học viện", Ten = "Học viện An ninh nhân dân", BieuTuong = "account_balance", SoCanBo = canBo.Count, SoTo = to.Count, MoTa = $"{khoa.Count} khoa · {to.Count} tổ · {canBo.Count} cán bộ" };
        foreach (var k in khoa)
        {
            var truong = canBo.FirstOrDefault(c => c.MaTraCuuNoiBo == k.MaTruongKhoa);
            var cacTo = to.Where(t => t.MaKhoa == k.MaKhoa).ToList();
            var cbKhoa = canBo.Where(c => c.MaKhoa == k.MaKhoa).ToList();
            var cdKhoa = chuyenDe.Where(c => c.ChuyenDe.MaKhoa == k.MaKhoa).Select(c => c.ChuyenDe.TenChuyenDe).ToList();
            var maCd = maCdTheoKhoa.GetValueOrDefault(k.MaKhoa, []);
            var lopKhoa = lichTuan.Where(b => maCd.Contains(b.Buoi.MaChuyenDe)).Select(b => b.Buoi.MaLop).Distinct().OrderBy(x => x).ToList();
            var coCauKhoa = CoCau(cbKhoa);
            var nutKhoa = new NutSoDo
            {
                Khoa = "k:" + k.MaKhoa, Loai = "Khoa", Ten = k.TenKhoa, Ma = k.MaKhoa, BieuTuong = "domain", SoCanBo = k.SiSo, SoTo = cacTo.Count,
                DuongDan = $"/khoa/{Uri.EscapeDataString(k.MaKhoa)}/to", Cha = goc,
                MoTa = truong is null ? null : $"Trưởng khoa: {truong.CapBacHam} {truong.HoTen}",
                CanhBao = k.MaTruongKhoa is null ? "Chưa có trưởng khoa." : cacTo.Count == 0 ? "Khoa chưa có tổ chuyên môn." : null,
                CoCauHocVi = coCauKhoa, ChuyenDe = cdKhoa, LopPhuTrach = lopKhoa,
                GioiThieu = $"{k.TenKhoa} gồm {cacTo.Count} tổ chuyên môn với {cbKhoa.Count} cán bộ ({MoTaCoCau(coCauKhoa)}). "
                    + (cdKhoa.Count > 0 ? $"Khoa phụ trách giảng dạy {cdKhoa.Count} chuyên đề; tuần này đang giảng dạy cho {lopKhoa.Count} lớp." : "Khoa chưa được giao chuyên đề.")
                    + (string.IsNullOrWhiteSpace(k.MoTa) ? "" : " " + k.MoTa)
            };
            foreach (var ld in new[] { LanhDao(k.MaTruongKhoa, "Trưởng khoa"), LanhDao(k.MaPhoKhoa, "Phó khoa"), LanhDao(k.MaPhoKhoaThuHai, "Phó khoa") })
                if (ld is not null) nutKhoa.LanhDao.Add(ld);
            foreach (var t in cacTo)
            {
                var toTruong = canBo.FirstOrDefault(c => c.MaTraCuuNoiBo == t.MaToTruong);
                var cbTo = canBo.Where(c => c.MaTo == t.MaTo).ToList();
                var coCauTo = CoCau(cbTo);
                var chuyenMon = cbTo.Select(c => c.ChuyenMon).Where(x => x != "").Distinct().ToList();
                var nutTo = new NutSoDo
                {
                    Khoa = "t:" + t.MaTo, Loai = "Tổ chuyên môn", Ten = t.TenTo, Ma = t.MaTo, BieuTuong = "groups", SoCanBo = t.SiSo, Cha = nutKhoa,
                    DuongDan = $"/khoa/{Uri.EscapeDataString(k.MaKhoa)}/to/{Uri.EscapeDataString(t.MaTo)}",
                    MoTa = toTruong is null ? null : $"Tổ trưởng: {toTruong.CapBacHam} {toTruong.HoTen}",
                    CanhBao = t.MaToTruong is null ? "Chưa có tổ trưởng." : t.SiSo == 0 ? "Tổ chưa có cán bộ." : null,
                    CoCauHocVi = coCauTo, ChuyenDe = cdKhoa,
                    GioiThieu = $"{t.TenTo} ({t.MaTo}) là tổ chuyên môn trực thuộc {k.TenKhoa}, gồm {cbTo.Count} cán bộ ({MoTaCoCau(coCauTo)})."
                        + (chuyenMon.Count > 0 ? $" Lĩnh vực chuyên môn: {string.Join(", ", chuyenMon).ToLowerInvariant()}." : "")
                        + (cdKhoa.Count > 0 ? $" Cán bộ của tổ tham gia giảng dạy các chuyên đề do khoa phụ trách." : "")
                };
                foreach (var ld in new[] { LanhDao(t.MaToTruong, "Tổ trưởng"), LanhDao(t.MaToPho, "Tổ phó") })
                    if (ld is not null) nutTo.LanhDao.Add(ld);
                nutKhoa.Con.Add(nutTo);
            }
            goc.Con.Add(nutKhoa);
        }
        return goc;
    }

    public NutSoDo LayCayDaoTao()
    {
        var lop = duLieu.LayDanhSachLop();
        var goc = new NutSoDo { Khoa = "goc", Loai = "Học viện", Ten = "Học viện An ninh nhân dân", BieuTuong = "account_balance", SoLop = lop.Count, SoHocVien = lop.Sum(l => l.SiSo), MoTa = $"{lop.Count} lớp · {lop.Sum(l => l.SiSo):N0} học viên" };
        string[] thuTuHe = ["Chính quy", "Liên thông", "Văn bằng 2"];
        foreach (var he in lop.GroupBy(l => l.HeDaoTao).OrderBy(g => Array.IndexOf(thuTuHe, g.Key)))
        {
            var nutHe = new NutSoDo { Khoa = "h:" + he.Key, Loai = "Hệ đào tạo", Ten = "Hệ " + he.Key.ToLowerInvariant(), BieuTuong = "school", SoLop = he.Count(), SoHocVien = he.Sum(l => l.SiSo), Cha = goc, MoTa = $"{he.Select(l => l.KhoaHoc).Distinct().Count()} khoá" };
            foreach (var kh in he.GroupBy(l => l.KhoaHoc).OrderByDescending(g => g.Key))
            {
                var dau = kh.First();
                var nutKh = new NutSoDo { Khoa = $"kh:{he.Key}/{kh.Key}", Loai = "Khoá", Ten = "Khoá " + kh.Key, Ma = kh.Key, BieuTuong = "event_note", SoLop = kh.Count(), SoHocVien = kh.Sum(l => l.SiSo), Cha = nutHe, MoTa = "Niên khoá " + dau.NienKhoa };
                foreach (var l in kh.OrderBy(l => SoLop(l.MaLop)))
                    nutKh.Con.Add(new NutSoDo
                    {
                        Khoa = "l:" + l.MaLop, Loai = "Lớp", Ten = l.MaLop, Ma = l.MaLop, BieuTuong = "groups", SoHocVien = l.SiSo, SoLop = 1, Cha = nutKh,
                        DuongDan = $"/quan-ly-hoc-vien/lop/{Uri.EscapeDataString(l.MaLop)}", MoTa = $"{l.ChuyenNganh} · GVCN {l.GiaoVienChuNhiem}",
                        CanhBao = l.SiSo == 0 ? "Lớp chưa có học viên." : string.IsNullOrWhiteSpace(l.GiaoVienChuNhiem) ? "Lớp chưa có giáo viên chủ nhiệm." : null
                    });
                nutHe.Con.Add(nutKh);
            }
            goc.Con.Add(nutHe);
        }
        return goc;
        static int SoLop(string ma) => int.TryParse(new string(ma.Skip(1).TakeWhile(char.IsDigit).ToArray()), out var so) ? so : 0;
    }

    /// <summary>
    /// Bố cục cây ngang: gốc bên trái, con bên phải; nút lá xếp dọc, nút cha nằm giữa các con.
    /// Chỉ vẽ nút đang mở; mỗi nhánh tối đa <see cref="SoConToiDa"/> thẻ, phần dư gom thành một thẻ.
    /// </summary>
    public static (List<ViTriThe> The, double Rong, double Cao) BoCuc(NutSoDo goc, ISet<string> dangMo, ISet<string> moHet)
    {
        var ketQua = new List<ViTriThe>();
        var y = 0.0;
        var sauNhat = 0;
        double Dat(NutSoDo nut, int sau)
        {
            sauNhat = Math.Max(sauNhat, sau);
            var x = sau * (RongThe + KhoangNgang);
            if (!dangMo.Contains(nut.Khoa) || nut.Con.Count == 0)
            {
                var yLa = y; y += CaoThe + KhoangDoc;
                ketQua.Add(new(nut, x, yLa, false, 0));
                return yLa;
            }
            var cacCon = moHet.Contains(nut.Khoa) ? nut.Con : nut.Con.Take(SoConToiDa).ToList();
            var cacY = cacCon.Select(c => Dat(c, sau + 1)).ToList();
            if (cacCon.Count < nut.Con.Count)
            {
                sauNhat = Math.Max(sauNhat, sau + 1);
                cacY.Add(y);
                ketQua.Add(new(nut, (sau + 1) * (RongThe + KhoangNgang), y, true, nut.Con.Count - cacCon.Count));
                y += CaoThe + KhoangDoc;
            }
            var yCha = (cacY.First() + cacY.Last()) / 2;
            ketQua.Add(new(nut, x, yCha, false, 0));
            return yCha;
        }
        Dat(goc, 0);
        return (ketQua, (sauNhat + 1) * (RongThe + KhoangNgang) - KhoangNgang, Math.Max(CaoThe, y - KhoangDoc));
    }
}
