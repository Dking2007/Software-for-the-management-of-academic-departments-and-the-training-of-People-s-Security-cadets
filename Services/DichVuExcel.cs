using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ClosedXML.Excel;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Xuất/nhập Excel (ý tưởng từ repo LTMT, thư viện ClosedXML).
/// Xuất: dùng đúng mặt cắt dữ liệu của màn hình theo vai trò, nên trường bị che trên màn hình cũng trống trong tệp.
/// Nhập học viên: đọc tệp → xem trước từng dòng kèm lý do lỗi → xác nhận mới ghi (qua dịch vụ, kiểm quyền lại).
/// </summary>
public sealed class DichVuExcel(DichVuQuanLyDuLieuDemo duLieu, DichVuHocVu hocVu, DichVuNhatKy nhatKy, DichVuPhanQuyenDemo phanQuyen, KhoDuLieuDemo kho)
{
    public const string MimeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    // ---------- Xuất ----------

    public async Task<byte[]> XuatCanBoAsync(string? maKhoa = null, string? maTo = null)
    {
        var ds = duLieu.LayDanhSachCanBo().Where(c => (maKhoa is null || c.MaKhoa == maKhoa) && (maTo is null || c.MaTo == maTo)).ToList();
        await GhiNhatKyXuat("Cán bộ", ds.Count);
        return TaoBang("Cán bộ, giảng viên", ["Mã cán bộ", "Họ và tên", "Giới tính", "Ngày sinh", "Cấp bậc hàm", "Chức vụ", "Học hàm", "Học vị", "Chuyên môn", "Khoa", "Tổ", "Số điện thoại", "Số CCCD", "Email"],
            ds.Select(c => new object?[] { c.MaCanBo, c.HoTen, c.GioiTinh, c.NgaySinh, c.CapBacHam, c.ChucVu, c.HocHam, c.HocVi, c.ChuyenMon, c.TenKhoa, c.TenTo, c.SoDienThoai, c.SoCCCD, c.Email }));
    }

    public async Task<byte[]> XuatHocVienAsync(string? maLop = null)
    {
        var ds = duLieu.LayDanhSachHocVien().Where(h => maLop is null || h.MaLop == maLop).OrderBy(h => h.MaLop).ThenBy(h => h.KhoaSapXepTen, StringComparer.Ordinal).ToList();
        await GhiNhatKyXuat("Học viên", ds.Count);
        return TaoBang(maLop is null ? "Danh sách học viên" : $"Danh sách học viên lớp {maLop}", ["Mã học viên", "Họ và tên", "Lớp", "Chức vụ", "Ngày sinh", "Giới tính", "Quê quán", "Số điện thoại", "Số CCCD", "Email", "Địa chỉ thường trú"],
            ds.Select(h => new object?[] { h.MaHocVien, h.HoTen, h.MaLop, h.ChucVu, h.NgaySinh == default ? null : h.NgaySinh, h.GioiTinh, h.QueQuan, h.SoDienThoai, h.SoCCCD, h.Email, h.DiaChiThuongTru }));
    }

    public async Task<byte[]> XuatLopAsync()
    {
        var ds = duLieu.LayDanhSachLop();
        await GhiNhatKyXuat("Lớp", ds.Count);
        return TaoBang("Danh sách lớp đào tạo", ["Mã lớp", "Hệ đào tạo", "Khoá", "Niên khoá", "Chuyên ngành", "Sĩ số", "GV chủ nhiệm"],
            ds.Select(l => new object?[] { l.MaLop, l.HeDaoTao, l.KhoaHoc, l.NienKhoa, l.ChuyenNganh, l.SiSo, l.GiaoVienChuNhiem }));
    }

    public async Task<byte[]?> XuatBangDiemAsync(string maLop, string maChuyenDe, string tenChuyenDe)
    {
        var (dong, _, loi) = await hocVu.LayBangDiemAsync(maLop, maChuyenDe);
        if (loi is not null) return null;
        await GhiNhatKyXuat($"Bảng điểm {maLop} · {maChuyenDe}", dong.Count);
        return TaoBang($"Bảng điểm lớp {maLop} · {tenChuyenDe}", ["Mã học viên", "Họ và tên", "Chuyên cần", "Số buổi", "Kiểm tra", "Tổng kết", "Xếp loại", "Ghi chú"],
            dong.Select(d => new object?[] { d.MaHocVien, d.HoTen, d.ChuyenCan, d.SoBuoiDaDiemDanh, d.DiemKiemTra, d.TongKet, d.XepLoai, d.LyDoThieu }),
            "Tổng kết = 30% chuyên cần + 70% kiểm tra, làm tròn 1 chữ số; từ 5,0 là đạt.");
    }

