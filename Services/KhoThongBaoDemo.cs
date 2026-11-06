using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Kho thông báo dùng chung giữa các phiên.
/// Khởi tạo không tham số (bộ kiểm thử): ba thông báo mẫu trong bộ nhớ.
/// Khởi tạo với CSDL (ứng dụng): nạp từ bảng ThongBao, lần đầu sinh mẫu; mọi thay đổi được lưu xuống CSDL.
/// </summary>
public sealed class KhoThongBaoDemo
{
    internal object Khoa { get; } = new();
    internal List<ThongBao> DanhSach { get; private set; }
    private readonly IDbContextFactory<CoSoDuLieuNghiepVu>? nhaMayCsdl;
    public event Action? DaThayDoi;
    /// <summary>Báo khi có người đọc (để chuông cập nhật số chưa đọc), không nạp lại nội dung.</summary>
    public event Action? DaDocThayDoi;
    internal bool CoCsdl => nhaMayCsdl is not null;
    internal IDbContextFactory<CoSoDuLieuNghiepVu>? Csdl => nhaMayCsdl;

    public KhoThongBaoDemo() => DanhSach = MauCoBan();

    public KhoThongBaoDemo(IDbContextFactory<CoSoDuLieuNghiepVu> nhaMayCsdl)
    {
        this.nhaMayCsdl = nhaMayCsdl;
        using var db = nhaMayCsdl.CreateDbContext();
        db.DamBaoCauTruc();
        if (!db.ThongBao.Any())
        {
            DanhSach = [.. MauCoBan(), .. MauHanhChinh()];
            db.BoQuaNhatKy = true;
            db.ThongBao.AddRange(DanhSach.Select(x => x.TaoBanSao()));
            db.SaveChanges();
        }
        else DanhSach = db.ThongBao.AsNoTracking().ToList();
        // Văn bản mẫu chưa có ảnh: gắn ảnh minh hoạ vẽ bằng SVG (một lần, không ghi nhật ký).
        var canAnh = db.ThongBao.Where(x => x.Anh == null && x.TacGia == "Quản trị viên (mẫu)").ToList();
        if (canAnh.Count > 0)
        {
            foreach (var x in canAnh) x.Anh = AnhMinhHoa.BannerVanBan(x.LoaiVanBan, x.SoKyHieu, x.TieuDe);
            db.BoQuaNhatKy = true;
            db.SaveChanges();
            DanhSach = db.ThongBao.AsNoTracking().ToList();
        }
    }

    internal void ThongBaoThayDoi(string? nguoiThucHien = null)
    {
        if (nhaMayCsdl is not null)
            lock (Khoa)
            {
                using var db = nhaMayCsdl.CreateDbContext();
                db.NguoiThucHien = nguoiThucHien ?? "Hệ thống";
                var coSan = db.ThongBao.ToDictionary(x => x.Ma);
                foreach (var bai in DanhSach)
                {
                    if (coSan.Remove(bai.Ma, out var cu)) { db.Entry(cu).CurrentValues.SetValues(bai); cu.Tags = [.. bai.Tags]; }
                    else db.ThongBao.Add(bai.TaoBanSao());
                }
                db.ThongBao.RemoveRange(coSan.Values);
                db.SaveChanges();
            }
        DaThayDoi?.Invoke();
    }

    internal void BaoDaDoc() => DaDocThayDoi?.Invoke();

    private static List<ThongBao> MauCoBan() => Enumerable.Range(0, 3).Select(i => new ThongBao
    {
        Ma = Guid.NewGuid(),
        TieuDe = new[] { "Thông báo sử dụng cổng thông tin đào tạo", "Hướng dẫn tra cứu thông tin khoa và lớp", "Lưu ý khi sử dụng dữ liệu minh họa" }[i],
        NoiDung = new[] {
            "Cổng thông tin hỗ trợ tra cứu khoa, lớp đào tạo và học viên. Người dùng lựa chọn một trong ba phân hệ bên dưới để bắt đầu.\n\nBảng thông báo giúp cập nhật nội dung chung và các nội dung hướng tới từng khoa, hệ đào tạo hoặc lớp cụ thể.",
            "Sử dụng ô tìm kiếm và các bộ lọc trong từng phân hệ để tìm thông tin cần thiết. Các tag trên bảng thông báo cho biết đối tượng được hướng tới.\n\nĐây là nội dung tổng hợp phục vụ thực hành, không phải thông báo nghiệp vụ thật.",
            "Các hồ sơ trong ứng dụng là dữ liệu tổng hợp, không đại diện người thật. Đăng nhập và phân quyền chỉ phục vụ minh họa học tập.\n\nThông báo được lưu trong cơ sở dữ liệu cục bộ của bản demo."
        }[i],
        TacGia = "Quản trị viên (mẫu)",
        DangLuc = new DateTimeOffset(2026, 11, 3 - i, 8, 0, 0, TimeSpan.FromHours(7))
    }).ToList();

