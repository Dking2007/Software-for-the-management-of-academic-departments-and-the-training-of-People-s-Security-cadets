// Vai trò tệp: Mô hình chuyên đề, lịch, phòng, điểm danh, kết quả, phân công và xét thi.
using System.ComponentModel.DataAnnotations;

namespace Nhom2_BTL.Models;

/// <summary>Chuyên đề (môn học) do một khoa phụ trách giảng dạy.</summary>
public sealed class ChuyenDe
{
    [Required(ErrorMessage = "Vui lòng nhập mã chuyên đề.")]
    [StringLength(16, ErrorMessage = "Mã chuyên đề không quá 16 ký tự.")]
    public string MaChuyenDe { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên chuyên đề.")]
    [StringLength(150, ErrorMessage = "Tên chuyên đề không quá 150 ký tự.")]
    public string TenChuyenDe { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn khoa phụ trách.")]
    public string MaKhoa { get; set; } = "";

    [Range(1, 300, ErrorMessage = "Số tiết từ 1 đến 300.")]
    public int SoTiet { get; set; } = 45;

    [StringLength(250, ErrorMessage = "Mô tả không quá 250 ký tự.")]
    public string MoTa { get; set; } = "";

    // Tạo bản sao phục vụ chỉnh sửa độc lập với dữ liệu gốc.
    public ChuyenDe TaoBanSao() => (ChuyenDe)MemberwiseClone();
}

/// <summary>Cán bộ đủ điều kiện dạy một chuyên đề (quan hệ nhiều – nhiều).</summary>
public sealed class ChuyenMonGiangDay
{
    public string MaCanBo { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
}

/// <summary>Phòng học, phòng máy, phòng thi hoặc sân tập.</summary>
public sealed class Phong
{
    [Required(ErrorMessage = "Vui lòng nhập mã phòng.")]
    [StringLength(16, ErrorMessage = "Mã phòng không quá 16 ký tự.")]
    public string MaPhong { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập tên phòng.")]
    [StringLength(80, ErrorMessage = "Tên phòng không quá 80 ký tự.")]
    public string TenPhong { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
    public string LoaiPhong { get; set; } = "Giảng đường";

    [Range(1, 1000, ErrorMessage = "Sức chứa từ 1 đến 1.000 chỗ.")]
    public int SucChua { get; set; } = 60;

    public bool DangBaoTri { get; set; }

    // Tạo bản sao phục vụ chỉnh sửa độc lập với dữ liệu gốc.
    public Phong TaoBanSao() => (Phong)MemberwiseClone();
}

/// <summary>Một buổi học của một lớp: chuyên đề, giảng viên, phòng, ngày và các tiết.</summary>
public sealed class BuoiHoc
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn lớp.")] public string MaLop { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng chọn chuyên đề.")] public string MaChuyenDe { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng chọn giảng viên.")] public string MaCanBo { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng chọn phòng.")] public string MaPhong { get; set; } = "";
    public DateTime Ngay { get; set; } = DateTime.Today;
    [Range(1, 12, ErrorMessage = "Tiết bắt đầu từ 1 đến 12.")] public int TietBatDau { get; set; } = 1;
    [Range(1, 5, ErrorMessage = "Mỗi buổi từ 1 đến 5 tiết.")] public int SoTiet { get; set; } = 4;
    [StringLength(200)] public string GhiChu { get; set; } = "";

    public int TietKetThuc => TietBatDau + SoTiet - 1;
    // Tạo bản sao phục vụ chỉnh sửa độc lập với dữ liệu gốc.
    public BuoiHoc TaoBanSao() => (BuoiHoc)MemberwiseClone();
}

/// <summary>Trạng thái điểm danh của một học viên trong một buổi.</summary>
public sealed class DiemDanh
{
    public int Id { get; set; }
    public int BuoiHocId { get; set; }
    public string MaHocVien { get; set; } = "";
    public string TrangThai { get; set; } = TrangThaiDiemDanh.CoMat;
    public bool TuDiemDanh { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiGhi { get; set; } = "";
}

public static class TrangThaiDiemDanh
{
    public const string CoMat = "Có mặt";
    public const string DiMuon = "Đi muộn";
    public const string VangCoPhep = "Vắng có phép";
    public const string VangKhongPhep = "Vắng không phép";
    public static readonly string[] TatCa = [CoMat, DiMuon, VangCoPhep, VangKhongPhep];
}

/// <summary>Điểm kiểm tra của học viên ở một chuyên đề. Điểm chuyên cần được tính từ điểm danh, không lưu.</summary>
public sealed class KetQua
{
    public int Id { get; set; }
    public string MaHocVien { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public double? DiemKiemTra { get; set; }
    public string NguoiNhap { get; set; } = "";
    public DateTimeOffset? CapNhatLuc { get; set; }
}

/// <summary>Một dòng nhật ký thao tác. Chỉ thêm, không sửa hoặc xoá (CSDL có trigger chặn).</summary>
public sealed class NhatKy
{
    public long Id { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiThucHien { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string DoiTuong { get; set; } = "";
    public string MaDoiTuong { get; set; } = "";
    public string? GiaTriCu { get; set; }
    public string? GiaTriMoi { get; set; }
}

/// <summary>Một dòng bảng điểm đã tính: chuyên cần, kiểm tra, tổng kết, xếp loại và lý do nếu chưa đủ điểm.</summary>
public sealed record DongBangDiem(string MaHocVien, string HoTen, double? ChuyenCan, int SoBuoiDaDiemDanh,
    double? DiemKiemTra, double? TongKet, string XepLoai, bool? Dat, string? LyDoThieu);

/// <summary>Buổi học kèm tên hiển thị, dùng cho lịch và danh sách.</summary>
public sealed record BuoiHocHienThi(BuoiHoc Buoi, string TenChuyenDe, string TenGiangVien, string TenPhong, int SiSo, bool DaDiemDanh);

/// <summary>
/// Lịch sử bổ nhiệm lãnh đạo khoa/tổ (có từ ngày, đến ngày). Các cột MaTruongKhoa, MaToTruong... vẫn là giá trị hiện hành;
/// mỗi khi một vị trí đổi người, kho tự đóng bản ghi cũ (DenNgay) và mở bản ghi mới.
/// </summary>
public sealed class BoNhiem
{
    public int Id { get; set; }
    public string MaCanBo { get; set; } = "";
    public string ChucVu { get; set; } = "";
    /// <summary>"Khoa" hoặc "Tổ".</summary>
    public string LoaiDonVi { get; set; } = "";
    public string MaDonVi { get; set; } = "";
    public DateTime TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public bool ConHieuLuc => DenNgay is null;
}

/// <summary>Phân công giảng dạy: lớp – chuyên đề – giảng viên (khoa phụ trách suy ra từ chuyên đề). Lớp mới thì phân công trước khi xếp lịch.</summary>
public sealed class PhanCongGiangDay
{
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public string MaCanBo { get; set; } = "";
    public string GhiChu { get; set; } = "";
}

/// <summary>Trạng thái quy trình xét tư cách thi: Quản lý học viên đề xuất → Khoa xác nhận → Phòng đào tạo duyệt.</summary>
public static class TrangThaiXetTuCach
{
    public const string Nhap = "Chưa gửi";
    public const string ChoKhoa = "Chờ khoa xác nhận";
    public const string ChoDaoTao = "Chờ đào tạo duyệt";
    public const string DaDuyet = "Đã duyệt";
    public const string TraLai = "Trả lại";
}

/// <summary>Hồ sơ xét tư cách thi của một lớp ở một chuyên đề; danh sách không đủ tư cách được chốt khi gửi.</summary>
public sealed class XetTuCachThi
{
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public string TrangThai { get; set; } = TrangThaiXetTuCach.Nhap;
    /// <summary>JSON: mã học viên → lý do không đủ tư cách (chốt lúc gửi).</summary>
    public string KhongDuTuCach { get; set; } = "{}";
    public int SoHocVien { get; set; }
    public string NguoiDeXuat { get; set; } = "";
    public string NguoiXacNhan { get; set; } = "";
    public string NguoiDuyet { get; set; } = "";
    public string GhiChu { get; set; } = "";
    public DateTimeOffset? CapNhatLuc { get; set; }
}

/// <summary>Lịch thi do Phòng đào tạo sắp xếp, chỉ cho chuyên đề đã duyệt tư cách thi.</summary>
public sealed class LichThi
{
    public int Id { get; set; }
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public DateTime Ngay { get; set; } = DateTime.Today;
    public int TietBatDau { get; set; } = 1;
    public int SoTiet { get; set; } = 2;
    public string MaPhong { get; set; } = "";
    public string HinhThuc { get; set; } = "Tự luận";
    public string GhiChu { get; set; } = "";
    public int TietKetThuc => TietBatDau + SoTiet - 1;
    public static readonly string[] CacHinhThuc = ["Tự luận", "Trắc nghiệm", "Vấn đáp", "Thực hành"];
}

/// <summary>Thông tin tổng quát bài thi kết thúc học phần (Phòng đào tạo nhập sau khi duyệt tư cách), gửi tới học viên lớp.</summary>
public sealed class BaiThi
{
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    /// <summary>Thời gian làm bài (phút).</summary>
    public int ThoiGianLamBai { get; set; } = 90;
    public string CauTrucDe { get; set; } = "";
    public string PhamViOnTap { get; set; } = "";
    public string QuyDinhTaiLieu { get; set; } = "Không được sử dụng tài liệu";
    public string CanBoCoiThi { get; set; } = "";
    public string LuuY { get; set; } = "";
    public DateTimeOffset? DaGuiLuc { get; set; }
    public string NguoiGui { get; set; } = "";
    public Guid? MaThongBao { get; set; }
    public static readonly string[] CacQuyDinhTaiLieu = ["Không được sử dụng tài liệu", "Được sử dụng tài liệu giấy", "Được sử dụng mọi tài liệu"];
}

/// <summary>Lịch sử từng bước của hồ sơ xét tư cách thi (ai, vai trò, lúc nào, làm gì).</summary>
public sealed class LichSuXetTuCach
{
    public int Id { get; set; }
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public string HanhDong { get; set; } = "";
    public string NguoiThucHien { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public DateTimeOffset ThoiDiem { get; set; }
    public string GhiChu { get; set; } = "";
}

/// <summary>Quản lý học viên điều chỉnh tư cách một học viên trước khi gửi (ví dụ vắng do đi công tác được miễn trừ), bắt buộc ghi lý do.</summary>
public sealed class DieuChinhTuCach
{
    public string MaLop { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    public string MaHocVien { get; set; } = "";
    public bool Du { get; set; }
    public string LyDo { get; set; } = "";
    public string NguoiDieuChinh { get; set; } = "";
    public DateTimeOffset ThoiDiem { get; set; }
}

/// <summary>Điểm danh đi làm (chấm công) của cán bộ theo ngày.</summary>
public sealed class ChamCong
{
    public string MaCanBo { get; set; } = "";
    public DateTime Ngay { get; set; }
    public string TrangThai { get; set; } = "Có mặt";
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiGhi { get; set; } = "";
    public static readonly string[] CacTrangThai = ["Có mặt", "Đi muộn", "Công tác", "Nghỉ phép", "Vắng"];
}

/// <summary>Kết quả học tập các học kỳ đã qua (lưu trữ). Học kỳ hiện tại tính trực tiếp từ điểm danh và điểm kiểm tra.</summary>
public sealed class KetQuaLuuTru
{
    public int Id { get; set; }
    public string MaHocVien { get; set; } = "";
    public string MaChuyenDe { get; set; } = "";
    /// <summary>Ví dụ "2025–2026".</summary>
    public string NamHoc { get; set; } = "";
    public int HocKy { get; set; }
    public double ChuyenCan { get; set; }
    public double? DiemKiemTra { get; set; }
    public double? TongKet { get; set; }
    public DateTime? NgayThi { get; set; }
}

/// <summary>Sổ đầu bài online: giảng viên ghi nội dung, nhận xét, xếp loại tiết học cho từng buổi; Quản lý học viên và Quản lý đào tạo theo dõi.</summary>
public sealed class SoDauBai
{
    public int BuoiHocId { get; set; }
    public string NoiDung { get; set; } = "";
    public string NhanXet { get; set; } = "";
    /// <summary>A (tốt), B (khá), C (trung bình), D (yếu).</summary>
    public string XepLoaiTiet { get; set; } = "A";
    public string NguoiGhi { get; set; } = "";
    public DateTimeOffset ThoiDiem { get; set; }
    public static readonly string[] CacXepLoai = ["A", "B", "C", "D"];
}
