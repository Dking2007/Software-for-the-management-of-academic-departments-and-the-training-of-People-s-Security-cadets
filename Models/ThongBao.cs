using System.ComponentModel.DataAnnotations;

namespace Nhom2_BTL.Models;

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
    public bool LaMoi(DateTimeOffset hienTai) => hienTai >= DangLuc && hienTai - DangLuc < TimeSpan.FromHours(24);
    public ThongBao TaoBanSao() => new()
    {
        Ma = Ma, TieuDe = TieuDe, NoiDung = NoiDung, TacGia = TacGia,
        DangLuc = DangLuc, SuaLuc = SuaLuc, Tags = [.. Tags], Anh = Anh
    };
}

public sealed record TagThongBao(string Ma, string Ten);
