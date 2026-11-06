using System.Globalization;
using System.Text.RegularExpressions;

namespace Nhom2_BTL.Services;

/// <summary>Quy tắc dùng chung cho dữ liệu khởi tạo, kiểm tra nghiệp vụ và giao diện.</summary>
public static class QuyTacDuLieu
{
    /// <summary>Học viên có được thấy họ tên bạn cùng lớp không (câu hỏi còn mở; tạm đặt có, đổi bằng cấu hình "HocVienXemBanCungLop").</summary>
    public static bool HocVienXemBanCungLop { get; set; } = true;

    public static string TinhNienKhoa(string heDaoTao, string khoaHoc)
    {
        var dungKhoaD = heDaoTao is "Chính quy" or "Liên thông";
        var mau = dungKhoaD ? @"^D([0-9]+)$" : heDaoTao == "Văn bằng 2" ? @"^K([0-9]+)$" : "(?!)";
        var ketQua = Regex.Match(khoaHoc ?? "", mau);
        if (!ketQua.Success || !int.TryParse(ketQua.Groups[1].Value, out var khoa)) return "—";
        var namNhapHoc = 2026L + khoa - (dungKhoaD ? 58 : 6);
        return namNhapHoc is >= 1900 and <= 9995 ? $"{namNhapHoc}–{namNhapHoc + 4}" : "—";
    }

