// Vai trò tệp: Bản tin Học viện: danh sách, chi tiết (tăng lượt xem), đăng/sửa/xoá (chỉ Admin).
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Bản tin Học viện: danh sách, chi tiết (tăng lượt xem), đăng/sửa/xoá (chỉ Admin).
/// Bố cục trang tham khảo các báo điện tử (đầu báo, tin mới chạy chữ, tin lớn + cột tin nổi bật, khối chuyên mục, đọc nhiều);
/// nội dung bài là giả lập, không lấy từ báo thật.
/// </summary>
public sealed class DichVuBanTin(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, DichVuPhanQuyenDemo phanQuyen, KhoDuLieuDemo kho)
{
    public async Task<List<TinTuc>> LayDanhSachAsync()
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return [];
        await using var db = await csdl.CreateDbContextAsync();
        return (await db.TinTuc.AsNoTracking().ToListAsync()).OrderByDescending(t => t.DangLuc).ToList();
    }

    public async Task<TinTuc?> MoBaiAsync(int id)
    {
        if (!(await phanQuyen.LayNguoiDungAsync()).DaDangNhap) return null;
        await using var db = await csdl.CreateDbContextAsync();
        db.BoQuaNhatKy = true; // lượt xem không phải thao tác nghiệp vụ, không ghi nhật ký
        var bai = await db.TinTuc.FindAsync(id);
        if (bai is null) return null;
        bai.LuotXem++;
        await db.SaveChangesAsync();
        return bai;
    }

    public async Task<KetQuaThaoTac> LuuAsync(TinTuc doiTuong, byte[]? anhMoi = null, bool xoaAnh = false)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaAdmin) return new(false, "Chỉ Admin được đăng, sửa bài trên bản tin.");
        var x = doiTuong.TaoBanSao();
        x.TieuDe = (x.TieuDe ?? "").Trim(); x.Sapo = (x.Sapo ?? "").Trim(); x.NoiDung = (x.NoiDung ?? "").Trim();
        var loi = new List<ValidationResult>();
        if (!Validator.TryValidateObject(x, new ValidationContext(x), loi, true)) return new(false, loi[0].ErrorMessage ?? "Dữ liệu không hợp lệ.");
        if (!TinTuc.CacChuyenMuc.Contains(x.ChuyenMuc)) return new(false, "Chuyên mục không hợp lệ.");
        string? anh = null;
        if (anhMoi is not null) { try { anh = DichVuThongBao.TaoAnh(anhMoi); } catch (ArgumentException e) { return new(false, e.Message); } }
        await using var db = await csdl.CreateDbContextAsync();
        db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro;
        if (x.Id == 0)
        {
            x.TacGia = nd.Ten; x.DangLuc = DateTimeOffset.Now; x.LuotXem = 0; x.Anh = anh;
            db.TinTuc.Add(x);
        }
        else
        {
            var cu = await db.TinTuc.FindAsync(x.Id);
            if (cu is null) return new(false, "Bài viết đã bị xoá.");
            cu.TieuDe = x.TieuDe; cu.Sapo = x.Sapo; cu.NoiDung = x.NoiDung; cu.ChuyenMuc = x.ChuyenMuc; cu.NoiBat = x.NoiBat;
            if (xoaAnh) cu.Anh = null;
            if (anh is not null) cu.Anh = anh;
        }
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, x.Id == 0 ? "Đã đăng bài." : "Đã cập nhật bài.", x.Id.ToString());
    }

    public async Task<KetQuaThaoTac> XoaAsync(int id)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaAdmin) return new(false, "Chỉ Admin được xoá bài.");
        await using var db = await csdl.CreateDbContextAsync();
        db.NguoiThucHien = nd.Ten; db.VaiTroThucHien = nd.TenVaiTro;
        var bai = await db.TinTuc.FindAsync(id);
        if (bai is null) return new(false, "Bài viết đã bị xoá.");
        db.TinTuc.Remove(bai);
        await db.SaveChangesAsync();
        kho.BaoThayDoiNgoai();
        return new(true, "Đã xoá bài.");
    }

    // ---------- Ảnh minh hoạ và liên kết liên quan ----------

    /// <summary>Ảnh minh hoạ (SVG vẽ sẵn trong wwwroot/images/ban-tin, không dùng ảnh bên ngoài) chọn theo nội dung tiêu đề, rồi theo chuyên mục.</summary>
    public static string AnhMinhHoa(TinTuc t)
    {
        var tieuDe = " " + TimKiemMo.ChuanHoa(t.TieuDe) + " ";
        foreach (var (tu, anh) in AnhTheoTuKhoa)
            if (tieuDe.Contains(" " + tu + " ")) return $"/images/ban-tin/{anh}.svg";
        var muc = Array.IndexOf(TinTuc.CacChuyenMuc, t.ChuyenMuc);
        return $"/images/ban-tin/{(muc >= 0 ? AnhTheoChuyenMuc[muc] : "hoi-nghi")}.svg";
    }

    // Thứ tự quan trọng: cụm cụ thể đứng trước cụm chung ("an ninh mang" trước "hoi thao").
    private static readonly (string Tu, string Anh)[] AnhTheoTuKhoa =
    [
        ("an ninh mang", "an-ninh-mang"), ("bao ve du lieu", "an-ninh-mang"), ("du lieu ca nhan", "an-ninh-mang"),
        ("vo thuat", "vo-thuat"), ("van nghe", "van-nghe"), ("dieu lenh", "huan-luyen"), ("huan luyen", "huan-luyen"),
        ("kiem tra", "kiem-tra"), ("lich thi", "kiem-tra"), ("cau lac bo", "cau-lac-bo"), ("chuyen doi so", "cau-lac-bo"),
        ("diem danh", "chuyen-doi-so"), ("quan ly lich", "chuyen-doi-so"), ("lien thong", "chuyen-doi-so"),
        ("nghiem thu", "nghien-cuu"), ("de tai", "nghien-cuu"), ("hoi thao", "nghien-cuu"),
        ("tieu bieu", "tieu-bieu"), ("guong mat", "tieu-bieu"), ("giao ban", "giao-ban"), ("hoi nghi", "hoi-nghi"),
        ("luat", "phap-luat"), ("phap luat", "phap-luat"),
    ];
    private static readonly string[] AnhTheoChuyenMuc = ["hoi-nghi", "kiem-tra", "nghien-cuu", "huan-luyen", "phap-luat", "vo-thuat"];

    public sealed record LienKet(string Ten, string DuongDan, string BieuTuong);

    /// <summary>Lối đi tiếp từ một bài viết tới đúng trang nghiệp vụ liên quan. Trang đích vẫn tự kiểm tra quyền.</summary>
    public static List<LienKet> LienKetLienQuan(TinTuc t)
    {
        var ds = new List<LienKet>();
        var tieuDe = " " + TimKiemMo.ChuanHoa(t.TieuDe + " " + t.Sapo) + " ";
        if (tieuDe.Contains(" lich ") || tieuDe.Contains(" diem danh ")) ds.Add(new("Lịch đào tạo tuần này", "/dao-tao/lich", "calendar_month"));
        if (tieuDe.Contains(" kiem tra ") || tieuDe.Contains(" thi ")) ds.Add(new("Xét tư cách & lịch thi", "/dao-tao/thi", "fact_check"));
        if (tieuDe.Contains(" lien thong ") || tieuDe.Contains(" khoa d58 ")) ds.Add(new("Lớp đào tạo theo hệ", "/quan-ly-hoc-vien/lop", "school"));
        ds.AddRange(t.ChuyenMuc switch
        {
            "Đào tạo" => [new("Chương trình & phân công giảng dạy", "/dao-tao/chuong-trinh", "assignment"), new("Tình hình lên lớp hôm nay", "/dao-tao/lich/tinh-hinh", "today")],
            "Học viên" => [new("Lớp đào tạo theo hệ", "/quan-ly-hoc-vien/lop", "school"), new("Tình hình lên lớp hôm nay", "/dao-tao/lich/tinh-hinh", "today")],
            "Nghiên cứu khoa học" => [new("Danh sách khoa", "/khoa", "account_balance"), new("Chuyên đề giảng dạy", "/dao-tao/chuyen-de", "menu_book")],
            "Pháp luật" => [new("Văn bản, thông báo", "/thong-bao", "gavel")],
            "Thể thao – Văn hoá" => [new("Văn bản, thông báo", "/thong-bao", "campaign"), new("Sơ đồ tổ chức", "/khoa/so-do", "account_tree")],
            _ => [new("Văn bản, thông báo", "/thong-bao", "description"), new("Sơ đồ tổ chức", "/khoa/so-do", "account_tree")],
        });
        return ds.DistinctBy(l => l.DuongDan).Take(4).ToList();
    }

    /// <summary>Một đoạn văn tách thành phần chữ thường và liên kết nội bộ viết theo cú pháp [chữ](/đường-dẫn).</summary>
    public sealed record PhanDoan(string Chu, string? DuongDan);
    private static readonly System.Text.RegularExpressions.Regex MauLienKet = new(@"\[([^\]\n]{1,120})\]\((/[A-Za-z0-9\-_/?=&%.#]{0,200})\)");
    public static List<PhanDoan> TachLienKet(string doan)
    {
        var kq = new List<PhanDoan>(); var vt = 0;
        foreach (System.Text.RegularExpressions.Match m in MauLienKet.Matches(doan))
        {
            // Chỉ nhận đường dẫn nội bộ "/..." (không "//"), tránh dẫn sang trang ngoài hoặc javascript:.
            if (m.Groups[2].Value.StartsWith("//")) continue;
            if (m.Index > vt) kq.Add(new(doan[vt..m.Index], null));
            kq.Add(new(m.Groups[1].Value, m.Groups[2].Value));
            vt = m.Index + m.Length;
        }
        if (vt < doan.Length) kq.Add(new(doan[vt..], null));
        return kq;
    }

    /// <summary>Bài mẫu (giả lập) theo các chuyên mục, ngày đăng tính lùi từ hôm nay.</summary>
    private static DateTimeOffset KhongTuongLai(DateTimeOffset luc, int thuTu)
    {
        var moiNhat = DateTimeOffset.Now.AddMinutes(-thuTu - 1);
        return luc > moiNhat ? moiNhat : luc;
    }

    private const string GhiChuMau = "Bài viết mẫu phục vụ minh hoạ hệ thống quản lý Khoa/Lớp đào tạo. Nội dung, số liệu và nhân vật đều là giả lập, không phản ánh sự kiện thật.";

    private static readonly (string Muc, string TieuDe, string Sapo, bool NoiBat, string Than)[] DanhSachMau =
    [
        ("Hoạt động Học viện", "Học viện tổ chức hội nghị triển khai nhiệm vụ năm học mới", "Hội nghị quán triệt mục tiêu nâng cao chất lượng đào tạo, đổi mới phương pháp giảng dạy và ứng dụng chuyển đổi số trong quản lý.", true,
            "Hội nghị thống nhất ba nhóm việc trọng tâm: rà soát chương trình đào tạo của từng hệ, chuẩn hoá lịch giảng dạy và tăng cường quản lý chuyên cần của học viên.\nCác khoa được giao xây dựng kế hoạch phân công giảng dạy ngay từ đầu năm học; tiến độ được theo dõi trên trang [Chương trình & phân công](/dao-tao/chuong-trinh).\nVăn bản hướng dẫn chi tiết được đăng tại mục [Văn bản, thông báo](/thong-bao)."),
        ("Đào tạo", "Đưa hệ thống quản lý lịch, điểm danh và điểm số vào sử dụng thử nghiệm", "Hệ thống giúp xếp lịch không trùng giảng viên, phòng học; học viên tự điểm danh trong khung giờ và theo dõi kết quả học tập.", true,
            "Khi xếp buổi học, hệ thống tự kiểm tra trùng giờ giảng viên, trùng phòng, sức chứa và phòng đang bảo trì, đồng thời nói rõ lý do nếu không xếp được.\nCán bộ quản lý có thể xem [Lịch đào tạo](/dao-tao/lich) theo lớp, theo tuần hoặc theo tháng, và biết ngay lớp nào nghỉ trong ngày ở trang [Tình hình lên lớp](/dao-tao/lich/tinh-hinh).\nĐiểm tổng kết được tính 30% chuyên cần và 70% kiểm tra, có nêu rõ khi còn thiếu điểm."),
        ("Nghiên cứu khoa học", "Hội thảo khoa học về an ninh mạng và phòng, chống tội phạm công nghệ cao", "Các nhà khoa học, giảng viên trao đổi nhiều giải pháp nâng cao năng lực phát hiện, ngăn chặn tội phạm sử dụng công nghệ cao.", true,
            "Hội thảo tập trung vào ba chủ đề: nhận diện phương thức tấn công mới, bảo vệ dữ liệu trong hệ thống thông tin và đào tạo nguồn nhân lực an ninh mạng.\nNhiều tham luận đề xuất đưa thêm tình huống thực hành vào các [chuyên đề giảng dạy](/dao-tao/chuyen-de) thuộc chuyên ngành ANM & PCTPCNC.\nKỷ yếu hội thảo sẽ được các [khoa](/khoa) chuyên môn phổ biến tới cán bộ, giảng viên."),
        ("Học viên", "Học viên khoá D58 hoàn thành đợt huấn luyện điều lệnh", "Sau ba tuần huấn luyện, học viên khoá mới đã nắm vững điều lệnh, nề nếp, tác phong theo quy định.", false,
            "Mười lăm lớp của khoá D58 tham gia huấn luyện tập trung với nội dung đội ngũ, điều lệnh nội vụ và nề nếp sinh hoạt.\nKết thúc đợt huấn luyện, các lớp bước vào học tập theo thời khoá biểu; danh sách lớp của khoá xem tại [Hệ chính quy · Khoá D58](/quan-ly-hoc-vien/he/chinh-quy?khoa=D58).\nBan quản lý học viên tiếp tục theo dõi chuyên cần của từng lớp hằng ngày."),
        ("Pháp luật", "Tuyên truyền Luật Phòng, chống tội phạm cho học viên các lớp chính quy", "Buổi sinh hoạt chuyên đề giúp học viên nắm được những điểm mới và trách nhiệm của người học trong thực thi pháp luật.", false,
            "Báo cáo viên giới thiệu những điểm mới của luật, phân tích tình huống thực tế và giải đáp câu hỏi của học viên.\nTài liệu tuyên truyền được đăng tại mục [Văn bản, thông báo](/thong-bao) để các lớp tổ chức sinh hoạt tiếp theo."),
        ("Thể thao – Văn hoá", "Sôi nổi giải võ thuật Công an nhân dân cấp Học viện", "Giải đấu thu hút đông đảo học viên tham gia ở nhiều nội dung đối kháng và quyền thuật.", true,
            "Các vận động viên đến từ nhiều khoá học thi đấu ở nội dung đối kháng, quyền thuật và võ thuật Công an nhân dân.\nGiải đấu là dịp để học viên rèn luyện thể lực, bản lĩnh và tinh thần đoàn kết; lịch tập luyện được bố trí xen kẽ với [lịch học](/dao-tao/lich) để không ảnh hưởng chương trình."),
        ("Đào tạo", "Khoa Luật tổ chức kiểm tra kết thúc chuyên đề Luật Hình sự", "Kiểm tra được tổ chức nghiêm túc, đúng quy chế; điểm tổng kết tính 30% chuyên cần và 70% kiểm tra.", false,
            "Trước khi kiểm tra, danh sách học viên đủ tư cách dự thi được xét theo quy trình: quản lý học viên đề xuất, khoa xác nhận, phòng đào tạo duyệt.\nKết quả xét và lịch thi của từng lớp được cập nhật tại trang [Xét tư cách & lịch thi](/dao-tao/thi)."),
        ("Hoạt động Học viện", "Ra mắt câu lạc bộ học thuật chuyển đổi số", "Câu lạc bộ là nơi học viên trao đổi kiến thức về dữ liệu, an toàn thông tin và lập trình ứng dụng.", false,
            "Câu lạc bộ sinh hoạt định kỳ hai tuần một lần với các chủ đề: phân tích dữ liệu, an toàn thông tin và xây dựng ứng dụng phục vụ quản lý.\nThành viên đầu tiên đến từ nhiều lớp thuộc [hệ chính quy và văn bằng 2](/quan-ly-hoc-vien/lop)."),
        ("Nghiên cứu khoa học", "Nghiệm thu đề tài cấp cơ sở về ứng dụng dữ liệu trong quản lý đào tạo", "Đề tài đề xuất mô hình quản lý lịch học, điểm danh và cảnh báo sớm học viên có nguy cơ không đạt.", false,
            "Hội đồng đánh giá cao tính thực tiễn của mô hình cảnh báo sớm dựa trên tỉ lệ vắng và điểm kiểm tra.\nKết quả đề tài được thử nghiệm trên dữ liệu mẫu của [lịch đào tạo](/dao-tao/lich) và sổ đầu bài các lớp."),
        ("Học viên", "Gương mặt học viên tiêu biểu trong học tập và rèn luyện", "Nhiều học viên đạt kết quả học tập xuất sắc, tích cực tham gia hoạt động phong trào của lớp và Đoàn thanh niên.", false,
            "Các học viên tiêu biểu được lựa chọn từ kết quả học tập, chuyên cần và hoạt động phong trào của từng lớp.\nDanh sách lớp và học viên theo từng hệ, khoá xem tại [Quản lý học viên](/quan-ly-hoc-vien/lop)."),
        ("Pháp luật", "Tập huấn kỹ năng bảo vệ dữ liệu cá nhân trong công tác", "Cán bộ, giảng viên được hướng dẫn nguyên tắc che dữ liệu nhạy cảm và ghi nhật ký truy cập.", false,
            "Nguyên tắc chính: chỉ xem dữ liệu cần cho công việc, dữ liệu cá nhân hiển thị dạng che và mọi lượt \"Hiện đủ\" đều được ghi nhật ký.\nHướng dẫn chi tiết được đăng tại [Văn bản, thông báo](/thong-bao)."),
        ("Thể thao – Văn hoá", "Hội diễn văn nghệ chào mừng ngày Nhà giáo Việt Nam", "Chương trình gồm nhiều tiết mục đặc sắc do học viên các khoa dàn dựng, tri ân thầy cô.", false,
            "Hội diễn quy tụ các tiết mục hát, múa và kịch ngắn do học viên nhiều khoá dàn dựng.\nThông báo lịch tổng duyệt và đêm diễn chính thức được đăng tại [Văn bản, thông báo](/thong-bao)."),
        ("Đào tạo", "Triển khai lớp liên thông đầu tiên theo quy ước mã lớp mới", "Lớp liên thông dùng chung quy ước mã lớp với hệ chính quy, đánh số từ 16 để không trùng mã.", false,
            "Mã lớp liên thông có dạng B{số lớp}D{khoá}, số lớp từ 16 đến 20; hệ thống tự sinh mã và niên khoá khi thêm lớp.\nLớp mới cần được phân công giảng dạy trước khi xếp lịch tại trang [Chương trình & phân công](/dao-tao/chuong-trinh)."),
        ("Hoạt động Học viện", "Giao ban công tác quản lý học viên tháng 11", "Cuộc họp đánh giá tình hình chuyên cần, nề nếp và thống nhất quy trình xét tư cách thi.", false,
            "Các đơn vị báo cáo tình hình lên lớp, số buổi vắng và các lớp cần nhắc nhở.\nTừ tháng này, số liệu lên lớp hằng ngày được theo dõi tại trang [Tình hình lên lớp](/dao-tao/lich/tinh-hinh); quy trình xét tư cách thi thực hiện tại [Xét tư cách & lịch thi](/dao-tao/thi)."),
    ];

    private static string TaoNoiDung(int i) => DanhSachMau[i].Sapo + "\n" + DanhSachMau[i].Than + "\n" + GhiChuMau;

    public static IEnumerable<TinTuc> BaiMau(DateTime homNay)
    {
        for (var i = 0; i < DanhSachMau.Length; i++)
            yield return new TinTuc
            {
                ChuyenMuc = DanhSachMau[i].Muc, TieuDe = DanhSachMau[i].TieuDe, Sapo = DanhSachMau[i].Sapo, NoiBat = DanhSachMau[i].NoiBat, TacGia = "Ban biên tập (mẫu)",
                NoiDung = TaoNoiDung(i),
                // Tin mẫu không được mang giờ tương lai, kể cả khi khởi tạo trước 8 giờ sáng.
                DangLuc = KhongTuongLai(new DateTimeOffset(homNay.AddDays(-i / 2.0).AddHours(8 + i % 9), TimeSpan.FromHours(7)), i),
                LuotXem = 120 + (i * 137) % 900
            };
    }

    /// <summary>CSDL tạo từ bản trước có nội dung bài mẫu một đoạn chung: thay bằng nội dung đầy đủ (có liên kết). Bài do người dùng viết không bị đụng tới.</summary>
    public static void NangCapBaiMau(CoSoDuLieuNghiepVu db)
    {
        var cu = db.TinTuc.Where(t => t.TacGia == "Ban biên tập (mẫu)" && t.NoiDung.Contains("Đây là bài viết mẫu trên Bản tin Học viện")).ToList();
        if (cu.Count == 0) return;
        foreach (var t in cu)
        {
            var i = Array.FindIndex(DanhSachMau, m => m.TieuDe == t.TieuDe);
            if (i >= 0) t.NoiDung = TaoNoiDung(i);
        }
        db.BoQuaNhatKy = true;
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }
}