    public async Task<byte[]?> XuatDiemDanhAsync(int buoiHocId)
    {
        var (buoi, dong, _, loi) = await hocVu.LayDiemDanhAsync(buoiHocId);
        if (buoi is null || loi is not null) return null;
        await GhiNhatKyXuat($"Điểm danh buổi {buoiHocId}", dong.Count);
        return TaoBang($"Điểm danh lớp {buoi.MaLop} ngày {buoi.Ngay:dd/MM/yyyy}, tiết {buoi.TietBatDau}–{buoi.TietKetThuc}", ["Mã học viên", "Họ và tên", "Chức vụ", "Trạng thái", "Tự điểm danh", "Thời điểm"],
            dong.Select(d => new object?[] { d.MaHocVien, d.HoTen, d.ChucVu, d.TrangThai ?? "Chưa điểm danh", d.TuDiemDanh ? "Có" : "", d.ThoiDiem?.ToLocalTime().DateTime }));
    }

    public async Task<byte[]> XuatNhatKyAsync(List<NhatKy> ds)
    {
        await GhiNhatKyXuat("Nhật ký", ds.Count);
        return TaoBang("Nhật ký thao tác", ["Thời điểm", "Người thực hiện", "Vai trò", "Hành động", "Đối tượng", "Mã", "Giá trị cũ", "Giá trị mới"],
            ds.Select(x => new object?[] { x.ThoiDiem.ToLocalTime().DateTime, x.NguoiThucHien, x.VaiTro, x.HanhDong, x.DoiTuong, x.MaDoiTuong, x.GiaTriCu, x.GiaTriMoi }));
    }

    private async Task GhiNhatKyXuat(string doiTuong, int soDong) => await nhatKy.GhiAsync("Xuất Excel", doiTuong, $"{soDong} dòng");

