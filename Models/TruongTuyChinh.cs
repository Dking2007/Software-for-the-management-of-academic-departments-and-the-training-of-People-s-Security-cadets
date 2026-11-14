// Vai trò tệp: Định nghĩa trường dữ liệu mở rộng và giá trị của trường trên từng đối tượng.
namespace Nhom2_BTL.Models;

/// <summary>Định nghĩa một trường dữ liệu mở rộng cho một loại đối tượng (Khoa, Lớp, Học viên...).</summary>
public sealed class TruongTuyChinh
{
    public int Id { get; set; }
    /// <summary>Mã loại: Khoa, ToChuyenMon, CanBo, Lop, HocVien, ChuyenDe, Phong.</summary>
    public string LoaiDoiTuong { get; set; } = "";
    public string Ten { get; set; } = "";
    public string KieuDuLieu { get; set; } = "Văn bản";
    /// <summary>Các lựa chọn của kiểu "Danh sách", mỗi dòng một lựa chọn.</summary>
    public string LuaChon { get; set; } = "";
    public string MoTa { get; set; } = "";
    public bool BatBuoc { get; set; }
    public bool HienTrenBang { get; set; } = true;
    public bool ChiChuSoHuu { get; set; }
    public int ThuTu { get; set; }
    public string NguoiTao { get; set; } = "";
    public DateTimeOffset TaoLuc { get; set; }
    public IReadOnlyList<string> CacLuaChon => LuaChon.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public static readonly string[] CacKieu = ["Văn bản", "Số", "Ngày", "Có/Không", "Danh sách"];
}

/// <summary>Giá trị của một trường mở rộng cho một đối tượng (khoá: trường + mã đối tượng).</summary>
public sealed class GiaTriTuyChinh
{
    public int TruongId { get; set; }
    public string MaDoiTuong { get; set; } = "";
    public string GiaTri { get; set; } = "";
}
