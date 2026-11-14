// Vai trò tệp: Thông báo theo thể thức văn bản hành chính (ý tưởng từ repo LTMT): loại văn bản, số/ký hiệu, cơ quan ban hành, người ký, trích yếu, mức độ, hiệu lực hiển thị, ghim, nơi nhận (tags).
using System.ComponentModel.DataAnnotations;

namespace Nhom2_BTL.Models;

/// <summary>
/// Thông báo theo thể thức văn bản hành chính (ý tưởng từ repo LTMT): loại văn bản, số/ký hiệu,
/// cơ quan ban hành, người ký, trích yếu, mức độ, hiệu lực hiển thị, ghim, nơi nhận (tags).
/// </summary>
public sealed class ThongBao
{
    public Guid Ma { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề."), StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
    public string TieuDe { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập nội dung."), StringLength(20000, ErrorMessage = "Nội dung không được vượt quá 20.000 ký tự.")]
    public string NoiDung { get; set; } = "";
    public string TacGia { get; set; } = "";
    public DateTimeOffset DangLuc { get; set; }
    public DateTimeOffset? SuaLuc { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? Anh { get; set; }

    // ---- Thể thức văn bản hành chính ----
    public string LoaiVanBan { get; set; } = "Thông báo";
    [StringLength(40, ErrorMessage = "Số/ký hiệu không quá 40 ký tự.")]
    public string SoKyHieu { get; set; } = "";
    [StringLength(150, ErrorMessage = "Cơ quan ban hành không quá 150 ký tự.")]
    public string CoQuanBanHanh { get; set; } = "Học viện An ninh nhân dân";
    [StringLength(100, ErrorMessage = "Người ký không quá 100 ký tự.")]
    public string NguoiKy { get; set; } = "";
    [StringLength(300, ErrorMessage = "Trích yếu không quá 300 ký tự.")]
    public string TrichYeu { get; set; } = "";
    /// <summary>Thường / Quan trọng / Khẩn.</summary>
    public string MucDo { get; set; } = "Thường";
    /// <summary>Hết hạn thì tự ẩn với người xem (Admin vẫn thấy).</summary>
    public DateTimeOffset? HieuLucDen { get; set; }
    public bool Ghim { get; set; }

    public bool LaMoi(DateTimeOffset hienTai) => hienTai >= DangLuc && hienTai - DangLuc < TimeSpan.FromHours(24);
    public bool HetHieuLuc(DateTimeOffset hienTai) => HieuLucDen is { } han && han < hienTai;
    // Tạo bản sao phục vụ chỉnh sửa độc lập với dữ liệu gốc.
    public ThongBao TaoBanSao()
    {
        var b = (ThongBao)MemberwiseClone();
        b.Tags = [.. Tags];
        return b;
    }
}

/// <summary>Ai đã mở trang chi tiết thông báo, lúc nào ("đã đọc" = đã mở, chưa phải xác nhận).</summary>
public sealed class ThongBaoDaDoc
{
    public Guid MaThongBao { get; set; }
    public string TenDangNhap { get; set; } = "";
    public DateTimeOffset Luc { get; set; }
}

public sealed record TagThongBao(string Ma, string Ten);
