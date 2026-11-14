// Vai trò tệp: Quy tắc miền của lịch học, điểm danh và điểm số.
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Quy tắc miền của lịch học, điểm danh và điểm số. Đặt ở một nơi, thuần tính toán (không đọc CSDL),
/// mọi kiểm tra trả về lý do bằng tiếng Việt để giao diện hiển thị nguyên văn.
/// </summary>
public static class QuyTacHocVu
{
    // Buổi sáng tiết 1–5, chiều 6–10, tối 11–12. Mỗi tiết 45 phút, nghỉ 5 phút.
    private static readonly TimeSpan[] GioBatDau =
    [
        new(7, 0, 0), new(7, 50, 0), new(8, 40, 0), new(9, 30, 0), new(10, 20, 0),
        new(13, 0, 0), new(13, 50, 0), new(14, 40, 0), new(15, 30, 0), new(16, 20, 0),
        new(18, 0, 0), new(18, 50, 0)
    ];
    public static readonly TimeSpan ThoiLuongTiet = TimeSpan.FromMinutes(45);
    /// <summary>Cổng tự điểm danh mở trước giờ học 30 phút và đóng sau giờ bắt đầu 15 phút.</summary>
    public static readonly TimeSpan MoTruoc = TimeSpan.FromMinutes(30), DongSau = TimeSpan.FromMinutes(15);

    public static TimeSpan GioBatDauTiet(int tiet) => GioBatDau[Math.Clamp(tiet, 1, 12) - 1];
    public static TimeSpan GioKetThucTiet(int tiet) => GioBatDauTiet(tiet) + ThoiLuongTiet;
    public static DateTime BatDau(BuoiHoc b) => b.Ngay.Date + GioBatDauTiet(b.TietBatDau);
    public static DateTime KetThuc(BuoiHoc b) => b.Ngay.Date + GioKetThucTiet(b.TietKetThuc);
    public static string KhungGio(BuoiHoc b) => $"{GioBatDauTiet(b.TietBatDau):hh\\:mm}–{GioKetThucTiet(b.TietKetThuc):hh\\:mm}";
    public static string Buoi(int tiet) => tiet <= 5 ? "Sáng" : tiet <= 10 ? "Chiều" : "Tối";

    /// <summary>Các tiết của một buổi phải nằm trọn trong một buổi (sáng/chiều/tối).</summary>
    public static string? KiemTraKhungTiet(int tietBatDau, int soTiet)
    {
        if (tietBatDau is < 1 or > 12) return "Tiết bắt đầu phải từ 1 đến 12.";
        if (soTiet is < 1 or > 5) return "Mỗi buổi từ 1 đến 5 tiết.";
        var tietCuoi = tietBatDau + soTiet - 1;
        if (tietCuoi > 12 || Buoi(tietBatDau) != Buoi(tietCuoi))
            return $"Các tiết {tietBatDau}–{tietCuoi} vượt qua ranh giới buổi {Buoi(tietBatDau).ToLowerInvariant()}. Buổi sáng tiết 1–5, chiều 6–10, tối 11–12.";
        return null;
    }

    public static bool TrungGio(BuoiHoc a, BuoiHoc b)
        => a.Ngay.Date == b.Ngay.Date && a.TietBatDau <= b.TietKetThuc && b.TietBatDau <= a.TietKetThuc;

    public static (DateTime Mo, DateTime Dong) CuaSoTuDiemDanh(BuoiHoc b) => (BatDau(b) - MoTruoc, BatDau(b) + DongSau);

    /// <summary>Lý do không tự điểm danh được lúc <paramref name="bayGio"/>; null nếu được.</summary>
    public static string? KiemTraTuDiemDanh(BuoiHoc b, DateTime bayGio)
    {
        var (mo, dong) = CuaSoTuDiemDanh(b);
        if (bayGio < mo) return $"Chưa đến giờ tự điểm danh. Cổng mở lúc {mo:HH:mm} ngày {mo:dd/MM} (trước giờ học 30 phút).";
        if (bayGio > dong) return $"Đã quá giờ tự điểm danh (đóng lúc {dong:HH:mm}, sau giờ học 15 phút). Liên hệ giảng viên để được điểm danh.";
        return null;
    }

    /// <summary>Tự điểm danh trong 5 phút đầu sau giờ học vẫn là có mặt; muộn hơn là đi muộn.</summary>
    public static string TrangThaiTuDiemDanh(BuoiHoc b, DateTime bayGio)
        => bayGio <= BatDau(b).AddMinutes(5) ? TrangThaiDiemDanh.CoMat : TrangThaiDiemDanh.DiMuon;

