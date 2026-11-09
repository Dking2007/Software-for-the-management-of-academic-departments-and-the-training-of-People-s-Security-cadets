using ClosedXML.Excel;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Một phần người dùng có thể chọn đưa vào tệp xuất (ví dụ "Bảng điểm", "Lịch học").</summary>
public sealed record PhanXuat(string Ma, string Ten);

/// <summary>
/// Báo cáo Excel nhiều trang và xuất theo khoảng ngày tự chọn.
/// Mọi số liệu lấy qua dịch vụ nghiệp vụ nên đã cắt đúng quyền của người đang đăng nhập; tệp không chứa thứ màn hình không cho xem.
/// Mỗi lần xuất được ghi nhật ký.
/// </summary>
public sealed class DichVuBaoCaoExcel(DichVuDaoTao daoTao, DichVuChuongTrinh chuongTrinh, DichVuHocVu hocVu, DichVuQuanLyDuLieuDemo duLieu, DichVuNhatKy nhatKy, DichVuPhanQuyenDemo phanQuyen)
{
    public const int SoNgayToiDa = 366;
    /// <summary>Bảng công dạng ma trận chỉ lập khi kỳ báo cáo không quá số ngày này, dài hơn thì quá nhiều cột để đọc.</summary>
    public const int SoNgayToiDaBangCong = 62;

    public static readonly PhanXuat[] PhanHocTap = [new("diem", "Bảng điểm toàn khoá"), new("diem-danh", "Lịch sử điểm danh"), new("lich", "Lịch học"), new("lich-thi", "Lịch thi")];
    public static readonly PhanXuat[] PhanGiangDay = [new("lich", "Lịch dạy"), new("tong-hop", "Tổng hợp theo lớp, chuyên đề"), new("chua-dd", "Buổi đã qua chưa điểm danh"), new("cham-cong", "Điểm danh đi làm của tôi"), new("lop", "Danh sách lớp tôi dạy")];

    /// <summary>Lý do khoảng ngày không hợp lệ, null nếu hợp lệ.</summary>
    public static string? KiemTraKhoang(DateTime tu, DateTime den)
    {
        if (den.Date < tu.Date) return "Ngày kết thúc phải từ ngày bắt đầu trở đi.";
        if ((den.Date - tu.Date).Days + 1 > SoNgayToiDa) return $"Mỗi lần xuất tối đa {SoNgayToiDa} ngày. Hãy thu hẹp khoảng thời gian.";
        return null;
    }

    /// <summary>Các trường để tìm kiếm tự do trên lịch; trang Lịch và tệp xuất dùng chung để luôn ra cùng kết quả.</summary>
    public static string?[] TruongTimLich(BuoiHocHienThi b)
        => [b.Buoi.MaLop, b.TenChuyenDe, b.TenGiangVien, b.TenPhong, b.Buoi.MaChuyenDe, QuyTacHocVu.Buoi(b.Buoi.TietBatDau), QuyTacHocVu.TenThu(b.Buoi.Ngay), b.Buoi.Ngay.ToString("dd/MM")];

    public static string TrangThaiBuoi(BuoiHocHienThi b)
        => b.DaDiemDanh ? "Đã điểm danh" : b.Buoi.Ngay.Date < DateTime.Today ? "Chưa điểm danh" : b.Buoi.Ngay.Date == DateTime.Today ? "Hôm nay" : "Sắp tới";

    private async Task Ghi(string doiTuong, int soDong) => await nhatKy.GhiAsync("Xuất Excel", doiTuong, $"{soDong} dòng");

    private static byte[] Luu(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string Khoang(DateTime tu, DateTime den) => $"Từ {tu:dd/MM/yyyy} đến {den:dd/MM/yyyy}";

    // ---------- Lịch đào tạo, lịch dạy, lịch học ----------

    public async Task<byte[]?> XuatLichAsync(DateTime tu, DateTime den, string? maLop = null, string? maCanBo = null, string? maPhong = null, string? tuKhoa = null)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || nd.VaiTro == "Guest") return null;
        var ds = await LayLichTheoLoc(tu, den, maLop, maCanBo, maPhong, tuKhoa);
        var phamVi = nd.LaGiangVien ? "Lịch dạy của tôi" : nd.LaHocVien ? "Lịch học của lớp" : maLop is not null ? $"Lớp {maLop}" : maPhong is not null ? $"Phòng {maPhong}" : maCanBo is not null ? "Một giảng viên" : "Toàn Học viện";
        await Ghi($"Lịch đào tạo ({Khoang(tu, den)})", ds.Count);

