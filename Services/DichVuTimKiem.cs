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
    public sealed record KetQuaTim(string Loai, string BieuTuong, string Ten, string Phu, string DuongDan);

    public async Task<List<KetQuaTim>> TimAsync(string tuKhoa, int toiDa = 30)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var k = BoDau(tuKhoa);
        if (!nd.DaDangNhap || k.Length < 2) return [];
        bool Khop(params string?[] cacTruong) => cacTruong.Any(t => t is not null && BoDau(t).Contains(k));

        var ketQua = new List<KetQuaTim>();
        ketQua.AddRange(CacTrang(nd).Where(t => Khop(t.Ten)));
        ketQua.AddRange(duLieu.LayDanhSachKhoa().Where(x => Khop(x.MaKhoa, x.TenKhoa))
            .Select(x => new KetQuaTim("Khoa", "account_balance", x.TenKhoa, x.MaKhoa, $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to")));
        ketQua.AddRange(duLieu.LayDanhSachTo().Where(x => Khop(x.MaTo, x.TenTo))
            .Select(x => new KetQuaTim("Tổ chuyên môn", "groups", $"{x.TenTo} · {duLieu.LayTenKhoa(x.MaKhoa)}", x.MaTo, $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to/{Uri.EscapeDataString(x.MaTo)}")));
        ketQua.AddRange(duLieu.LayDanhSachLop().Where(x => Khop(x.MaLop, x.ChuyenNganh, x.KhoaHoc))
            .Select(x => new KetQuaTim("Lớp", "school", x.MaLop, $"{x.HeDaoTao} · {x.KhoaHoc} · {x.ChuyenNganh}", $"/quan-ly-hoc-vien/lop/{Uri.EscapeDataString(x.MaLop)}")));
        ketQua.AddRange(duLieu.LayDanhSachCanBo().Where(x => Khop(x.HoTen, x.MaCanBo == "" ? null : x.MaCanBo, x.ChuyenMon))
            .Select(x => new KetQuaTim("Cán bộ", "badge", $"{x.CapBacHam} {x.HoTen}", $"{x.ChucVu} · {x.TenTo} · {x.TenKhoa}", $"/khoa/{Uri.EscapeDataString(x.MaKhoa)}/to/{Uri.EscapeDataString(x.MaTo ?? "")}")));
        // Học viên: kết quả chỉ gồm họ tên và lớp (mặt cắt tối thiểu), dẫn tới trang lớp.
        ketQua.AddRange(duLieu.LayDanhSachHocVien().Where(x => Khop(x.HoTen, x.MaHocVien == "" ? null : x.MaHocVien))
            .Select(x => new KetQuaTim("Học viên", "person", x.HoTen, $"Lớp {x.MaLop}", $"/quan-ly-hoc-vien/lop/{Uri.EscapeDataString(x.MaLop)}")));
        ketQua.AddRange((await daoTao.LayChuyenDeAsync()).Where(x => Khop(x.ChuyenDe.MaChuyenDe, x.ChuyenDe.TenChuyenDe))
            .Select(x => new KetQuaTim("Chuyên đề", "menu_book", x.ChuyenDe.TenChuyenDe, $"{x.ChuyenDe.MaChuyenDe} · {x.TenKhoa}", "/dao-tao/chuyen-de")));
        ketQua.AddRange((await daoTao.LayPhongAsync()).Where(x => Khop(x.MaPhong, x.TenPhong))
            .Select(x => new KetQuaTim("Phòng", "meeting_room", x.TenPhong, $"{x.LoaiPhong} · {x.SucChua} chỗ", "/dao-tao/phong")));
        return ketQua.Take(toiDa).ToList();
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