    // Khớp tên ở cuối họ tên: "Anh" không khớp "Thanh" hoặc "Anh Thư".
    // Giữ phân biệt dấu tiếng Việt, bỏ khác biệt hoa/thường và khoảng trắng thừa.
    public static bool TenKhopTuKhoa(string hoTen, string tuKhoa)
    {
        static string ChuanHoa(string giaTri) => string.Join(" ",
            giaTri.Normalize(System.Text.NormalizationForm.FormC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var cumTu = ChuanHoa(tuKhoa);
        return cumTu.Length == 0 || (" " + ChuanHoa(hoTen))
            .EndsWith(" " + cumTu, StringComparison.OrdinalIgnoreCase);
    }

    // Danh mục 34 tỉnh/thành theo Quyết định 19/2025/QĐ-TTg, nguồn ghi trong tài liệu bàn giao.
    public static readonly string[] TinhThanh = ["Hà Nội", "Cao Bằng", "Tuyên Quang", "Điện Biên", "Lai Châu", "Sơn La", "Lào Cai", "Thái Nguyên", "Lạng Sơn", "Quảng Ninh", "Bắc Ninh", "Phú Thọ", "Hải Phòng", "Hưng Yên", "Ninh Bình", "Thanh Hóa", "Nghệ An", "Hà Tĩnh", "Quảng Trị", "Huế", "Đà Nẵng", "Quảng Ngãi", "Gia Lai", "Khánh Hòa", "Đắk Lắk", "Lâm Đồng", "Đồng Nai", "Thành phố Hồ Chí Minh", "Tây Ninh", "Đồng Tháp", "Vĩnh Long", "An Giang", "Cần Thơ", "Cà Mau"];
    public static readonly IReadOnlyDictionary<string, int> ChiTieuChucVu = new Dictionary<string, int>
    { ["B trưởng"] = 1, ["B phó"] = 2, ["A trưởng"] = 4, ["A phó"] = 4, ["BTĐ"] = 1, ["PBT"] = 1, ["UVĐ"] = 3 };
    public static readonly StringComparer SoSanhTiengViet = StringComparer.Create(CultureInfo.GetCultureInfo("vi-VN"), true);

    // Sắp xếp theo tên chính, sau đó họ và tên đệm; dùng khóa so sánh tiếng Việt ở bảng.
    public static string KhoaSapXep(string hoTen)
    {
        var phan = hoTen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ten = phan.Length == 0 ? "" : phan[^1] + " " + string.Join(" ", phan[..^1]);
        return Convert.ToHexString(CultureInfo.GetCultureInfo("vi-VN").CompareInfo.GetSortKey(ten, CompareOptions.IgnoreCase).KeyData);
    }

    public static string ChuyenNganhChinhQuy(int so) => so switch
    { <= 3 => "Trinh sát an ninh", <= 7 => "Điều tra hình sự", 8 => "Quản lý xuất nhập cảnh", <= 12 => "ANM & PCTPCNC", _ => "Công nghệ thông tin" };

    /// <summary>Chuyên ngành suy ra từ hệ và số lớp. Văn bằng 2: số lẻ Điều tra, số chẵn ANM &amp; PCTPCNC. Liên thông: chọn tay (trả rỗng).</summary>
    public static string ChuyenNganhTheoSoLop(string he, int soLop)
        => he == "Chính quy" ? ChuyenNganhChinhQuy(soLop) : he == "Liên thông" ? "" : soLop % 2 == 1 ? "Điều tra" : "ANM & PCTPCNC";

    public static readonly string[] CacHeDaoTao = ["Chính quy", "Liên thông", "Văn bằng 2"];
    public static readonly string[] ChuyenNganhChinhQuyDanhMuc = ["Trinh sát an ninh", "Điều tra hình sự", "Quản lý xuất nhập cảnh", "ANM & PCTPCNC", "Công nghệ thông tin"];

    // Hệ Văn bằng 2 giữ mã B<số>K<khóa>; số lớp không bị giới hạn cứng bằng bốn.
    /// <summary>
    /// Liên thông dùng chung quy ước mã lớp B{số}D{khoá} với chính quy; để không trùng mã, số lớp liên thông là 16–20
    /// (chính quy 1–15). Chuyên ngành chọn trong danh mục chính quy.
    /// </summary>
    public static bool LopHopLe(string ma, string he, string khoa, string chuyenNganh)
    {
        if (he == "Liên thông")
        {
            var lt = Regex.Match(ma, @"^B(1[6-9]|20)(D5[3-8])$");
            return lt.Success && lt.Groups[2].Value == khoa && ChuyenNganhChinhQuyDanhMuc.Contains(chuyenNganh);
        }
        if (he == "Chính quy")
        {
            var m = Regex.Match(ma, @"^B([1-9]|1[0-5])(D5[3-8])$");
            return m.Success && m.Groups[2].Value == khoa && ChuyenNganhChinhQuy(int.Parse(m.Groups[1].Value)) == chuyenNganh;
        }
        var vb = Regex.Match(ma, @"^B([1-9][0-9]*)(K[3-6])$");
        return he == "Văn bằng 2" && vb.Success && vb.Groups[2].Value == khoa && ChuyenNganhTheoSoLop(he, int.Parse(vb.Groups[1].Value)) == chuyenNganh;
    }

    // Hoán vị ổn định giúp đổi đều họ, đệm và tên, không phụ thuộc ngẫu nhiên mỗi lần chạy.
    public static string TaoHoTen(int so, string gioiTinh)
    {
        string[] ho = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Đoàn", "Trịnh", "Mai"];
        string[] dem = gioiTinh == "Nữ" ? ["Thị", "Ngọc", "Thu", "Khánh", "Minh", "Bảo", "Thanh", "Phương", "Thùy", "Hồng", "Diệu", "Kim"] : ["Văn", "Đức", "Quốc", "Minh", "Hoàng", "Anh", "Tuấn", "Gia", "Hữu", "Thanh", "Đình", "Nhật"];
        string[] ten = gioiTinh == "Nữ" ? ["Linh", "Hà", "Anh", "Châu", "Ngọc", "Trang", "Hương", "Lan", "Mai", "Vy", "Yến", "Huyền", "Thảo", "Phương", "Trâm", "Ngân", "Quỳnh", "Nhung", "Hoa", "Chi", "Oanh", "Hiền", "Dung", "Tâm"] : ["Huy", "Bảo", "Quân", "Phúc", "Tuấn", "Khang", "Dũng", "Sơn", "Nam", "Long", "Minh", "Khôi", "Đạt", "Hiếu", "Kiên", "Lâm", "Phong", "Hùng", "Vinh", "Hải", "Thành", "Trung", "Cường", "Bình"];
        var n = (so * 137) % (ho.Length * dem.Length * ten.Length);
        return $"{ho[n % ho.Length]} {dem[n / ho.Length % dem.Length]} {ten[n / (ho.Length * dem.Length)]}";
    }
}

