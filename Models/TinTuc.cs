// Vai trò tệp: Bài viết trên Bản tin Học viện (nội dung giả lập phục vụ học tập).
using System.ComponentModel.DataAnnotations;

namespace Nhom2_BTL.Models;

/// <summary>Bài viết trên Bản tin Học viện (nội dung giả lập phục vụ học tập).</summary>
public sealed class TinTuc
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề."), StringLength(200, ErrorMessage = "Tiêu đề không quá 200 ký tự.")]
    public string TieuDe { get; set; } = "";
    /// <summary>Đoạn dẫn (sapo) in đậm dưới tiêu đề.</summary>
    [Required(ErrorMessage = "Vui lòng nhập đoạn dẫn."), StringLength(400, ErrorMessage = "Đoạn dẫn không quá 400 ký tự.")]
    public string Sapo { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập nội dung."), StringLength(20000)]
    public string NoiDung { get; set; } = "";
    public string ChuyenMuc { get; set; } = "Hoạt động Học viện";
    public string TacGia { get; set; } = "";
    public DateTimeOffset DangLuc { get; set; }
    public int LuotXem { get; set; }
    public bool NoiBat { get; set; }
    public string? Anh { get; set; }

    public static readonly string[] CacChuyenMuc = ["Hoạt động Học viện", "Đào tạo", "Nghiên cứu khoa học", "Học viên", "Pháp luật", "Thể thao – Văn hoá"];
    // Tạo bản sao phục vụ chỉnh sửa độc lập với dữ liệu gốc.
    public TinTuc TaoBanSao() => (TinTuc)MemberwiseClone();
}
