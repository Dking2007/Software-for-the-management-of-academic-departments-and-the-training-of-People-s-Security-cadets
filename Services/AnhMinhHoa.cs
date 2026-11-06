using System.Net;
using System.Text;

namespace Nhom2_BTL.Services;

/// <summary>
/// Ảnh minh hoạ vẽ bằng SVG (không dùng ảnh bên ngoài): banner cho văn bản, thông báo mẫu.
/// Ảnh SVG trong thẻ img không chạy được script nên an toàn để hiển thị.
/// </summary>
public static class AnhMinhHoa
{
    private static readonly Dictionary<string, (string Tu, string Den, string Hinh)> MauTheoLoai = new()
    {
        ["Kế hoạch"] = ("#1b4b34", "#2d6b49", "lich"),
        ["Công văn"] = ("#8c1d17", "#b3261e", "van-ban"),
        ["Lịch công tác"] = ("#1e40af", "#3b82c4", "lich"),
        ["Quyết định"] = ("#5b21b6", "#7c4dbd", "con-dau"),
        ["Hướng dẫn"] = ("#0f766e", "#14a094", "so-tay"),
        ["Thông báo"] = ("#9a6a12", "#c99a2e", "loa"),
    };

    public static string BannerVanBan(string loai, string soKyHieu, string tieuDe)
    {
        var (tu, den, hinh) = MauTheoLoai.GetValueOrDefault(loai, MauTheoLoai["Thông báo"]);
        var dong = TachDong(tieuDe, 34, 2);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 480' width='1200' height='480'>");
        sb.Append($"<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'><stop offset='0' stop-color='{tu}'/><stop offset='1' stop-color='{den}'/></linearGradient>");
        sb.Append("<pattern id='p' width='28' height='28' patternUnits='userSpaceOnUse' patternTransform='rotate(45)'><rect width='2' height='28' fill='rgba(255,255,255,0.06)'/></pattern></defs>");
        sb.Append("<rect width='1200' height='480' fill='url(#g)'/><rect width='1200' height='480' fill='url(#p)'/>");
        sb.Append("<circle cx='1080' cy='70' r='190' fill='rgba(255,255,255,0.07)'/><circle cx='1150' cy='420' r='120' fill='rgba(255,255,255,0.05)'/>");
        sb.Append("<rect x='0' y='0' width='1200' height='10' fill='#e4cf8c'/>");
        // Biểu tượng theo loại văn bản (vẽ bằng hình cơ bản).
        sb.Append("<g transform='translate(900,150)' fill='none' stroke='rgba(255,255,255,0.9)' stroke-width='10' stroke-linejoin='round' stroke-linecap='round'>");
        sb.Append(hinh switch
        {
            "lich" => "<rect x='0' y='20' width='200' height='180' rx='18'/><line x1='0' y1='70' x2='200' y2='70'/><line x1='50' y1='0' x2='50' y2='40'/><line x1='150' y1='0' x2='150' y2='40'/><rect x='35' y='100' width='30' height='30' fill='rgba(255,255,255,0.9)' stroke='none'/><rect x='85' y='100' width='30' height='30' fill='rgba(255,255,255,0.5)' stroke='none'/><rect x='135' y='100' width='30' height='30' fill='rgba(255,255,255,0.5)' stroke='none'/><rect x='35' y='145' width='30' height='30' fill='rgba(255,255,255,0.5)' stroke='none'/>",
            "van-ban" => "<path d='M20 0 H140 L200 60 V220 H20 Z'/><path d='M140 0 V60 H200'/><line x1='55' y1='105' x2='165' y2='105'/><line x1='55' y1='145' x2='165' y2='145'/><line x1='55' y1='185' x2='125' y2='185'/>",
            "con-dau" => "<circle cx='100' cy='100' r='95'/><circle cx='100' cy='100' r='65'/><path d='M100 55 L112 90 H148 L119 111 L130 146 L100 125 L70 146 L81 111 L52 90 H88 Z' fill='rgba(255,255,255,0.9)' stroke='none'/>",
            "so-tay" => "<rect x='20' y='0' width='170' height='220' rx='14'/><line x1='60' y1='0' x2='60' y2='220'/><line x1='90' y1='60' x2='160' y2='60'/><line x1='90' y1='100' x2='160' y2='100'/>",
            _ => "<path d='M10 90 L120 40 V180 L10 130 Z'/><path d='M120 60 Q190 110 120 160'/><path d='M30 135 L50 210 H85 L70 140'/>",
        });
        sb.Append("</g>");
        sb.Append("<text x='70' y='120' font-family='Segoe UI, Arial, sans-serif' font-size='30' font-weight='700' fill='#e4cf8c' letter-spacing='3'>");
        sb.Append(WebUtility.HtmlEncode((loai + (soKyHieu == "" ? "" : " · " + soKyHieu)).ToUpperInvariant())).Append("</text>");
        for (var i = 0; i < dong.Count; i++)
            sb.Append($"<text x='70' y='{215 + i * 70}' font-family='Segoe UI, Arial, sans-serif' font-size='54' font-weight='700' fill='#ffffff'>{WebUtility.HtmlEncode(dong[i])}</text>");
        sb.Append("<text x='70' y='420' font-family='Segoe UI, Arial, sans-serif' font-size='24' fill='rgba(255,255,255,0.75)'>HỌC VIỆN AN NINH NHÂN DÂN · ẢNH MINH HOẠ</text>");
        sb.Append("</svg>");
        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static List<string> TachDong(string chu, int toiDa, int soDong)
    {
        var dong = new List<string>(); var hienTai = "";
        foreach (var tu in chu.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if ((hienTai + " " + tu).Trim().Length > toiDa) { dong.Add(hienTai.Trim()); hienTai = tu; if (dong.Count == soDong) break; }
            else hienTai = (hienTai + " " + tu).Trim();
        }
        if (dong.Count < soDong && hienTai != "") dong.Add(hienTai);
        else if (dong.Count == soDong && hienTai != "" && !dong[^1].EndsWith('…')) dong[^1] += "…";
        return dong;
    }
}