    // Trọng số chuyên cần cho từng trạng thái điểm danh.
    public static double TrongSo(string trangThai) => trangThai switch
    {
        TrangThaiDiemDanh.CoMat => 1.0,
        TrangThaiDiemDanh.DiMuon => 0.75,
        TrangThaiDiemDanh.VangCoPhep => 0.5,
        _ => 0.0
    };

    /// <summary>Điểm chuyên cần thang 10 từ các buổi đã điểm danh; null nếu chưa có buổi nào.</summary>
    public static double? TinhChuyenCan(IReadOnlyCollection<string> cacTrangThai)
        => cacTrangThai.Count == 0 ? null : Math.Round(10.0 * cacTrangThai.Sum(TrongSo) / cacTrangThai.Count, 1, MidpointRounding.AwayFromZero);

    /// <summary>Tổng kết = 30% chuyên cần + 70% kiểm tra, làm tròn 1 chữ số; thiếu điểm thì nói rõ thiếu điểm nào.</summary>
    public static (double? TongKet, string? LyDoThieu) TinhTongKet(double? chuyenCan, double? kiemTra)
    {
        var thieu = new List<string>();
        if (chuyenCan is null) thieu.Add("điểm chuyên cần (chưa có buổi nào được điểm danh)");
        if (kiemTra is null) thieu.Add("điểm kiểm tra");
        if (thieu.Count > 0) return (null, "Thiếu " + string.Join(" và ", thieu) + ".");
        return (Math.Round(0.3 * chuyenCan!.Value + 0.7 * kiemTra!.Value, 1, MidpointRounding.AwayFromZero), null);
    }

    public static string XepLoai(double? tongKet) => tongKet switch
    {
        null => "Chưa xếp loại",
        < 5.0 => "Không đạt",
        < 6.5 => "Đạt",
        < 8.0 => "Khá",
        < 9.0 => "Giỏi",
        _ => "Xuất sắc"
    };

    public static bool? Dat(double? tongKet) => tongKet is null ? null : tongKet >= 5.0;

    public static string? KiemTraDiem(double? diem)
        => diem is null or (>= 0 and <= 10) ? null : "Điểm phải từ 0 đến 10.";

    /// <summary>Năm học bắt đầu tháng 9: tháng 9–1 là học kỳ I, tháng 2–8 là học kỳ II.</summary>
    /// <summary>Quy đổi điểm hệ 10 sang điểm chữ và hệ 4 (thang tín chỉ thông dụng).</summary>
    public static (string Chu, double He4) QuyDoi(double? diem) => diem switch
    {
        null => ("—", 0),
        >= 8.5 => ("A", 4.0), >= 8.0 => ("B+", 3.5), >= 7.0 => ("B", 3.0), >= 6.5 => ("C+", 2.5),
        >= 5.5 => ("C", 2.0), >= 5.0 => ("D+", 1.5), >= 4.0 => ("D", 1.0), _ => ("F", 0)
    };

    /// <summary>Xếp loại học lực theo điểm trung bình tích luỹ hệ 10.</summary>
    public static string HocLuc(double? tb) => tb switch
    {
        null => "Chưa xếp loại", >= 9.0 => "Xuất sắc", >= 8.0 => "Giỏi", >= 6.5 => "Khá", >= 5.0 => "Trung bình", _ => "Yếu"
    };

    public static (string NamHoc, int HocKy) NamHocHocKy(DateTime ngay)
    {
        var namDau = ngay.Month >= 9 ? ngay.Year : ngay.Year - 1;
        var hk = ngay.Month >= 9 || ngay.Month == 1 ? 1 : 2;
        return ($"{namDau}–{namDau + 1}", hk);
    }

    /// <summary>Ngày thứ Hai của tuần chứa <paramref name="ngay"/>.</summary>
    public static DateTime DauTuan(DateTime ngay) => ngay.Date.AddDays(-(((int)ngay.DayOfWeek + 6) % 7));

    public static string TenThu(DateTime ngay) => ngay.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ Hai", DayOfWeek.Tuesday => "Thứ Ba", DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm", DayOfWeek.Friday => "Thứ Sáu", DayOfWeek.Saturday => "Thứ Bảy", _ => "Chủ nhật"
    };

    /// <summary>Hiển thị che dữ liệu cá nhân: giữ 3 ký tự đầu, 3 ký tự cuối.</summary>
    public static string Che(string? giaTri)
    {
        if (string.IsNullOrEmpty(giaTri)) return "";
        if (giaTri.Length <= 6) return new string('•', giaTri.Length);
        return giaTri[..3] + new string('•', giaTri.Length - 6) + giaTri[^3..];
    }
}
