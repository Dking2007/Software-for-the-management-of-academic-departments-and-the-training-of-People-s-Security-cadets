namespace Nhom2_BTL.Models;

/// <summary>
/// Một ca trực ban: một khoa, một ngày, một cán bộ trực. Ca trực là quyền hẹp có thời hạn (thiết kế mục 6.6):
/// đúng ngày trực, người trực được điểm danh đi làm cho cán bộ toàn khoa và nắm tổng hợp quân số học viên
/// học các chuyên đề của khoa; qua ngày hoặc đã chốt báo cáo thì quyền tự hết, không cần đổi vai trò tài khoản.
/// </summary>
public sealed class CaTrucBan
{
    public int Id { get; set; }
    public string MaKhoa { get; set; } = "";
    public DateTime Ngay { get; set; }
    public string MaCanBo { get; set; } = "";
    public string NguoiPhanCong { get; set; } = "";
    public DateTimeOffset PhanCongLuc { get; set; }

    // ---- Sổ trực ban ----
    /// <summary>Tình hình chung trong ca (quân số, nề nếp, an ninh trật tự…).</summary>
    public string TinhHinh { get; set; } = "";
    /// <summary>Sự việc phát sinh, đề xuất, nội dung bàn giao cho ca sau.</summary>
    public string BanGiao { get; set; } = "";
    /// <summary>Chốt báo cáo: sau khi chốt, ca bị khoá, mọi quyền của người trực kết thúc.</summary>
    public DateTimeOffset? ChotLuc { get; set; }
    /// <summary>Số liệu chụp lúc chốt, để báo cáo giữ nguyên dù dữ liệu gốc thay đổi về sau.</summary>
    public string TongHopCanBo { get; set; } = "";
    public string TongHopHocVien { get; set; } = "";
}

/// <summary>Quân số cán bộ trong ngày theo trạng thái điểm danh đi làm.</summary>
public sealed record QuanSoCanBo(int Tong, int CoMat, int DiMuon, int CongTac, int NghiPhep, int Vang, int ChuaDiemDanh)
{
    /// <summary>Có mặt tại đơn vị: có mặt + đi muộn.</summary>
    public int TaiDonVi => CoMat + DiMuon;
    public override string ToString() => $"Tổng {Tong} · Có mặt {CoMat} · Đi muộn {DiMuon} · Công tác {CongTac} · Nghỉ phép {NghiPhep} · Vắng {Vang} · Chưa điểm danh {ChuaDiemDanh}";
}

/// <summary>Quân số học viên trong các buổi học của khoa trong ngày, chỉ ở mức số liệu (hàng rào 2).</summary>
public sealed record QuanSoHocVien(int SoBuoi, int SoLop, int TheoLich, int CoMat, int DiMuon, int VangCoPhep, int VangKhongPhep, int BuoiChuaDiemDanh)
{
    public int DaDiemDanh => CoMat + DiMuon + VangCoPhep + VangKhongPhep;
    public override string ToString() => $"{SoBuoi} buổi · {SoLop} lớp · Theo lịch {TheoLich} · Có mặt {CoMat} · Đi muộn {DiMuon} · Vắng có phép {VangCoPhep} · Vắng không phép {VangKhongPhep} · Buổi chưa điểm danh {BuoiChuaDiemDanh}";
}
