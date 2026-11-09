using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Các loại dữ liệu nhập được từ Excel (ngoài học viên đã có ở DichVuExcel).</summary>
public enum LoaiNhap { CanBo, Lop, ChuyenDe, Phong, Diem, LichHoc }

/// <summary>
/// Nhập Excel dùng chung cho cán bộ, lớp, chuyên đề, phòng học và điểm kiểm tra.
/// Quy trình giống nhập học viên: tải tệp mẫu (có danh sách chọn) → đọc tệp, kiểm tra từng dòng, KHÔNG ghi gì →
/// người dùng xác nhận → ghi từng dòng hợp lệ qua đúng dịch vụ nghiệp vụ (kiểm quyền và quy tắc lại, ghi nhật ký).
/// Quyền theo mảng sở hữu: cán bộ – Quản lý khoa; lớp – Quản lý học viên; chuyên đề, phòng – Quản lý đào tạo;
/// điểm – giảng viên dạy chuyên đề cho lớp, Quản lý học viên, Admin.
/// </summary>
public sealed class DichVuNhapExcel(DichVuQuanLyDuLieuDemo duLieu, DichVuDaoTao daoTao, DichVuHocVu hocVu, DichVuNhatKy nhatKy,
    DichVuPhanQuyenDemo phanQuyen, KhoDuLieuDemo kho, IDbContextFactory<CoSoDuLieuNghiepVu> csdl)
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    public const int SoDongToiDa = 2000;

    public sealed record DongNhapChung(int SoDong, string Ma, string Ten, string MoTa, List<string> Loi, object DoiTuong)
    {
        public bool HopLe => Loi.Count == 0;
    }

    public static readonly string[] ChucVuCanBo = ["Trưởng khoa", "Phó khoa", "Tổ trưởng", "Tổ phó", "Cán bộ giảng viên"];
    public static readonly string[] CapBacHam = ["Đại tá", "Thượng tá", "Trung tá", "Thiếu tá", "Đại úy", "Không xác định"];
    public static readonly string[] HocVi = ["Tiến sĩ", "Thạc sĩ", "Cử nhân", "Khác"];

    public static string TenLoai(LoaiNhap l) => l switch
    {
        LoaiNhap.CanBo => "cán bộ, giảng viên", LoaiNhap.Lop => "lớp", LoaiNhap.ChuyenDe => "chuyên đề", LoaiNhap.Phong => "phòng học", LoaiNhap.LichHoc => "buổi học", _ => "điểm kiểm tra"
    };

    public static string[] Cot(LoaiNhap l) => l switch
    {
        LoaiNhap.CanBo => ["Mã cán bộ *", "Họ và tên *", "Giới tính *", "Ngày sinh * (dd/MM/yyyy)", "Mã khoa *", "Mã tổ *", "Chức vụ *", "Cấp bậc hàm *", "Học hàm", "Học vị *", "Chuyên môn *", "Quê quán", "Số điện thoại *", "Số CCCD *", "Email"],
        LoaiNhap.Lop => ["Mã lớp *", "Hệ đào tạo *", "Khóa *", "Chuyên ngành *", "Giáo viên chủ nhiệm"],
        LoaiNhap.ChuyenDe => ["Mã chuyên đề *", "Tên chuyên đề *", "Mã khoa phụ trách *", "Số tiết *", "Mô tả"],
        LoaiNhap.Phong => ["Mã phòng *", "Tên phòng *", "Loại phòng *", "Sức chứa *", "Đang bảo trì (Có/Không)"],
        LoaiNhap.LichHoc => ["Ngày * (dd/MM/yyyy)", "Tiết bắt đầu * (1–12)", "Số tiết * (1–5)", "Mã lớp *", "Mã chuyên đề *", "Mã cán bộ *", "Mã phòng *", "Ghi chú"],
        _ => ["Mã học viên *", "Họ và tên (tham khảo)", "Điểm kiểm tra (0–10; để trống: giữ nguyên)"]
    };

    private static string[] ViDu(LoaiNhap l) => l switch
    {
        LoaiNhap.CanBo => ["CBVD01", "Trần Văn Ví Dụ", "Nam", "15/03/1985", "K01", "K01T1", "Cán bộ giảng viên", "Thiếu tá", "", "Thạc sĩ", "Điều tra hình sự", "Hà Nội", "0912000999", "001085000999", "vidu.canbo@example.invalid"],
        LoaiNhap.Lop => ["B5K6", "Văn bằng 2", "K6", "Điều tra", "Nguyễn Văn Ví Dụ"],
        LoaiNhap.ChuyenDe => ["CDVD01", "Chuyên đề ví dụ", "K01", "45", "Mô tả ngắn"],
        LoaiNhap.Phong => ["PVD01", "Phòng ví dụ", "Giảng đường", "60", "Không"],
        LoaiNhap.LichHoc => ["16/11/2026", "1", "4", "B1D58", "CD-01", "CB006", "PM-01", "Dòng ví dụ, hệ thống tự bỏ qua"],
        _ => []
    };

    /// <summary>Trả lý do từ chối nếu người dùng không có quyền nhập loại dữ liệu này.</summary>
    public async Task<string?> KiemQuyenAsync(LoaiNhap loai, string? maLop = null, string? maChuyenDe = null)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        return loai switch
        {
            LoaiNhap.CanBo => nd.CoQuyenVung("Khoa") ? null : "Chỉ Admin và Quản lý khoa được nhập cán bộ.",
            LoaiNhap.Lop => nd.CoQuyenVung("Lop") ? null : "Chỉ Admin và Quản lý học viên được nhập lớp.",
            LoaiNhap.ChuyenDe or LoaiNhap.Phong => nd.CoQuyenVung("DaoTao") ? null : "Chỉ Admin và Quản lý đào tạo được nhập chuyên đề, phòng học.",
            LoaiNhap.LichHoc => nd.CoQuyenVung("DaoTao") ? null : "Chỉ Admin và Quản lý đào tạo được nhập lịch đào tạo.",
            _ => maLop is null || maChuyenDe is null ? "Hãy chọn lớp và chuyên đề trước khi nhập điểm."
                : (await hocVu.LayBangDiemAsync(maLop, maChuyenDe)).DuocSua ? null : "Chỉ giảng viên dạy chuyên đề này cho lớp, Quản lý học viên hoặc Admin được nhập điểm."
        };
    }

    // ---------- Tệp mẫu ----------

    public async Task<byte[]?> TaoMauAsync(LoaiNhap loai, string? maLop = null, string? maChuyenDe = null)
    {
        if (await KiemQuyenAsync(loai, maLop, maChuyenDe) is not null) return null;
        var cot = Cot(loai);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(loai == LoaiNhap.Diem ? "Điểm" : DichVuExcel.TenTrang(TenLoai(loai)));
        var dm = wb.Worksheets.Add("Danh mục");
        for (var i = 0; i < cot.Length; i++) ws.Cell(1, i + 1).Value = cot[i];
        ws.Range(1, 1, 1, cot.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4B34"));
        ws.Columns(1, cot.Length).Style.NumberFormat.Format = "@";

        if (loai == LoaiNhap.Diem)
        {
            // Điểm: điền sẵn danh sách lớp và điểm hiện có để giảng viên chỉ cần sửa cột điểm.
            var (dong, _, _) = await hocVu.LayBangDiemAsync(maLop!, maChuyenDe!);
            for (var r = 0; r < dong.Count; r++)
            {
                ws.Cell(r + 2, 1).SetValue(dong[r].MaHocVien);
                ws.Cell(r + 2, 2).SetValue(dong[r].HoTen);
                ws.Cell(r + 2, 3).SetValue(dong[r].DiemKiemTra?.ToString("0.0", Vi) ?? "");
            }
            ws.Range(2, 1, Math.Max(2, dong.Count + 1), 2).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F1F4F2"));
            dm.Cell(1, 1).Value = $"Lớp {maLop} – chuyên đề {maChuyenDe}. Chỉ sửa cột điểm; dùng dấu phẩy hoặc chấm cho phần thập phân.";
        }
        else
        {
            var vd = ViDu(loai);
            for (var i = 0; i < vd.Length; i++) { ws.Cell(2, i + 1).SetValue(vd[i]); ws.Cell(2, i + 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray); }
            var danhSach = await DanhSachChonAsync(loai);
            var c = 1;
            foreach (var (cotSo, giaTri) in danhSach)
            {
                for (var i = 0; i < giaTri.Count; i++) dm.Cell(i + 1, c).Value = giaTri[i];
                ws.Range(2, cotSo, SoDongToiDa + 1, cotSo).CreateDataValidation().List(dm.Range(1, c, Math.Max(1, giaTri.Count), c));
                c++;
            }
        }
        dm.Hide();
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, cot.Length).AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private async Task<List<(int Cot, List<string> GiaTri)>> DanhSachChonAsync(LoaiNhap loai)
    {
        List<string> khoa, to, lop, canBo;
        lock (kho.KhoaDongBo)
        {
            khoa = kho.DanhSachKhoa.Select(k => k.MaKhoa).ToList(); to = kho.DanhSachTo.Select(t => t.MaTo).ToList();
            lop = kho.DanhSachLop.Select(l => l.MaLop).ToList(); canBo = kho.DanhSachCanBo.Select(c => c.MaCanBo).ToList();
        }
        if (loai == LoaiNhap.LichHoc)
        {
            await using var db = csdl.CreateDbContext();
            var cd = await db.ChuyenDe.Select(c => c.MaChuyenDe).OrderBy(x => x).ToListAsync();
            var phong = await db.Phong.Where(p => !p.DangBaoTri).Select(p => p.MaPhong).OrderBy(x => x).ToListAsync();
            return [(2, Enumerable.Range(1, 12).Select(i => i.ToString()).ToList()), (3, Enumerable.Range(1, 5).Select(i => i.ToString()).ToList()), (4, lop), (5, cd), (6, canBo), (7, phong)];
        }
        return loai switch
        {
            LoaiNhap.CanBo => [(3, ["Nam", "Nữ"]), (5, khoa), (6, to), (7, [.. ChucVuCanBo]), (8, [.. CapBacHam]), (10, [.. HocVi])],
            LoaiNhap.Lop => [(2, [.. QuyTacDuLieu.CacHeDaoTao]), (4, [.. QuyTacDuLieu.ChuyenNganhChinhQuyDanhMuc])],
            LoaiNhap.ChuyenDe => [(3, khoa)],
            LoaiNhap.Phong => [(3, [.. DichVuDaoTao.LoaiPhong]), (5, ["Có", "Không"])],
            _ => []
        };
    }

    // ---------- Đọc và kiểm tra ----------

    public async Task<(List<DongNhapChung> Dong, string? Loi)> DocTepAsync(LoaiNhap loai, Stream tep, string? maLop = null, string? maChuyenDe = null)
    {
        if (await KiemQuyenAsync(loai, maLop, maChuyenDe) is { } tuChoi) return ([], tuChoi);
        XLWorkbook wb;
        try { wb = new XLWorkbook(tep); }
        catch (Exception) { return ([], "Không đọc được tệp. Hãy dùng tệp .xlsx tải từ nút \"Tải tệp mẫu\"."); }
        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null) return ([], "Tệp không có trang tính.");
            var cot = Cot(loai);
            var sai = cot.Where((c, i) => !string.Equals(ws.Cell(1, i + 1).GetString().Trim(), c, StringComparison.OrdinalIgnoreCase)).ToList();
            if (sai.Count > 0) return ([], $"Tiêu đề cột không khớp tệp mẫu (sai ở: {string.Join(", ", sai.Take(3))}). Hãy tải tệp mẫu và giữ nguyên dòng tiêu đề.");
            var cuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
            var vd = ViDu(loai);
            var hang = new List<(int So, string[] O, IXLCell[] Cell)>();
            for (var r = 2; r <= Math.Min(cuoi, SoDongToiDa + 1); r++)
            {
                var o = Enumerable.Range(1, cot.Length).Select(c => ws.Cell(r, c).GetFormattedString().Trim()).ToArray();
                if (o.All(x => x == "")) continue;
                if (r == 2 && vd.Length > 0 && o.SequenceEqual(vd)) continue; // dòng ví dụ của tệp mẫu
                hang.Add((r, o, Enumerable.Range(1, cot.Length).Select(c => ws.Cell(r, c)).ToArray()));
            }
            var ketQua = loai switch
            {
                LoaiNhap.CanBo => KiemTraCanBo(hang),
                LoaiNhap.Lop => KiemTraLop(hang),
                LoaiNhap.ChuyenDe => await KiemTraChuyenDeAsync(hang),
                LoaiNhap.Phong => await KiemTraPhongAsync(hang),
                LoaiNhap.LichHoc => await KiemTraLichAsync(hang),
                _ => await KiemTraDiemAsync(hang, maLop!, maChuyenDe!)
            };
            if (cuoi > SoDongToiDa + 1) return (ketQua, $"Mỗi lần nhập tối đa {SoDongToiDa:N0} dòng; phần dư bị bỏ qua.");
            return (ketQua, ketQua.Count == 0 ? "Tệp chưa có dòng dữ liệu nào." : null);
        }
    }

    private static List<string> KiemTraChuThich(object x)
    {
        var kq = new List<ValidationResult>();
        Validator.TryValidateObject(x, new ValidationContext(x), kq, true);
        return kq.Select(v => v.ErrorMessage ?? "Dữ liệu không hợp lệ.").ToList();
    }

    private static bool DocNgay(IXLCell o, string chu, out DateTime ngay)
    {
        if (o.DataType == XLDataType.DateTime) { ngay = o.GetDateTime(); return true; }
        return DateTime.TryParseExact(chu, ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], Vi, DateTimeStyles.None, out ngay);
    }

    private static bool DocSo(string chu, out double so)
        => double.TryParse(chu.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out so);

    private List<DongNhapChung> KiemTraCanBo(List<(int So, string[] O, IXLCell[] Cell)> hang)
    {
        HashSet<string> ma, cccd, sdt, khoa; Dictionary<string, string> toKhoa;
        lock (kho.KhoaDongBo)
        {
            ma = kho.DanhSachCanBo.Select(c => c.MaCanBo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            cccd = kho.DanhSachCanBo.Select(c => c.SoCCCD).Concat(kho.DanhSachHocVien.Select(h => h.SoCCCD)).ToHashSet();
            sdt = kho.DanhSachCanBo.Select(c => c.SoDienThoai).Concat(kho.DanhSachHocVien.Select(h => h.SoDienThoai)).ToHashSet();
            khoa = kho.DanhSachKhoa.Select(k => k.MaKhoa).ToHashSet(StringComparer.OrdinalIgnoreCase);
            toKhoa = kho.DanhSachTo.ToDictionary(t => t.MaTo, t => t.MaKhoa, StringComparer.OrdinalIgnoreCase);
        }
        var trongTep = (Ma: new HashSet<string>(StringComparer.OrdinalIgnoreCase), Cccd: new HashSet<string>(), Sdt: new HashSet<string>());
        return hang.Select(h =>
        {
            var o = h.O; var loi = new List<string>();
            if (!DocNgay(h.Cell[3], o[3], out var ngaySinh)) loi.Add("Ngày sinh phải theo dạng dd/MM/yyyy.");
            var cb = new CanBoGiangVien
            {
                MaCanBo = o[0].ToUpperInvariant(), HoTen = o[1], GioiTinh = o[2], NgaySinh = ngaySinh, MaKhoa = o[4].ToUpperInvariant(), MaTo = o[5] == "" ? null : o[5].ToUpperInvariant(),
                ChucVu = o[6], CapBacHam = o[7], HocHam = o[8], HocVi = o[9], ChuyenMon = o[10], QueQuan = o[11], SoDienThoai = o[12], SoCCCD = o[13], Email = o[14] == "" ? null : o[14]
            };
            loi.AddRange(KiemTraChuThich(cb));
            if (cb.GioiTinh is not ("Nam" or "Nữ")) loi.Add("Giới tính phải là Nam hoặc Nữ.");
            if (cb.MaKhoa != "" && !khoa.Contains(cb.MaKhoa)) loi.Add($"Khoa {cb.MaKhoa} không tồn tại.");
            if (cb.MaTo is null || !toKhoa.TryGetValue(cb.MaTo, out var k) || !k.Equals(cb.MaKhoa, StringComparison.OrdinalIgnoreCase)) loi.Add("Tổ không tồn tại hoặc không thuộc khoa đã nhập.");
            if (!ChucVuCanBo.Contains(cb.ChucVu)) loi.Add($"Chức vụ \"{cb.ChucVu}\" không hợp lệ.");
            if (!CapBacHam.Contains(cb.CapBacHam)) loi.Add($"Cấp bậc hàm \"{cb.CapBacHam}\" không hợp lệ.");
            if (!HocVi.Contains(cb.HocVi)) loi.Add($"Học vị \"{cb.HocVi}\" không hợp lệ.");
            if (ma.Contains(cb.MaCanBo)) loi.Add($"Mã {cb.MaCanBo} đã có trong hệ thống.");
            if (cb.MaCanBo != "" && !trongTep.Ma.Add(cb.MaCanBo)) loi.Add($"Mã {cb.MaCanBo} lặp lại trong tệp.");
            if (cb.SoCCCD != "" && (cccd.Contains(cb.SoCCCD) || !trongTep.Cccd.Add(cb.SoCCCD))) loi.Add("Số CCCD đã được dùng (trong hệ thống hoặc dòng khác của tệp).");
            if (cb.SoDienThoai != "" && (sdt.Contains(cb.SoDienThoai) || !trongTep.Sdt.Add(cb.SoDienThoai))) loi.Add("Số điện thoại đã được dùng (trong hệ thống hoặc dòng khác của tệp).");
            return new DongNhapChung(h.So, cb.MaCanBo, cb.HoTen, $"{cb.ChucVu} · {cb.MaTo}", loi.Distinct().ToList(), cb);
        }).ToList();
    }

    private List<DongNhapChung> KiemTraLop(List<(int So, string[] O, IXLCell[] Cell)> hang)
    {
        HashSet<string> co;
        lock (kho.KhoaDongBo) co = kho.DanhSachLop.Select(l => l.MaLop).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var trongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return hang.Select(h =>
        {
            var o = h.O;
            var lop = new LopDaoTao { MaLop = o[0].ToUpperInvariant(), HeDaoTao = o[1], KhoaHoc = o[2].ToUpperInvariant(), ChuyenNganh = o[3], GiaoVienChuNhiem = o[4] };
            var loi = KiemTraChuThich(lop);
            if (loi.Count == 0 && !QuyTacDuLieu.LopHopLe(lop.MaLop, lop.HeDaoTao, lop.KhoaHoc, lop.ChuyenNganh)) loi.Add("Mã lớp, hệ, khóa hoặc chuyên ngành không khớp quy tắc đào tạo (ví dụ B1D58: Chính quy, khóa D58, lớp 1–3 Trinh sát an ninh; liên thông lớp 16–20; văn bằng 2 B{số}K{khóa}).");
            if (co.Contains(lop.MaLop)) loi.Add($"Lớp {lop.MaLop} đã có.");
            if (lop.MaLop != "" && !trongTep.Add(lop.MaLop)) loi.Add($"Mã {lop.MaLop} lặp lại trong tệp.");
            return new DongNhapChung(h.So, lop.MaLop, lop.ChuyenNganh, $"{lop.HeDaoTao} · {lop.KhoaHoc}", loi, lop);
        }).ToList();
    }

    private async Task<List<DongNhapChung>> KiemTraChuyenDeAsync(List<(int So, string[] O, IXLCell[] Cell)> hang)
    {
        await using var db = csdl.CreateDbContext();
        var co = (await db.ChuyenDe.Select(c => c.MaChuyenDe).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> khoa;
        lock (kho.KhoaDongBo) khoa = kho.DanhSachKhoa.Select(k => k.MaKhoa).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var trongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return hang.Select(h =>
        {
            var o = h.O; var loi = new List<string>();
            if (!int.TryParse(o[3], out var soTiet)) loi.Add("Số tiết phải là số nguyên.");
            var cd = new ChuyenDe { MaChuyenDe = o[0].ToUpperInvariant(), TenChuyenDe = o[1], MaKhoa = o[2].ToUpperInvariant(), SoTiet = soTiet, MoTa = o[4] };
            loi.AddRange(KiemTraChuThich(cd));
            if (cd.MaKhoa != "" && !khoa.Contains(cd.MaKhoa)) loi.Add($"Khoa {cd.MaKhoa} không tồn tại.");
            if (co.Contains(cd.MaChuyenDe)) loi.Add($"Mã chuyên đề {cd.MaChuyenDe} đã có.");
            if (cd.MaChuyenDe != "" && !trongTep.Add(cd.MaChuyenDe)) loi.Add($"Mã {cd.MaChuyenDe} lặp lại trong tệp.");
            return new DongNhapChung(h.So, cd.MaChuyenDe, cd.TenChuyenDe, $"{cd.MaKhoa} · {cd.SoTiet} tiết", loi.Distinct().ToList(), cd);
        }).ToList();
    }

    private async Task<List<DongNhapChung>> KiemTraPhongAsync(List<(int So, string[] O, IXLCell[] Cell)> hang)
    {
        await using var db = csdl.CreateDbContext();
        var co = (await db.Phong.Select(p => p.MaPhong).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var trongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return hang.Select(h =>
        {
            var o = h.O; var loi = new List<string>();
            if (!int.TryParse(o[3], out var sucChua)) loi.Add("Sức chứa phải là số nguyên.");
            if (o[4] is not ("" or "Có" or "Không")) loi.Add("Cột bảo trì ghi Có hoặc Không.");
            var p = new Phong { MaPhong = o[0].ToUpperInvariant(), TenPhong = o[1], LoaiPhong = o[2], SucChua = sucChua, DangBaoTri = o[4] == "Có" };
            loi.AddRange(KiemTraChuThich(p));
            if (!DichVuDaoTao.LoaiPhong.Contains(p.LoaiPhong)) loi.Add($"Loại phòng \"{p.LoaiPhong}\" không hợp lệ.");
            if (co.Contains(p.MaPhong)) loi.Add($"Mã phòng {p.MaPhong} đã có.");
            if (p.MaPhong != "" && !trongTep.Add(p.MaPhong)) loi.Add($"Mã {p.MaPhong} lặp lại trong tệp.");
            return new DongNhapChung(h.So, p.MaPhong, p.TenPhong, $"{p.LoaiPhong} · {p.SucChua} chỗ", loi.Distinct().ToList(), p);
        }).ToList();
    }

    /// <summary>Kiểm tra từng buổi bằng đúng quy tắc của biểu mẫu xếp lịch, thêm kiểm tra trùng giữa các dòng trong tệp.</summary>
    private async Task<List<DongNhapChung>> KiemTraLichAsync(List<(int So, string[] O, IXLCell[] Cell)> hang)
    {
        var tenCd = await TenChuyenDeAsync();
        var kq = new List<DongNhapChung>();
        var trongTep = new List<(int SoDong, BuoiHoc Buoi)>();
        foreach (var h in hang)
        {
            var o = h.O; var loi = new List<string>();
            if (!DocNgay(h.Cell[0], o[0], out var ngay)) loi.Add("Ngày phải theo dạng dd/MM/yyyy.");
            if (!int.TryParse(o[1], out var tiet)) loi.Add("Tiết bắt đầu phải là số nguyên.");
            if (!int.TryParse(o[2], out var soTiet)) loi.Add("Số tiết phải là số nguyên.");
            var b = new BuoiHoc { Ngay = ngay.Date, TietBatDau = tiet, SoTiet = soTiet, MaLop = o[3].ToUpperInvariant(), MaChuyenDe = o[4].ToUpperInvariant(), MaCanBo = o[5].ToUpperInvariant(), MaPhong = o[6].ToUpperInvariant(), GhiChu = o[7] };
            if (loi.Count == 0)
            {
                loi.AddRange(await daoTao.KiemTraBuoiHocAsync(b));
                foreach (var (soDong, x) in trongTep.Where(t => QuyTacHocVu.TrungGio(t.Buoi, b)))
                {
                    if (x.MaLop == b.MaLop) loi.Add($"Trùng giờ lớp {b.MaLop} với dòng {soDong} trong tệp.");
                    if (x.MaCanBo == b.MaCanBo) loi.Add($"Trùng giờ giảng viên {b.MaCanBo} với dòng {soDong} trong tệp.");
                    if (x.MaPhong == b.MaPhong) loi.Add($"Trùng phòng {b.MaPhong} với dòng {soDong} trong tệp.");
                }
            }
            if (loi.Count == 0) trongTep.Add((h.So, b));
            var moTaTiet = loi.Count == 0 ? $"{QuyTacHocVu.TenThu(b.Ngay)} {b.Ngay:dd/MM/yyyy}, tiết {b.TietBatDau}–{b.TietKetThuc}" : $"{o[0]}, tiết {o[1]}";
            kq.Add(new DongNhapChung(h.So, b.MaLop, tenCd.GetValueOrDefault(b.MaChuyenDe, b.MaChuyenDe), $"{moTaTiet} · {b.MaCanBo} · {b.MaPhong}", loi.Distinct().ToList(), b));
        }
        return kq;
    }

    private async Task<Dictionary<string, string>> TenChuyenDeAsync()
    {
        await using var db = csdl.CreateDbContext();
        return await db.ChuyenDe.ToDictionaryAsync(c => c.MaChuyenDe, c => c.TenChuyenDe);
    }

    private async Task<List<DongNhapChung>> KiemTraDiemAsync(List<(int So, string[] O, IXLCell[] Cell)> hang, string maLop, string maChuyenDe)
    {
        var (bang, _, _) = await hocVu.LayBangDiemAsync(maLop, maChuyenDe);
        var theoMa = bang.ToDictionary(d => d.MaHocVien, StringComparer.OrdinalIgnoreCase);
        var trongTep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kq = new List<DongNhapChung>();
        foreach (var h in hang)
        {
            var o = h.O; var loi = new List<string>();
            var ma = o[0].ToUpperInvariant();
            if (o[2] == "") continue; // để trống: giữ nguyên điểm hiện có
            double? diem = null;
            if (!DocSo(o[2], out var so)) loi.Add("Điểm phải là số (ví dụ 8,5).");
            else { diem = so; if (QuyTacHocVu.KiemTraDiem(diem) is { } l) loi.Add(l); }
            if (!theoMa.TryGetValue(ma, out var dong)) loi.Add($"Học viên {ma} không thuộc lớp {maLop}.");
            if (!trongTep.Add(ma)) loi.Add($"Học viên {ma} lặp lại trong tệp.");
            var cu = dong?.DiemKiemTra;
            kq.Add(new DongNhapChung(h.So, ma, dong?.HoTen ?? o[1], cu is null ? $"mới: {diem:0.0}" : $"{cu:0.0} → {diem:0.0}", loi, (ma, diem)));
        }
        return kq;
    }

    // ---------- Ghi ----------

    public async Task<(int ThanhCong, List<string> Loi)> XacNhanAsync(LoaiNhap loai, IEnumerable<DongNhapChung> dong, string? maLop = null, string? maChuyenDe = null)
    {
        var hopLe = dong.Where(d => d.HopLe).ToList();
        var thanhCong = 0; var loi = new List<string>();
        if (loai == LoaiNhap.Diem)
        {
            var bang = hopLe.Select(d => ((string Ma, double? Diem))d.DoiTuong).ToDictionary(x => x.Ma, x => x.Diem);
            var kq = await hocVu.LuuDiemKiemTraAsync(maLop!, maChuyenDe!, bang);
            if (kq.ThanhCong) thanhCong = bang.Count; else loi.Add(kq.ThongBao);
        }
        else
        {
            foreach (var d in hopLe)
            {
                var kq = d.DoiTuong switch
                {
                    CanBoGiangVien cb => await duLieu.LuuCanBoAsync(cb),
                    LopDaoTao l => await duLieu.LuuLopAsync(l),
                    ChuyenDe cd => await daoTao.LuuChuyenDeAsync(cd),
                    Phong p => await daoTao.LuuPhongAsync(p),
                    BuoiHoc b => await daoTao.LuuBuoiHocAsync(b),
                    _ => new KetQuaThaoTac(false, "Loại dữ liệu không hỗ trợ.")
                };
                if (kq.ThanhCong) thanhCong++; else loi.Add($"Dòng {d.SoDong}: {kq.ThongBao}");
            }
        }
        var doiTuong = loai switch { LoaiNhap.CanBo => "Cán bộ", LoaiNhap.Lop => "Lớp", LoaiNhap.ChuyenDe => "Chuyên đề", LoaiNhap.Phong => "Phòng học", LoaiNhap.LichHoc => "Lịch đào tạo", _ => "Điểm" };
        await nhatKy.GhiAsync("Nhập Excel", doiTuong, $"{thanhCong} dòng thành công, {loi.Count} lỗi{(loai == LoaiNhap.Diem ? $" (lớp {maLop}, {maChuyenDe})" : "")}");
        return (thanhCong, loi);
    }
}