        using var wb = new XLWorkbook();
        var moTa = $"{phamVi} · {Khoang(tu, den)}{(string.IsNullOrWhiteSpace(tuKhoa) ? "" : $" · từ khoá \"{tuKhoa.Trim()}\"")}";
        // Thời khoá biểu chỉ dễ đọc khi lịch của một đối tượng; toàn trường có nhiều lớp cùng giờ nên chỉ lập bảng chi tiết và tổng hợp.
        if (nd.LaGiangVien || nd.LaHocVien || maLop is not null || maCanBo is not null || maPhong is not null)
            ThemThoiKhoaBieu(wb, "Thời khoá biểu", moTa, ds, nd.LaGiangVien || maCanBo is not null);
        ThemChiTietLich(wb, "Lịch chi tiết", moTa, ds, nd.CoQuyenVung("DaoTao"));
        ThemTongHopLich(wb, moTa, ds);
        return Luu(wb);
    }

    public async Task<string?> XemTruocLichAsync(DateTime tu, DateTime den, string? maLop = null, string? maCanBo = null, string? maPhong = null, string? tuKhoa = null)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var ds = await LayLichTheoLoc(tu, den, maLop, maCanBo, maPhong, tuKhoa);
        return $"{ds.Count:N0} buổi học, {ds.Sum(b => b.Buoi.SoTiet):N0} tiết, {ds.Select(b => b.Buoi.MaLop).Distinct().Count()} lớp.";
    }

    private async Task<List<BuoiHocHienThi>> LayLichTheoLoc(DateTime tu, DateTime den, string? maLop, string? maCanBo, string? maPhong, string? tuKhoa)
        => TimKiemMo.Loc(await daoTao.LayLichAsync(tu, den, maLop, maCanBo, maPhong), tuKhoa, TruongTimLich)
            .OrderBy(b => b.Buoi.Ngay).ThenBy(b => b.Buoi.TietBatDau).ThenBy(b => b.Buoi.MaLop).ToList();

    private static void ThemChiTietLich(XLWorkbook wb, string tenTrang, string moTa, List<BuoiHocHienThi> ds, bool coMaXepLich)
    {
        string[] cot = coMaXepLich
            ? ["Ngày", "Thứ", "Buổi", "Tiết", "Giờ", "Lớp", "Mã chuyên đề", "Chuyên đề", "Mã cán bộ", "Giảng viên", "Mã phòng", "Phòng", "Sĩ số", "Trạng thái", "Ghi chú"]
            : ["Ngày", "Thứ", "Buổi", "Tiết", "Giờ", "Lớp", "Mã chuyên đề", "Chuyên đề", "Giảng viên", "Phòng", "Sĩ số", "Trạng thái", "Ghi chú"];
        DichVuExcel.ThemTrang(wb, tenTrang, "Lịch đào tạo chi tiết", cot, ds.Select(b => coMaXepLich
            ? new object?[] { b.Buoi.Ngay.Date, QuyTacHocVu.TenThu(b.Buoi.Ngay), QuyTacHocVu.Buoi(b.Buoi.TietBatDau), $"{b.Buoi.TietBatDau}–{b.Buoi.TietKetThuc}", QuyTacHocVu.KhungGio(b.Buoi), b.Buoi.MaLop, b.Buoi.MaChuyenDe, b.TenChuyenDe, b.Buoi.MaCanBo, b.TenGiangVien, b.Buoi.MaPhong, b.TenPhong, b.SiSo, TrangThaiBuoi(b), b.Buoi.GhiChu }
            : new object?[] { b.Buoi.Ngay.Date, QuyTacHocVu.TenThu(b.Buoi.Ngay), QuyTacHocVu.Buoi(b.Buoi.TietBatDau), $"{b.Buoi.TietBatDau}–{b.Buoi.TietKetThuc}", QuyTacHocVu.KhungGio(b.Buoi), b.Buoi.MaLop, b.Buoi.MaChuyenDe, b.TenChuyenDe, b.TenGiangVien, b.TenPhong, b.SiSo, TrangThaiBuoi(b), b.Buoi.GhiChu }), moTa);
    }

    private static void ThemTongHopLich(XLWorkbook wb, string moTa, List<BuoiHocHienThi> ds)
    {
        static IEnumerable<object?[]> Nhom(string loai, IEnumerable<IGrouping<string, BuoiHocHienThi>> cacNhom)
            => cacNhom.OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => new object?[]
                { loai, g.Key, g.Count(), g.Sum(b => b.Buoi.SoTiet), g.Count(b => b.DaDiemDanh), g.Count(b => !b.DaDiemDanh && b.Buoi.Ngay.Date < DateTime.Today) });
        var dong = Nhom("Lớp", ds.GroupBy(b => b.Buoi.MaLop))
            .Concat(Nhom("Chuyên đề", ds.GroupBy(b => b.TenChuyenDe)))
            .Concat(Nhom("Giảng viên", ds.GroupBy(b => b.TenGiangVien)))
            .Concat(Nhom("Phòng", ds.GroupBy(b => b.TenPhong)));
        DichVuExcel.ThemTrang(wb, "Tổng hợp", "Tổng hợp số buổi, số tiết", ["Nhóm", "Tên", "Số buổi", "Số tiết", "Đã điểm danh", "Đã qua chưa điểm danh"], dong, moTa);
    }

    /// <summary>Mỗi tuần một khối: hàng là buổi (sáng, chiều, tối), cột là thứ Hai đến thứ Bảy.</summary>
    private static void ThemThoiKhoaBieu(XLWorkbook wb, string tenTrang, string moTa, List<BuoiHocHienThi> ds, bool hienLopThayGiangVien)
    {
        var ws = wb.Worksheets.Add(DichVuExcel.TenTrang(tenTrang));
        DichVuExcel.TieuDeTrang(ws, "Thời khoá biểu", 7, moTa);
        var r = 4;
        if (ds.Count == 0) ws.Cell(r, 1).Value = "Không có buổi học trong khoảng thời gian đã chọn.";
        foreach (var tuan in ds.GroupBy(b => QuyTacHocVu.DauTuan(b.Buoi.Ngay)).OrderBy(g => g.Key))
        {
            ws.Cell(r, 1).Value = $"Tuần {tuan.Key:dd/MM} – {tuan.Key.AddDays(5):dd/MM/yyyy}";
            ws.Range(r, 1, r, 7).Merge().Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#1B4B34")).Fill.SetBackgroundColor(XLColor.FromHtml("#E3EEE6"));
            r++;
            ws.Cell(r, 1).Value = "Buổi";
            for (var d = 0; d < 6; d++) ws.Cell(r, d + 2).Value = $"{QuyTacHocVu.TenThu(tuan.Key.AddDays(d))} {tuan.Key.AddDays(d):dd/MM}";
            ws.Range(r, 1, r, 7).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4B34")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            var dauBang = r;
            r++;
            foreach (var buoi in new[] { "Sáng", "Chiều", "Tối" })
            {
                if (buoi == "Tối" && !tuan.Any(b => QuyTacHocVu.Buoi(b.Buoi.TietBatDau) == "Tối")) continue;
                ws.Cell(r, 1).Value = buoi;
                ws.Cell(r, 1).Style.Font.SetBold().Alignment.SetVertical(XLAlignmentVerticalValues.Center).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                for (var d = 0; d < 6; d++)
                {
                    var ngay = tuan.Key.AddDays(d);
                    var cacBuoi = tuan.Where(b => b.Buoi.Ngay.Date == ngay && QuyTacHocVu.Buoi(b.Buoi.TietBatDau) == buoi).OrderBy(b => b.Buoi.TietBatDau)
                        .Select(b => $"Tiết {b.Buoi.TietBatDau}–{b.Buoi.TietKetThuc} ({QuyTacHocVu.KhungGio(b.Buoi)})\n{b.TenChuyenDe}\n{(hienLopThayGiangVien ? $"Lớp {b.Buoi.MaLop}" : b.TenGiangVien)}\n{b.TenPhong}");
                    var o = ws.Cell(r, d + 2);
                    o.Value = string.Join("\n\n", cacBuoi);
                    o.Style.Alignment.SetWrapText().Alignment.SetVertical(XLAlignmentVerticalValues.Top);
                }
                r++;
            }
            ws.Range(dauBang, 1, r - 1, 7).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorderColor(XLColor.FromHtml("#DCE0DA"));
            r++;
        }
        ws.Column(1).Width = 9;
        ws.Columns(2, 7).Width = 30;
    }

    // ---------- Báo cáo điểm danh đi làm ----------

    private static bool DiLam(string trangThai) => trangThai is "Có mặt" or "Đi muộn" or "Công tác";
    private static List<DateTime> NgayLamViec(DateTime tu, DateTime den)
    {
        var cuoi = den.Date < DateTime.Today ? den.Date : DateTime.Today;
        var kq = new List<DateTime>();
        for (var d = tu.Date; d <= cuoi; d = d.AddDays(1)) if (d.DayOfWeek != DayOfWeek.Sunday) kq.Add(d);
        return kq;
    }

    private sealed record TongHopCong(int SoNgay, int CoMat, int DiMuon, int CongTac, int NghiPhep, int Vang)
    {
        public int DaGhi => CoMat + DiMuon + CongTac + NghiPhep + Vang;
        public int ChuaGhi => SoNgay - DaGhi;
        public double TiLeDiLam => DaGhi == 0 ? 0 : (double)(CoMat + DiMuon + CongTac) / DaGhi;
    }

    private static TongHopCong Cong(IEnumerable<ChamCong> cong, int soNgay)
    {
        var ds = cong.ToList();
        return new(soNgay, ds.Count(c => c.TrangThai == "Có mặt"), ds.Count(c => c.TrangThai == "Đi muộn"), ds.Count(c => c.TrangThai == "Công tác"), ds.Count(c => c.TrangThai == "Nghỉ phép"), ds.Count(c => c.TrangThai == "Vắng"));
    }

    public async Task<string?> XemTruocChamCongAsync(DateTime tu, DateTime den, string? maKhoa = null)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var (dong, loi) = await chuongTrinh.LayBangCongKhoangAsync(tu, den, maKhoa);
        if (loi is not null) return loi;
        var ngay = NgayLamViec(tu, den);
        var bo = ngay.ToHashSet();
        var th = Cong(dong.SelectMany(d => d.Cong.Values).Where(c => bo.Contains(c.Ngay.Date)), ngay.Count * dong.Count);
        return $"{dong.Count} cán bộ, {ngay.Count} ngày làm việc. Đã ghi {th.DaGhi:N0}/{th.SoNgay:N0} lượt công, tỉ lệ đi làm {th.TiLeDiLam:P1}.";
    }

    /// <summary>Báo cáo điểm danh đi làm: tổng quan kèm phần cần chú ý, theo cán bộ, theo ngày và bảng công.</summary>
    public async Task<byte[]?> XuatBaoCaoChamCongAsync(DateTime tu, DateTime den, string? maKhoa = null)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var (dong, loi) = await chuongTrinh.LayBangCongKhoangAsync(tu, den, maKhoa);
        if (loi is not null) return null;
        var nd = await phanQuyen.LayNguoiDungAsync();
        var ngay = NgayLamViec(tu, den);
        var boNgay = ngay.ToHashSet();
        var tenKhoa = duLieu.LayDanhSachKhoa().ToDictionary(k => k.MaKhoa, k => k.TenKhoa);
        var theoCanBo = dong.Select(d => (Dong: d, Th: Cong(d.Cong.Values.Where(c => boNgay.Contains(c.Ngay.Date)), ngay.Count))).ToList();
        var tong = Cong(dong.SelectMany(d => d.Cong.Values).Where(c => boNgay.Contains(c.Ngay.Date)), ngay.Count * dong.Count);
        var phamVi = maKhoa is null ? "Toàn Học viện" : tenKhoa.GetValueOrDefault(maKhoa, maKhoa);
        var moTa = $"{phamVi} · {Khoang(tu, den)}";
        await Ghi($"Báo cáo điểm danh đi làm ({moTa})", dong.Count);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Báo cáo");
        DichVuExcel.TieuDeTrang(ws, "Báo cáo điểm danh đi làm", 9, $"{moTa} · Người lập: {nd.Ten} ({nd.TenVaiTro})");
        var r = DichVuExcel.BangNho(ws, 4, "1. Số liệu chung", ["Chỉ tiêu", "Giá trị"], [
            ["Số cán bộ", dong.Count], ["Số ngày làm việc đã qua trong kỳ (thứ Hai – thứ Bảy)", ngay.Count], ["Số lượt công cần ghi", tong.SoNgay], ["Đã ghi", tong.DaGhi], ["Chưa ghi", tong.ChuaGhi],
            ["Có mặt", tong.CoMat], ["Đi muộn", tong.DiMuon], ["Công tác", tong.CongTac], ["Nghỉ phép", tong.NghiPhep], ["Vắng", tong.Vang],
            ["Tỉ lệ đi làm (trên số lượt đã ghi)", new DichVuExcel.PhanTram(tong.TiLeDiLam)]]);
        r = DichVuExcel.BangNho(ws, r + 1, "2. Theo khoa", ["Khoa", "Cán bộ", "Có mặt", "Đi muộn", "Công tác", "Nghỉ phép", "Vắng", "Chưa ghi", "Tỉ lệ đi làm"],
            theoCanBo.GroupBy(x => x.Dong.CanBo.MaKhoa).OrderBy(g => g.Key).Select(g =>
            {
                var t = Cong(g.SelectMany(x => x.Dong.Cong.Values).Where(c => boNgay.Contains(c.Ngay.Date)), ngay.Count * g.Count());
                return new object?[] { tenKhoa.GetValueOrDefault(g.Key, g.Key), g.Count(), t.CoMat, t.DiMuon, t.CongTac, t.NghiPhep, t.Vang, t.ChuaGhi, new DichVuExcel.PhanTram(t.TiLeDiLam) };
            }));
        var canChuY = theoCanBo.Where(x => x.Th.Vang + x.Th.DiMuon > 0).OrderByDescending(x => x.Th.Vang).ThenByDescending(x => x.Th.DiMuon).Take(15).ToList();
        r = DichVuExcel.BangNho(ws, r + 1, "3. Cần chú ý (vắng, đi muộn nhiều nhất)", ["Cán bộ", "Khoa", "Vắng", "Đi muộn", "Nghỉ phép"],
            canChuY.Select(x => new object?[] { $"{x.Dong.CanBo.CapBacHam} {x.Dong.CanBo.HoTen}", tenKhoa.GetValueOrDefault(x.Dong.CanBo.MaKhoa, x.Dong.CanBo.MaKhoa), x.Th.Vang, x.Th.DiMuon, x.Th.NghiPhep }));
        if (canChuY.Count == 0) ws.Cell(r - 1, 1).Value = "Không có cán bộ vắng hoặc đi muộn trong kỳ.";
        ws.Column(1).Width = 46;
        ws.Columns(2, 9).Width = 13;

        DichVuExcel.ThemTrang(wb, "Theo cán bộ", "Tổng hợp công theo cán bộ", ["Mã cán bộ", "Họ và tên", "Cấp bậc hàm", "Chức vụ", "Khoa", "Tổ", "Ngày làm việc", "Có mặt", "Đi muộn", "Công tác", "Nghỉ phép", "Vắng", "Chưa ghi", "Tỉ lệ đi làm"],
            theoCanBo.Select(x => new object?[] { x.Dong.CanBo.MaCanBo, x.Dong.CanBo.HoTen, x.Dong.CanBo.CapBacHam, x.Dong.CanBo.ChucVu, tenKhoa.GetValueOrDefault(x.Dong.CanBo.MaKhoa, x.Dong.CanBo.MaKhoa), x.Dong.TenTo, x.Th.SoNgay, x.Th.CoMat, x.Th.DiMuon, x.Th.CongTac, x.Th.NghiPhep, x.Th.Vang, x.Th.ChuaGhi, new DichVuExcel.PhanTram(x.Th.TiLeDiLam) }), moTa);

        DichVuExcel.ThemTrang(wb, "Theo ngày", "Tổng hợp công theo ngày", ["Ngày", "Thứ", "Có mặt", "Đi muộn", "Công tác", "Nghỉ phép", "Vắng", "Chưa ghi", "Tỉ lệ đi làm"],
            ngay.Select(n =>
            {
                var t = Cong(dong.Select(d => d.Cong.GetValueOrDefault(n)).OfType<ChamCong>(), dong.Count);
                return new object?[] { n, QuyTacHocVu.TenThu(n), t.CoMat, t.DiMuon, t.CongTac, t.NghiPhep, t.Vang, t.ChuaGhi, new DichVuExcel.PhanTram(t.TiLeDiLam) };
            }), moTa);

        if (ngay.Count is > 0 and <= SoNgayToiDaBangCong) ThemBangCong(wb, moTa, dong, ngay);
        return Luu(wb);
    }

    private static readonly Dictionary<string, (string KyHieu, string Nen, string Chu)> MauCong = new()
    {
        ["Có mặt"] = ("X", "#DCFCE7", "#166534"), ["Đi muộn"] = ("M", "#FEF3C7", "#92400E"), ["Công tác"] = ("CT", "#DBEAFE", "#1E40AF"), ["Nghỉ phép"] = ("P", "#EDE9FE", "#5B21B6"), ["Vắng"] = ("V", "#FEE2E2", "#B91C1C")
    };

    private static void ThemBangCong(XLWorkbook wb, string moTa, List<DichVuChuongTrinh.DongCongTuan> dong, List<DateTime> ngay)
    {
        var ws = wb.Worksheets.Add("Bảng công");
        var soCot = ngay.Count + 3;
        DichVuExcel.TieuDeTrang(ws, "Bảng công", soCot, $"{moTa} · X có mặt, M đi muộn, CT công tác, P nghỉ phép, V vắng, ô trống là chưa ghi");
        ws.Cell(4, 1).Value = "Họ và tên";
        ws.Cell(4, 2).Value = "Tổ";
        for (var i = 0; i < ngay.Count; i++)
        {
            ws.Cell(4, i + 3).Value = ngay[i].ToString("dd/MM");
            ws.Column(i + 3).Width = 6;
        }
        ws.Cell(4, soCot).Value = "Đi làm";
        ws.Range(4, 1, 4, soCot).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4B34")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        var r = 5;
        foreach (var d in dong)
        {
            ws.Cell(r, 1).Value = $"{d.CanBo.CapBacHam} {d.CanBo.HoTen}";
            ws.Cell(r, 2).Value = d.TenTo == "" ? "Lãnh đạo khoa" : d.TenTo;
            var diLam = 0;
            for (var i = 0; i < ngay.Count; i++)
            {
                if (d.Cong.GetValueOrDefault(ngay[i]) is not { } c || !MauCong.TryGetValue(c.TrangThai, out var m)) continue;
                if (DiLam(c.TrangThai)) diLam++;
                var o = ws.Cell(r, i + 3);
                o.Value = m.KyHieu;
                o.Style.Font.SetFontColor(XLColor.FromHtml(m.Chu)).Fill.SetBackgroundColor(XLColor.FromHtml(m.Nen)).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }
            ws.Cell(r, soCot).Value = diLam;
            ws.Cell(r, soCot).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            r++;
        }
        ws.Range(4, 1, Math.Max(4, r - 1), soCot).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorderColor(XLColor.FromHtml("#DCE0DA"));
        ws.Column(1).Width = 32;
        ws.Column(2).Width = 16;
        ws.Column(soCot).Width = 8;
        ws.SheetView.FreezeRows(4);
        ws.SheetView.FreezeColumns(2);
    }

    // ---------- Giảng viên ----------

    public async Task<byte[]?> XuatGiangDayCuaToiAsync(DateTime tu, DateTime den, IReadOnlySet<string> phan)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaGiangVien) return null;
        var cb = duLieu.LayDanhSachCanBo().FirstOrDefault(c => c.MaTraCuuNoiBo == nd.MaCanBo);
        var ds = await daoTao.LayLichAsync(tu, den);
        var moTa = $"{cb?.CapBacHam} {cb?.HoTen} · {Khoang(tu, den)}";
        using var wb = new XLWorkbook();
        if (phan.Contains("lich"))
        {
            ThemThoiKhoaBieu(wb, "Thời khoá biểu", moTa, ds, true);
            ThemChiTietLich(wb, "Lịch dạy", moTa, ds, false);
        }
        if (phan.Contains("tong-hop"))
            DichVuExcel.ThemTrang(wb, "Tổng hợp giảng dạy", "Tổng hợp giảng dạy theo lớp, chuyên đề", ["Lớp", "Chuyên đề", "Số buổi", "Số tiết", "Đã điểm danh", "Đã qua chưa điểm danh"],
                ds.GroupBy(b => (b.Buoi.MaLop, b.TenChuyenDe)).OrderBy(g => g.Key.MaLop).ThenBy(g => g.Key.TenChuyenDe).Select(g => new object?[]
                    { g.Key.MaLop, g.Key.TenChuyenDe, g.Count(), g.Sum(b => b.Buoi.SoTiet), g.Count(b => b.DaDiemDanh), g.Count(b => !b.DaDiemDanh && b.Buoi.Ngay.Date < DateTime.Today) }), moTa);
        if (phan.Contains("chua-dd"))
            DichVuExcel.ThemTrang(wb, "Chưa điểm danh", "Buổi đã qua chưa điểm danh", ["Ngày", "Thứ", "Tiết", "Lớp", "Chuyên đề", "Phòng"],
                ds.Where(b => !b.DaDiemDanh && b.Buoi.Ngay.Date < DateTime.Today).Select(b => new object?[] { b.Buoi.Ngay.Date, QuyTacHocVu.TenThu(b.Buoi.Ngay), $"{b.Buoi.TietBatDau}–{b.Buoi.TietKetThuc}", b.Buoi.MaLop, b.TenChuyenDe, b.TenPhong }), moTa);
        var soDongCong = 0;
        if (phan.Contains("cham-cong"))
        {
            var cong = (await chuongTrinh.LayChamCongCuaToiKhoangAsync(tu, den)).ToDictionary(c => c.Ngay.Date);
            var ngay = NgayLamViec(tu, den);
            soDongCong = ngay.Count;
            var th = Cong(cong.Values.Where(c => ngay.Contains(c.Ngay.Date)), ngay.Count);
            DichVuExcel.ThemTrang(wb, "Điểm danh đi làm", "Điểm danh đi làm của tôi", ["Ngày", "Thứ", "Trạng thái", "Thời điểm ghi", "Người ghi"],
                ngay.Select(n => cong.GetValueOrDefault(n) is { } c
                    ? new object?[] { n, QuyTacHocVu.TenThu(n), c.TrangThai, c.ThoiDiem.ToLocalTime().DateTime, c.NguoiGhi }
                    : new object?[] { n, QuyTacHocVu.TenThu(n), "Chưa ghi", null, "" }),
                $"{moTa} · có mặt {th.CoMat}, đi muộn {th.DiMuon}, công tác {th.CongTac}, nghỉ phép {th.NghiPhep}, vắng {th.Vang}, chưa ghi {th.ChuaGhi}");
        }
        if (phan.Contains("lop"))
            DichVuExcel.ThemTrang(wb, "Lớp tôi dạy", "Danh sách lớp tôi dạy", ["Mã lớp", "Hệ đào tạo", "Khoá", "Niên khoá", "Chuyên ngành", "Sĩ số"],
                duLieu.LayDanhSachLop().Select(l => new object?[] { l.MaLop, l.HeDaoTao, l.KhoaHoc, l.NienKhoa, l.ChuyenNganh, l.SiSo }), moTa);
        if (wb.Worksheets.Count == 0) return null;
        await Ghi($"Giảng dạy của tôi ({Khoang(tu, den)})", ds.Count + soDongCong);
        return Luu(wb);
    }

    // ---------- Học viên ----------

    public async Task<byte[]?> XuatHocTapCuaToiAsync(DateTime tu, DateTime den, IReadOnlySet<string> phan)
    {
        if (KiemTraKhoang(tu, den) is not null) return null;
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaHocVien) return null;
        var toi = duLieu.LayDanhSachHocVien().FirstOrDefault(h => h.MaHocVien == nd.MaHocVien);
        if (toi is null) return null;
        var moTa = $"{toi.HoTen} · {toi.MaHocVien} · Lớp {toi.MaLop}";
        var moTaKy = $"{moTa} · {Khoang(tu, den)}";
        using var wb = new XLWorkbook();
        var soDong = 0;
        if (phan.Contains("diem"))
        {
            var bang = await hocVu.LayBangDiemToanKhoaCuaToiAsync();
            var diemTb = bang.SelectMany(h => h.MonHoc).Where(m => m.TongKet is not null).Select(m => m.TongKet!.Value).DefaultIfEmpty().Average();
            var coDiem = bang.Any(h => h.MonHoc.Any(m => m.TongKet is not null));
            var ws = wb.Worksheets.Add("Tổng kết học tập");
            DichVuExcel.TieuDeTrang(ws, "Kết quả học tập toàn khoá", 6, moTa);
            var r = DichVuExcel.BangNho(ws, 4, "Tổng kết chung", ["Chỉ tiêu", "Giá trị"], [
                ["Số học kỳ có kết quả", bang.Count], ["Số chuyên đề đã có điểm tổng kết", bang.Sum(h => h.MonHoc.Count(m => m.TongKet is not null))],
                ["Tổng số tiết đạt", bang.Sum(h => h.TietDat)], ["Điểm trung bình tích luỹ", coDiem ? Math.Round(diemTb, 2) : null], ["Học lực", QuyTacHocVu.HocLuc(coDiem ? diemTb : null)]]);
            DichVuExcel.BangNho(ws, r + 1, "Theo học kỳ", ["Năm học", "Học kỳ", "Số chuyên đề", "Số tiết", "Số tiết đạt", "Điểm trung bình"],
                bang.Select(h => new object?[] { h.NamHoc, $"HK{h.HocKy}{(h.HienTai ? " (hiện tại)" : "")}", h.MonHoc.Count, h.TongTiet, h.TietDat, h.DiemTrungBinh }));
            ws.Column(1).Width = 40;
            ws.Columns(2, 6).Width = 16;
            var dong = bang.SelectMany(h => h.MonHoc.Select(m => new object?[]
            {
                h.NamHoc, h.HocKy, m.MaChuyenDe, m.TenChuyenDe, h.SoTiet?.GetValueOrDefault(m.MaChuyenDe), m.ChuyenCan, m.DiemKiemTra, m.TongKet, m.XepLoai,
                QuyTacHocVu.QuyDoi(m.TongKet).Chu, m.TongKet is null ? null : QuyTacHocVu.QuyDoi(m.TongKet).He4, h.NgayThi.GetValueOrDefault(m.MaChuyenDe), m.LyDoThieu
            })).ToList();
            soDong += dong.Count;
            DichVuExcel.ThemTrang(wb, "Bảng điểm", "Bảng điểm toàn khoá", ["Năm học", "Học kỳ", "Mã chuyên đề", "Chuyên đề", "Số tiết", "Chuyên cần", "Kiểm tra", "Tổng kết", "Xếp loại", "Điểm chữ", "Hệ 4", "Ngày thi", "Ghi chú"], dong,
                $"{moTa} · Tổng kết = 30% chuyên cần + 70% kiểm tra");
        }
        if (phan.Contains("diem-danh"))
        {
            var ls = (await hocVu.LayLichSuDiemDanhCuaToiAsync()).Where(x => x.Buoi.Ngay.Date >= tu.Date && x.Buoi.Ngay.Date <= den.Date).OrderBy(x => x.Buoi.Ngay).ThenBy(x => x.Buoi.TietBatDau).ToList();
            soDong += ls.Count;
            DichVuExcel.ThemTrang(wb, "Điểm danh", "Lịch sử điểm danh", ["Ngày", "Thứ", "Tiết", "Chuyên đề", "Trạng thái", "Tự điểm danh"],
                ls.Select(x => new object?[] { x.Buoi.Ngay.Date, QuyTacHocVu.TenThu(x.Buoi.Ngay), $"{x.Buoi.TietBatDau}–{x.Buoi.TietKetThuc}", x.TenChuyenDe, x.TrangThai, x.TuDiemDanh ? "Có" : "" }),
                $"{moTaKy} · {string.Join(", ", TrangThaiDiemDanh.TatCa.Select(t => $"{t}: {ls.Count(x => x.TrangThai == t)}"))}");
        }
        if (phan.Contains("lich"))
        {
            var ds = await daoTao.LayLichAsync(tu, den);
            soDong += ds.Count;
            ThemThoiKhoaBieu(wb, "Thời khoá biểu", moTaKy, ds, false);
            ThemChiTietLich(wb, "Lịch học", moTaKy, ds, false);
        }
        if (phan.Contains("lich-thi"))
        {
            var lt = await chuongTrinh.LayLichThiCuaToiAsync();
            soDong += lt.Count;
            DichVuExcel.ThemTrang(wb, "Lịch thi", "Lịch thi của tôi", ["Ngày", "Thứ", "Giờ bắt đầu", "Tiết", "Chuyên đề", "Phòng", "Hình thức", "Tư cách dự thi", "Lý do"],
                lt.OrderBy(x => x.Lich.Ngay).Select(x => new object?[] { x.Lich.Ngay.Date, QuyTacHocVu.TenThu(x.Lich.Ngay), QuyTacHocVu.GioBatDauTiet(x.Lich.TietBatDau).ToString(@"hh\:mm"), $"{x.Lich.TietBatDau}–{x.Lich.TietKetThuc}", x.TenChuyenDe, x.TenPhong, x.Lich.HinhThuc, x.DuTuCach ? "Đủ" : "Không đủ", x.LyDo }), moTa);
        }
        if (wb.Worksheets.Count == 0) return null;
        await Ghi("Kết quả học tập của tôi", soDong);
        return Luu(wb);
    }

    // ---------- Các danh sách còn thiếu nút xuất ----------

    public async Task<byte[]> XuatKhoaAsync()
    {
        var cacTo = duLieu.LayDanhSachTo();
        var ds = duLieu.LayDanhSachKhoa();
        await Ghi("Khoa", ds.Count);
        return DichVuExcel.TaoBang("Danh sách khoa", ["Mã khoa", "Tên khoa", "Trưởng khoa", "Phó khoa", "Phó khoa thứ hai", "Số tổ", "Số cán bộ", "Mô tả"],
            ds.Select(k => new object?[] { k.MaKhoa, k.TenKhoa, TenCanBo(k.MaTruongKhoa), TenCanBo(k.MaPhoKhoa), TenCanBo(k.MaPhoKhoaThuHai), cacTo.Count(t => t.MaKhoa == k.MaKhoa), k.SiSo, k.MoTa }));
    }

    public async Task<byte[]> XuatToAsync(string? maKhoa = null)
    {
        var ds = duLieu.LayDanhSachTo().Where(t => maKhoa is null || t.MaKhoa == maKhoa).ToList();
        await Ghi("Tổ chuyên môn", ds.Count);
        return DichVuExcel.TaoBang("Danh sách tổ chuyên môn", ["Mã tổ", "Tên tổ", "Khoa", "Tổ trưởng", "Tổ phó", "Số cán bộ"],
            ds.Select(t => new object?[] { t.MaTo, t.TenTo, duLieu.LayTenKhoa(t.MaKhoa), TenCanBo(t.MaToTruong), TenCanBo(t.MaToPho), t.SiSo }));
    }

    private string TenCanBo(string? ma) => string.IsNullOrEmpty(ma) ? "" : duLieu.LayTenCanBo(ma);

    public async Task<byte[]> XuatPhongAsync()
    {
        var ds = await daoTao.LayPhongAsync();
        await Ghi("Phòng học", ds.Count);
        return DichVuExcel.TaoBang("Danh sách phòng học", ["Mã phòng", "Tên phòng", "Loại phòng", "Sức chứa", "Đang bảo trì"],
            ds.Select(p => new object?[] { p.MaPhong, p.TenPhong, p.LoaiPhong, p.SucChua, p.DangBaoTri ? "Có" : "Không" }));
    }

    public async Task<byte[]> XuatChuyenDeAsync()
    {
        var ds = await daoTao.LayChuyenDeAsync();
        await Ghi("Chuyên đề", ds.Count);
        return DichVuExcel.TaoBang("Danh mục chuyên đề", ["Mã chuyên đề", "Tên chuyên đề", "Khoa phụ trách", "Số tiết", "Giáo viên đủ điều kiện", "Số buổi đã xếp", "Mô tả"],
            ds.Select(d => new object?[] { d.ChuyenDe.MaChuyenDe, d.ChuyenDe.TenChuyenDe, d.TenKhoa, d.ChuyenDe.SoTiet, d.SoGiaoVien, d.SoBuoi, d.ChuyenDe.MoTa }));
    }

    public async Task<byte[]> XuatPhanCongAsync(IReadOnlyCollection<DichVuChuongTrinh.DongChuongTrinh> ds)
    {
        await Ghi("Chương trình và phân công", ds.Count);
        return DichVuExcel.TaoBang("Chương trình và phân công giảng dạy", ["Lớp", "Mã chuyên đề", "Chuyên đề", "Khoa phụ trách", "Giảng viên phân công", "Từ ngày", "Đến ngày", "Số buổi đã học", "Tổng số buổi", "Trạng thái"],
            ds.Select(d => new object?[] { d.MaLop, d.MaChuyenDe, d.TenChuyenDe, d.TenKhoa, d.TenGiangVien, d.TuNgay, d.DenNgay, d.SoBuoiDaHoc, d.TongBuoi, d.TrangThai }));
    }

    public async Task<byte[]> XuatXetThiAsync(IReadOnlyCollection<(string MaLop, string MaChuyenDe, string TenChuyenDe, string TenKhoa, string TrangThai, bool CoLichThi)> ds)
    {
        await Ghi("Xét tư cách thi", ds.Count);
        return DichVuExcel.TaoBang("Xét tư cách thi và lịch thi", ["Lớp", "Mã chuyên đề", "Chuyên đề", "Khoa", "Trạng thái", "Đã có lịch thi"],
            ds.Select(d => new object?[] { d.MaLop, d.MaChuyenDe, d.TenChuyenDe, d.TenKhoa, d.TrangThai, d.CoLichThi ? "Có" : "Chưa" }));
    }

    public async Task<byte[]?> XuatChuyenCanLopAsync(string maLop)
    {
        var (dong, loi) = await chuongTrinh.LayChuyenCanLopAsync(maLop);
        if (loi is not null) return null;
        await Ghi($"Chuyên cần lớp {maLop}", dong.Count);
        return DichVuExcel.TaoBang($"Chuyên cần lớp {maLop}", ["Mã học viên", "Họ và tên", "Buổi đã học", "Có mặt", "Đi muộn", "Vắng có phép", "Vắng không phép", "Tỉ lệ vắng"],
            dong.Select(d => new object?[] { d.MaHocVien, d.HoTen, d.SoBuoi, d.CoMat, d.DiMuon, d.VangCoPhep, d.VangKhongPhep, new DichVuExcel.PhanTram(d.TiLeVang) }), "Vắng quá 20% có nguy cơ không đủ tư cách thi");
    }

    public async Task<byte[]?> XuatSoDauBaiAsync(string maLop)
    {
        var (dong, loi) = await hocVu.LaySoDauBaiAsync(maLop);
        if (loi is not null) return null;
        await Ghi($"Sổ đầu bài lớp {maLop}", dong.Count);
        return DichVuExcel.TaoBang($"Sổ đầu bài lớp {maLop}", ["Ngày", "Thứ", "Tiết", "Chuyên đề", "Giảng viên", "Có mặt", "Sĩ số", "Nội dung bài giảng", "Nhận xét", "Xếp loại tiết"],
            dong.Select(d => new object?[] { d.Buoi.Ngay.Date, QuyTacHocVu.TenThu(d.Buoi.Ngay), $"{d.Buoi.TietBatDau}–{d.Buoi.TietKetThuc}", d.TenChuyenDe, d.TenGiangVien, d.CoMat, d.SiSo, d.Ghi?.NoiDung, d.Ghi?.NhanXet, d.Ghi?.XepLoaiTiet }));
    }
}
