using System.ComponentModel.DataAnnotations;

namespace Nhom2_BTL.Models;

/// <summary>Thông tin khoa quản lý trong dữ liệu giả lập.</summary>
public sealed class Khoa
{
    [Required(ErrorMessage = "Vui lòng nhập mã khoa.")]
    [StringLength(12, ErrorMessage = "Mã khoa không quá 12 ký tự.")]
    public string MaKhoa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên khoa.")]
    [StringLength(120, ErrorMessage = "Tên khoa không quá 120 ký tự.")]
    public string TenKhoa { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "Mô tả không quá 250 ký tự.")]
    public string MoTa { get; set; } = string.Empty;

    public string? MaTruongKhoa { get; set; }
    public string? MaPhoKhoa { get; set; }
    public string? MaPhoKhoaThuHai { get; set; }
    public int SiSo { get; internal set; }
    public bool DangHoatDong { get; set; } = true;

    // Bản sao tách dữ liệu biểu mẫu khỏi kho đang dùng chung.
    public Khoa TaoBanSao() => (Khoa)MemberwiseClone();
}

/// <summary>Tổ chuyên môn trực thuộc một khoa.</summary>
public sealed class ToChuyenMon
{
    [Required(ErrorMessage = "Vui lòng nhập mã tổ.")]
    [StringLength(16, ErrorMessage = "Mã tổ không quá 16 ký tự.")]
    public string MaTo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên tổ.")]
    [StringLength(80, ErrorMessage = "Tên tổ không quá 80 ký tự.")]
    public string TenTo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    public string MaKhoa { get; set; } = string.Empty;

    public string? MaToTruong { get; set; }
    public string? MaToPho { get; set; }
    public int SiSo { get; internal set; }

    // Bản sao tách dữ liệu biểu mẫu khỏi kho đang dùng chung.
    public ToChuyenMon TaoBanSao() => (ToChuyenMon)MemberwiseClone();
}

/// <summary>Thông tin cán bộ, giảng viên; mọi trường trong dữ liệu demo đều được tạo giả.</summary>
public sealed class CanBoGiangVien
{
    // Chỉ dùng trong bộ nhớ máy chủ để mở hồ sơ; không hiển thị hoặc tuần tự hóa.
    internal string MaTraCuuNoiBo { get; set; } = "";
    // Mốc thời gian chỉ dùng cho nhãn giao diện; dữ liệu khởi tạo không có nhãn mới.
    public DateTimeOffset? TaoLuc { get; set; }
    public DateTimeOffset? CapNhatLuc { get; set; }
    public string KhoaSapXepTen => Nhom2_BTL.Services.QuyTacDuLieu.KhoaSapXep(HoTen);
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(150)]
    public string? Email { get; set; }
    public string GioiTinh { get; set; } = "Nam";
    public string TenKhoa { get; internal set; } = "";
    public string TenTo { get; internal set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mã cán bộ.")]
    [StringLength(16, ErrorMessage = "Mã cán bộ không quá 16 ký tự.")]
    public string MaCanBo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn chức vụ.")]
    public string ChucVu { get; set; } = "Cán bộ giảng viên";

    [Required(ErrorMessage = "Vui lòng chọn cấp bậc hàm.")]
    public string CapBacHam { get; set; } = "Không xác định";

    public string HocHam { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn học vị.")]
    public string HocVi { get; set; } = "Cử nhân";

    [DataType(DataType.Date)]
    public DateTime NgaySinh { get; set; } = new(1985, 1, 1);

    [StringLength(100, ErrorMessage = "Quê quán không quá 100 ký tự.")]
    public string QueQuan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập chuyên môn.")]
    [StringLength(120, ErrorMessage = "Chuyên môn không quá 120 ký tự.")]
    public string ChuyenMon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"0[35789][0-9]{8}", ErrorMessage = "Số điện thoại phải có 10 chữ số và đầu số di động hợp lệ.")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CCCD.")]
    [RegularExpression(@"[0-9]{12}", ErrorMessage = "Số CCCD phải có đúng 12 chữ số.")]
    public string SoCCCD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn khoa.")]
    public string MaKhoa { get; set; } = string.Empty;

    public string? MaTo { get; set; }

    // Bản sao tách dữ liệu biểu mẫu khỏi kho đang dùng chung.
    public CanBoGiangVien TaoBanSao() => (CanBoGiangVien)MemberwiseClone();
}