    /// <summary>Tạo tệp một trang tính: tiêu đề, dòng chú thích, bảng có tiêu đề cột cố định, lọc, tự giãn cột.</summary>
    public static byte[] TaoBang(string tieuDe, string[] cot, IEnumerable<object?[]> dong, string? ghiChu = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(TenTrang(tieuDe));
        ws.Cell(1, 1).Value = tieuDe.ToUpperInvariant();
        ws.Range(1, 1, 1, cot.Length).Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1B4B34"));
        ws.Cell(2, 1).Value = $"Học viện An ninh nhân dân · Xuất lúc {DateTime.Now:HH:mm dd/MM/yyyy} · Dữ liệu giả lập phục vụ học tập" + (ghiChu is null ? "" : " · " + ghiChu);
        ws.Range(2, 1, 2, cot.Length).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#5A665F"));
        for (var i = 0; i < cot.Length; i++) ws.Cell(4, i + 1).Value = cot[i];
        var tieuDeCot = ws.Range(4, 1, 4, cot.Length);
        tieuDeCot.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4B34")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        var r = 5;
        foreach (var d in dong)
        {
            for (var i = 0; i < d.Length; i++)
            {
                var o = ws.Cell(r, i + 1);
                switch (d[i])
                {
                    case null: break;
                    case DateTime t: o.Value = t; o.Style.DateFormat.Format = t.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm"; break;
                    case double so: o.Value = so; o.Style.NumberFormat.Format = "0.0"; break;
                    case int so: o.Value = so; break;
                    // Mã, CCCD, SĐT giữ dạng chữ để không mất số 0 đầu.
                    default: o.SetValue(d[i]!.ToString()); o.Style.NumberFormat.Format = "@"; break;
                }
            }
            r++;
        }
        var bang = ws.Range(4, 1, Math.Max(4, r - 1), cot.Length);
        bang.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorderColor(XLColor.FromHtml("#DCE0DA")).Border.SetOutsideBorderColor(XLColor.FromHtml("#C5CBC4"));
        if (r > 5) bang.SetAutoFilter();
        ws.SheetView.FreezeRows(4);
        ws.Columns(1, cot.Length).AdjustToContents(4, Math.Min(r, 300));
        foreach (var c in ws.Columns(1, cot.Length)) if (c.Width > 60) c.Width = 60;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static string TenTrang(string ten)
    {
        var sach = new string(ten.Where(c => !"[]:*?/\\".Contains(c)).ToArray());
        return sach.Length > 31 ? sach[..31] : sach;
    }

    // ---------- Nhập học viên ----------

    public static readonly string[] CotNhapHocVien = ["Mã học viên (để trống: tự sinh)", "Họ và tên *", "Lớp *", "Chức vụ", "Ngày sinh * (dd/MM/yyyy)", "Giới tính *", "Quê quán", "Số điện thoại *", "Số CCCD *", "Email", "Địa chỉ thường trú *"];

    /// <summary>Tệp mẫu: tiêu đề cột, một dòng ví dụ, danh sách chọn cho lớp, giới tính, chức vụ.</summary>
    public byte[] TaoMauNhapHocVien()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Học viên");
        var dm = wb.Worksheets.Add("Danh mục");
        for (var i = 0; i < CotNhapHocVien.Length; i++) ws.Cell(1, i + 1).Value = CotNhapHocVien[i];
        ws.Range(1, 1, 1, CotNhapHocVien.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#1B4B34"));
        object[] viDu = ["", "Nguyễn Văn Ví Dụ", "B1D58", "Học viên", "12/04/2005", "Nam", "Hà Nội", "0912000001", "001205000001", "vidu@example.invalid", "Số 1, đường Số 1, Hà Nội"];
        for (var i = 0; i < viDu.Length; i++) { ws.Cell(2, i + 1).SetValue(viDu[i].ToString()); ws.Cell(2, i + 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray); }
        ws.Columns(1, CotNhapHocVien.Length).Style.NumberFormat.Format = "@";
        var lop = duLieu.LayDanhSachLop().Select(l => l.MaLop).ToList();
        string[] gioiTinh = ["Nam", "Nữ"];
        var chucVu = QuyTacDuLieu.ChiTieuChucVu.Keys.Prepend("Học viên").ToList();
        for (var i = 0; i < lop.Count; i++) dm.Cell(i + 1, 1).Value = lop[i];
        for (var i = 0; i < gioiTinh.Length; i++) dm.Cell(i + 1, 2).Value = gioiTinh[i];
        for (var i = 0; i < chucVu.Count; i++) dm.Cell(i + 1, 3).Value = chucVu[i];
        ws.Range("C2:C2000").CreateDataValidation().List(dm.Range(1, 1, Math.Max(1, lop.Count), 1));
        ws.Range("F2:F2000").CreateDataValidation().List(dm.Range(1, 2, 2, 2));
        ws.Range("D2:D2000").CreateDataValidation().List(dm.Range(1, 3, chucVu.Count, 3));
        dm.Hide();
        ws.SheetView.FreezeRows(1);
        ws.Columns(1, CotNhapHocVien.Length).AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public sealed record DongNhap(int SoDong, HocVien HocVien, List<string> Loi, bool TuSinhMa)
    {
        public bool HopLe => Loi.Count == 0;
    }

    /// <summary>Đọc tệp và kiểm tra từng dòng (không ghi gì). Dòng ví dụ in nghiêng của tệp mẫu được bỏ qua.</summary>
    public async Task<(List<DongNhap> Dong, string? Loi)> DocTepHocVienAsync(Stream tep)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Lop")) return ([], "Chỉ Admin và Quản lý học viên được nhập học viên.");
        XLWorkbook wb;
        try { wb = new XLWorkbook(tep); }
        catch (Exception) { return ([], "Không đọc được tệp. Hãy dùng tệp .xlsx tải từ nút \"Tải tệp mẫu\"."); }
        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault();
            if (ws is null) return ([], "Tệp không có trang tính.");
            var tieuDe = Enumerable.Range(1, CotNhapHocVien.Length).Select(i => ws.Cell(1, i).GetString().Trim()).ToArray();
            var thieu = CotNhapHocVien.Where((c, i) => !string.Equals(tieuDe[i], c, StringComparison.OrdinalIgnoreCase)).ToList();
            if (thieu.Count > 0) return ([], $"Tiêu đề cột không khớp tệp mẫu (sai ở: {string.Join(", ", thieu.Take(3))}). Hãy tải tệp mẫu và giữ nguyên dòng tiêu đề.");

            HashSet<string> maCo, cccdCo, sdtCo, lopCo;
            int maLonNhat;
            lock (kho.KhoaDongBo)
            {
                maCo = kho.DanhSachHocVien.Select(h => h.MaHocVien).ToHashSet(StringComparer.OrdinalIgnoreCase);
                cccdCo = kho.DanhSachHocVien.Select(h => h.SoCCCD).Concat(kho.DanhSachCanBo.Select(c => c.SoCCCD)).ToHashSet();
                sdtCo = kho.DanhSachHocVien.Select(h => h.SoDienThoai).Concat(kho.DanhSachCanBo.Select(c => c.SoDienThoai)).ToHashSet();
                lopCo = kho.DanhSachLop.Select(l => l.MaLop).ToHashSet(StringComparer.OrdinalIgnoreCase);
                maLonNhat = kho.DanhSachHocVien.Select(h => h.MaHocVien.StartsWith("HV") && int.TryParse(h.MaHocVien[2..], out var so) ? so : 0).DefaultIfEmpty(0).Max();
            }
            var ketQua = new List<DongNhap>();
            var trongTep = new { Ma = new HashSet<string>(StringComparer.OrdinalIgnoreCase), Cccd = new HashSet<string>(), Sdt = new HashSet<string>() };
            var cuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
            for (var r = 2; r <= Math.Min(cuoi, 2001); r++)
            {
                string O(int c) => ws.Cell(r, c).GetFormattedString().Trim();
                if (Enumerable.Range(1, CotNhapHocVien.Length).All(c => O(c) == "")) continue;
                if (r == 2 && O(2) == "Nguyễn Văn Ví Dụ") continue;
                var loi = new List<string>();
                var ma = O(1).ToUpperInvariant();
                var tuSinh = ma == "";
                if (tuSinh) ma = $"HV{++maLonNhat:00000}";
                DateTime ngaySinh = default;
                var oNgay = ws.Cell(r, 5);
                if (oNgay.DataType == XLDataType.DateTime) ngaySinh = oNgay.GetDateTime();
                else if (!DateTime.TryParseExact(O(5), ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"], Vi, DateTimeStyles.None, out ngaySinh)) loi.Add("Ngày sinh phải theo dạng dd/MM/yyyy.");
                var hv = new HocVien
                {
                    MaHocVien = ma, HoTen = O(2), MaLop = O(3).ToUpperInvariant(), ChucVu = O(4) == "" ? "Học viên" : O(4), NgaySinh = ngaySinh,
                    GioiTinh = O(6), QueQuan = O(7), SoDienThoai = O(8), SoCCCD = O(9), Email = O(10) == "" ? null : O(10), DiaChiThuongTru = O(11)
                };
                var kq = new List<ValidationResult>();
                if (!Validator.TryValidateObject(hv, new ValidationContext(hv), kq, true)) loi.AddRange(kq.Select(x => x.ErrorMessage ?? "Dữ liệu không hợp lệ."));
                if (hv.GioiTinh is not ("Nam" or "Nữ")) loi.Add("Giới tính phải là Nam hoặc Nữ.");
                if (hv.MaLop != "" && !lopCo.Contains(hv.MaLop)) loi.Add($"Lớp {hv.MaLop} không tồn tại.");
                if (hv.ChucVu != "Học viên" && !QuyTacDuLieu.ChiTieuChucVu.ContainsKey(hv.ChucVu)) loi.Add($"Chức vụ \"{hv.ChucVu}\" không hợp lệ.");
                if (!tuSinh && maCo.Contains(ma)) loi.Add($"Mã {ma} đã có trong hệ thống.");
                if (!trongTep.Ma.Add(ma)) loi.Add($"Mã {ma} lặp lại trong tệp.");
                if (hv.SoCCCD != "" && (cccdCo.Contains(hv.SoCCCD) || !trongTep.Cccd.Add(hv.SoCCCD))) loi.Add("Số CCCD đã được dùng (trong hệ thống hoặc dòng khác của tệp).");
                if (hv.SoDienThoai != "" && (sdtCo.Contains(hv.SoDienThoai) || !trongTep.Sdt.Add(hv.SoDienThoai))) loi.Add("Số điện thoại đã được dùng (trong hệ thống hoặc dòng khác của tệp).");
                ketQua.Add(new(r, hv, loi.Distinct().ToList(), tuSinh));
            }
            if (cuoi > 2001) return (ketQua, "Mỗi lần nhập tối đa 2.000 dòng; phần dư bị bỏ qua.");
            return (ketQua, ketQua.Count == 0 ? "Tệp chưa có dòng dữ liệu nào." : null);
        }
    }

    /// <summary>Ghi các dòng hợp lệ qua dịch vụ dữ liệu (kiểm quyền và quy tắc lại từng dòng, kể cả chỉ tiêu chức vụ).</summary>
    public async Task<(int ThanhCong, List<string> Loi)> XacNhanNhapAsync(IEnumerable<DongNhap> dong)
    {
        var thanhCong = 0; var loi = new List<string>();
        foreach (var d in dong.Where(d => d.HopLe))
        {
            var kq = await duLieu.LuuHocVienAsync(d.HocVien);
            if (kq.ThanhCong) thanhCong++; else loi.Add($"Dòng {d.SoDong}: {kq.ThongBao}");
        }
        await nhatKy.GhiAsync("Nhập Excel", "Học viên", $"{thanhCong} dòng thành công, {loi.Count} lỗi");
        return (thanhCong, loi);
    }
}
