// Vai trò tệp: Tìm kiếm nhanh (Ctrl+K).
using System.Globalization;
using System.Text;

namespace Nhom2_BTL.Services;

/// <summary>
/// Tìm kiếm nhanh (Ctrl+K). Chỉ tìm trong dữ liệu mà vai trò hiện tại được thấy, vì nguồn là
/// các mặt cắt đã lọc quyền của DichVuQuanLyDuLieuDemo và DichVuDaoTao.
/// So khớp không phân biệt hoa thường và dấu tiếng Việt để gõ nhanh ("nguyen van" khớp "Nguyễn Văn").
/// </summary>
public sealed class DichVuTimKiem(DichVuQuanLyDuLieuDemo duLieu, DichVuDaoTao daoTao, DichVuPhanQuyenDemo phanQuyen)
{
    public sealed record KetQuaTim(string Loai, string BieuTuong, string Ten, string Phu, string DuongDan)
    {
        /// <summary>True khi không có bản ghi khớp đủ mọi từ và đây là kết quả gần đúng hoặc khớp một phần.</summary>
        public bool GanDung { get; init; }
    }

    private sealed record UngVien(KetQuaTim KetQua, string?[] Truong);

    /// <summary>Số kết quả tối đa mỗi loại, để một loại đông (học viên) không che hết các loại khác.</summary>
    private const int ToiDaMoiLoai = 12;

    public async Task<List<KetQuaTim>> TimAsync(string tuKhoa, int toiDa = 30)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.DaDangNhap || BoDau(tuKhoa).Length < 2 || TimKiemMo.TachTu(tuKhoa).Length == 0) return [];

        // Các trường so khớp gồm cả đơn vị của bản ghi, nhờ đó "duy b10" hay "tổ 1 châu" tìm được đúng người.
        var nguon = new List<UngVien>();
        nguon.AddRange(CacTrang(nd).Select(t => new UngVien(t, [t.Ten])));
        var tenKhoa = duLieu.LayDanhSachKhoa().ToDictionary(x => x.MaKhoa, x => x.TenKhoa);
        nguon.AddRange(duLieu.LayDanhSachKhoa().Select(x => new UngVien(
            new KetQuaTim("Khoa", "account_balance", x.TenKhoa, x.MaKhoa, $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to"), [x.TenKhoa, x.MaKhoa])));
        nguon.AddRange(duLieu.LayDanhSachTo().Select(x => new UngVien(
            new KetQuaTim("Tổ chuyên môn", "groups", $"{x.TenTo} · {tenKhoa.GetValueOrDefault(x.MaKhoa, x.MaKhoa)}", x.MaTo, $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to/{Uri.EscapeDataString(x.MaTo)}"),
            [x.TenTo, x.MaTo, tenKhoa.GetValueOrDefault(x.MaKhoa, x.MaKhoa)])));
        nguon.AddRange(duLieu.LayDanhSachLop().Select(x => new UngVien(
            new KetQuaTim("Lớp", "school", x.MaLop, $"{x.HeDaoTao} · {x.KhoaHoc} · {x.ChuyenNganh}", $"/quan-ly-hoc-vien/lop/{Uri.EscapeDataString(x.MaLop)}"),
            [x.MaLop, x.ChuyenNganh, x.KhoaHoc, x.HeDaoTao, x.NienKhoa, x.GiaoVienChuNhiem])));
        nguon.AddRange(duLieu.LayDanhSachCanBo().Select(x => new UngVien(
            new KetQuaTim("Cán bộ", "badge", $"{x.CapBacHam} {x.HoTen}", $"{x.ChucVu} · {x.TenTo} · {x.TenKhoa}", $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to/{Uri.EscapeDataString(x.MaTo ?? "")}"),
            [x.HoTen, x.MaCanBo == "" ? null : x.MaCanBo, x.CapBacHam, x.ChucVu, x.TenTo, x.TenKhoa, x.ChuyenMon])));
        // Học viên: kết quả chỉ gồm họ tên và lớp (mặt cắt tối thiểu), dẫn tới trang lớp.
        nguon.AddRange(duLieu.LayDanhSachHocVien().Select(x => new UngVien(
            new KetQuaTim("Học viên", "person", x.HoTen, $"Lớp {x.MaLop}", $"/quan-ly-hoc-vien/lop/{Uri.EscapeDataString(x.MaLop)}"),
            [x.HoTen, x.MaHocVien == "" ? null : x.MaHocVien, x.MaLop])));
        nguon.AddRange((await daoTao.LayChuyenDeAsync()).Select(x => new UngVien(
            new KetQuaTim("Chuyên đề", "menu_book", x.ChuyenDe.TenChuyenDe, $"{x.ChuyenDe.MaChuyenDe} · {x.TenKhoa}", "/dao-tao/chuyen-de"),
            [x.ChuyenDe.TenChuyenDe, x.ChuyenDe.MaChuyenDe, x.TenKhoa])));
        nguon.AddRange((await daoTao.LayPhongAsync()).Select(x => new UngVien(
            new KetQuaTim("Phòng", "meeting_room", x.TenPhong, $"{x.LoaiPhong} · {x.SucChua} chỗ", "/dao-tao/phong"), [x.TenPhong, x.MaPhong, x.LoaiPhong])));

        var (ds, muc) = TimKiemMo.LocChiTiet(nguon, tuKhoa, u => u.Truong);
        var ganDung = muc != MucKhop.ChinhXac;
        return ds.GroupBy(u => u.KetQua.Loai).SelectMany(g => g.Take(ToiDaMoiLoai)).Take(toiDa)
            .Select(u => u.KetQua with { GanDung = ganDung }).ToList();
    }

    private static IEnumerable<KetQuaTim> CacTrang(NguoiDungHienTai nd)
    {
        yield return new("Trang", "home", "Trang chủ", "Tổng quan", "/");
        yield return new("Trang", "calendar_month", "Lịch đào tạo", "Lịch tuần, phòng, giảng viên", "/dao-tao/lich");
        if (nd.LaHocVien) yield return new("Trang", "person", "Học tập của tôi", "Lịch, điểm danh, điểm", "/cua-toi");
        if (nd.LaGiangVien) yield return new("Trang", "co_present", "Giảng dạy của tôi", "Lớp dạy, điểm danh, nhập điểm", "/giang-day");
        if (nd.LaAdmin) yield return new("Trang", "history", "Nhật ký thao tác", "Quản trị hệ thống", "/quan-tri/nhat-ky");
        if (DichVuTaiKhoan.VaiTroDuocQuanLy(new(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.Role, nd.VaiTro), new("PhamVi", nd.PhamVi)], "x"))).Count > 0)
            yield return new("Trang", "manage_accounts", "Tài khoản và phân quyền", "Quản trị hệ thống", "/quan-tri/tai-khoan");
        yield return new("Trang", "help_outline", "Trợ giúp", "Hướng dẫn sử dụng", "/tro-giup");
    }

    public static string BoDau(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var chuan = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(chuan.Length);
        foreach (var c in chuan) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