    // Văn bản mẫu theo thể thức hành chính; số hiệu, nội dung đều giả lập.
    private static IEnumerable<ThongBao> MauHanhChinh()
    {
        var gio7 = TimeSpan.FromHours(7);
        yield return new ThongBao
        {
            Ma = Guid.NewGuid(), LoaiVanBan = "Kế hoạch", SoKyHieu = "125/KH-HV", MucDo = "Quan trọng", Ghim = true,
            TieuDe = "Kế hoạch tổ chức kiểm tra kết thúc chuyên đề học kỳ I",
            TrichYeu = "Lịch, hình thức và yêu cầu kiểm tra kết thúc chuyên đề cho các lớp khoá D58 và K6.",
            NoiDung = "1. Thời gian: theo lịch đào tạo tuần cuối của mỗi chuyên đề.\n2. Hình thức: tự luận hoặc trắc nghiệm theo đề cương của khoa phụ trách.\n3. Điểm tổng kết = 30% chuyên cần + 70% kiểm tra; từ 5,0 là đạt.\n4. Học viên vắng kiểm tra có lý do được bố trí kiểm tra bổ sung.\n\n(Văn bản mẫu, nội dung giả lập.)",
            NguoiKy = "Phó Giám đốc (mẫu)", TacGia = "Quản trị viên (mẫu)", Tags = ["lop:*"],
            DangLuc = new DateTimeOffset(2026, 11, 4, 9, 0, 0, gio7)
        };
        yield return new ThongBao
        {
            Ma = Guid.NewGuid(), LoaiVanBan = "Công văn", SoKyHieu = "87/CV-ĐT", MucDo = "Khẩn",
            TieuDe = "Về việc rà soát, cập nhật hồ sơ học viên hệ chính quy",
            TrichYeu = "Đề nghị các lớp hệ chính quy rà soát số điện thoại, địa chỉ thường trú trước ngày 15/11.",
            NoiDung = "Đề nghị cán bộ quản lý học viên phối hợp với các lớp hệ chính quy rà soát, cập nhật thông tin liên hệ của học viên trên hệ thống trước ngày 15/11/2026.\n\n(Văn bản mẫu, nội dung giả lập.)",
            NguoiKy = "Trưởng phòng Đào tạo (mẫu)", TacGia = "Quản trị viên (mẫu)", Tags = ["he:Chính quy"],
            DangLuc = new DateTimeOffset(2026, 11, 5, 7, 30, 0, gio7), HieuLucDen = new DateTimeOffset(2026, 11, 16, 0, 0, 0, gio7)
        };
        yield return new ThongBao
        {
            Ma = Guid.NewGuid(), LoaiVanBan = "Lịch công tác", SoKyHieu = "45/LCT-HV", MucDo = "Thường",
            TieuDe = "Lịch công tác tuần 46",
            TrichYeu = "Lịch họp giao ban, sinh hoạt chuyên môn của các khoa và tổ.",
            NoiDung = "Thứ Hai: giao ban Ban Giám đốc.\nThứ Tư: sinh hoạt chuyên môn các tổ.\nThứ Sáu: họp các khoa về chương trình đào tạo.\n\n(Văn bản mẫu, nội dung giả lập.)",
            NguoiKy = "Chánh Văn phòng (mẫu)", TacGia = "Quản trị viên (mẫu)", Tags = ["khoa:KH-01", "khoa:KH-11"],
            DangLuc = new DateTimeOffset(2026, 11, 2, 16, 0, 0, gio7)
        };
    }
}
