using System.Text;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Phân biệt hồ sơ nhập lặp, hồ sơ mâu thuẫn và trùng trường định danh.</summary>
public static class KiemTraTrungHoSo
{
    public static string? KiemTra(HocVien moi, IEnumerable<HocVien> hocVien, IEnumerable<CanBoGiangVien> canBo)
        => TimLoi(moi.HoTen, moi.SoDienThoai, moi.SoCCCD,
            hocVien.Select(x => new HoSo(x.HoTen, x.SoDienThoai, x.SoCCCD, GiongNhau(CacTruong(moi), CacTruong(x))))
                .Concat(canBo.Select(x => new HoSo(x.HoTen, x.SoDienThoai, x.SoCCCD, false))));

    public static string? KiemTra(CanBoGiangVien moi, IEnumerable<CanBoGiangVien> canBo, IEnumerable<HocVien> hocVien)
        => TimLoi(moi.HoTen, moi.SoDienThoai, moi.SoCCCD,
            canBo.Select(x => new HoSo(x.HoTen, x.SoDienThoai, x.SoCCCD, GiongNhau(CacTruong(moi), CacTruong(x))))
                .Concat(hocVien.Select(x => new HoSo(x.HoTen, x.SoDienThoai, x.SoCCCD, false))));

    // Không so mã hồ sơ, nhãn thời gian hay trường tính toán: đổi mã vẫn là nhập lặp một người.
    private static string?[] CacTruong(HocVien x) =>
        [x.HoTen, x.NgaySinh.ToString("O"), x.GioiTinh, x.QueQuan, x.SoDienThoai, x.SoCCCD,
         x.DiaChiThuongTru, x.Email, x.MaLop, x.ChucVu];

    private static string?[] CacTruong(CanBoGiangVien x) =>
        [x.HoTen, x.NgaySinh.ToString("O"), x.GioiTinh, x.QueQuan, x.SoDienThoai, x.SoCCCD,
         x.Email, x.MaKhoa, x.MaTo, x.ChucVu, x.CapBacHam, x.HocHam, x.HocVi, x.ChuyenMon];

    private static string ChuanHoa(string? x) => string.Join(" ",
        (x ?? "").Normalize(NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool GiongNhau(string?[] a, string?[] b)
        => a.Select(ChuanHoa).SequenceEqual(b.Select(ChuanHoa), StringComparer.OrdinalIgnoreCase);

    private sealed record HoSo(string HoTen, string SoDienThoai, string SoCCCD, bool TrungToanBo);

    private static string? TimLoi(string hoTen, string soDienThoai, string soCCCD, IEnumerable<HoSo> cacHoSo)
    {
        var trung = cacHoSo.Where(x => x.SoDienThoai == soDienThoai || x.SoCCCD == soCCCD).ToList();
        if (trung.Any(x => x.TrungToanBo))
            return "Hồ sơ trùng toàn bộ thông tin với hồ sơ đã có (không tính mã hồ sơ). Không thể thêm hoặc lưu bản trùng.";
        if (trung.Any(x => x.SoDienThoai == soDienThoai && x.SoCCCD == soCCCD &&
            string.Equals(ChuanHoa(x.HoTen), ChuanHoa(hoTen), StringComparison.OrdinalIgnoreCase)))
            return "Họ tên, số điện thoại và số CCCD trùng hồ sơ đã có, nhưng thông tin khác không khớp. Vui lòng kiểm tra và sửa hồ sơ hiện có.";

        var truongTrung = new List<string>();
        if (trung.Any(x => x.SoCCCD == soCCCD)) truongTrung.Add("Số CCCD");
        if (trung.Any(x => x.SoDienThoai == soDienThoai)) truongTrung.Add("Số điện thoại");
        return truongTrung.Count == 0 ? null : string.Join(" và ", truongTrung) + " đã được dùng cho hồ sơ khác. Vui lòng kiểm tra lại.";
    }
}