/// <summary>Lớp thuộc một hệ và khóa đào tạo theo sơ đồ người dùng cung cấp.</summary>
public sealed class LopDaoTao
{
    [StringLength(120, ErrorMessage = "Giáo viên chủ nhiệm không quá 120 ký tự.")]
    public string GiaoVienChuNhiem { get; set; } = string.Empty;

    // Tính từ khóa học để niên khóa luôn đồng bộ khi thêm hoặc sửa lớp.
    public string NienKhoa => Nhom2_BTL.Services.QuyTacDuLieu.TinhNienKhoa(HeDaoTao, KhoaHoc);

    [Required(ErrorMessage = "Vui lòng nhập mã lớp.")]
    [StringLength(16, ErrorMessage = "Mã lớp không quá 16 ký tự.")]
    public string MaLop { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn hệ đào tạo.")]
    public string HeDaoTao { get; set; } = "Chính quy";

    [Required(ErrorMessage = "Vui lòng chọn khóa.")]
    public string KhoaHoc { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn chuyên ngành.")]
    public string ChuyenNganh { get; set; } = string.Empty;

    // Sĩ số là kết quả tính từ học viên, chỉ gán khi dựng bản sao để hiển thị.
    public int SiSo { get; internal set; }

    // Bản sao tách dữ liệu biểu mẫu khỏi kho đang dùng chung.
    public LopDaoTao TaoBanSao() => (LopDaoTao)MemberwiseClone();
}

/// <summary>Học viên thuộc lớp đào tạo; thông tin định danh chỉ là số liệu giả.</summary>
public sealed class HocVien
{
    // Mốc thời gian chỉ dùng cho nhãn giao diện; dữ liệu khởi tạo không có nhãn mới.
    public DateTimeOffset? TaoLuc { get; set; }
    public DateTimeOffset? CapNhatLuc { get; set; }
    public string KhoaSapXepTen => Nhom2_BTL.Services.QuyTacDuLieu.KhoaSapXep(HoTen);
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã học viên.")]
    [StringLength(16, ErrorMessage = "Mã học viên không quá 16 ký tự.")]
    public string MaHocVien { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên không quá 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn lớp.")]
    public string MaLop { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn chức vụ.")]
    public string ChucVu { get; set; } = "Học viên";

    [DataType(DataType.Date)]
    public DateTime NgaySinh { get; set; } = new(2000, 1, 1);

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string GioiTinh { get; set; } = "Nam";

    [StringLength(100, ErrorMessage = "Quê quán không quá 100 ký tự.")]
    public string QueQuan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(@"0[35789][0-9]{8}", ErrorMessage = "Số điện thoại phải có 10 chữ số và đầu số di động hợp lệ.")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số CCCD.")]
    [RegularExpression(@"[0-9]{12}", ErrorMessage = "Số CCCD phải có đúng 12 chữ số.")]
    public string SoCCCD { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ thường trú.")]
    [StringLength(180, ErrorMessage = "Địa chỉ không quá 180 ký tự.")]
    public string DiaChiThuongTru { get; set; } = string.Empty;

    // Bản sao tách dữ liệu biểu mẫu khỏi kho đang dùng chung.
    public HocVien TaoBanSao() => (HocVien)MemberwiseClone();
}

/// <summary>Kết quả thống nhất để component hiện thông báo thành công hoặc thất bại.</summary>
public sealed record KetQuaThaoTac(bool ThanhCong, string ThongBao, string? MaBanGhi = null);

/// <summary>Ảnh chụp thống kê hiện hành; mỗi nhánh chỉ đếm từ đúng dữ liệu nguồn của nhánh đó.</summary>
public sealed record ThongKeQuanLy(int SoKhoa, int SoTo, int SoCanBo, int SoLop, int SoHocVien,
    Dictionary<string, int> HocVienTheoLop, Dictionary<string, int> HocVienTheoKhoaHoc,
    Dictionary<string, int> HocVienTheoHe, Dictionary<string, int> CanBoTheoTo, Dictionary<string, int> CanBoTheoKhoa);

