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

    /// <summary>Bài mẫu (giả lập) theo các chuyên mục, ngày đăng tính lùi từ hôm nay.</summary>
    public static IEnumerable<TinTuc> BaiMau(DateTime homNay)
    {
        (string Muc, string TieuDe, string Sapo, bool NoiBat)[] bai =
        [
            ("Hoạt động Học viện", "Học viện tổ chức hội nghị triển khai nhiệm vụ năm học mới", "Hội nghị quán triệt mục tiêu nâng cao chất lượng đào tạo, đổi mới phương pháp giảng dạy và ứng dụng chuyển đổi số trong quản lý.", true),
            ("Đào tạo", "Đưa hệ thống quản lý lịch, điểm danh và điểm số vào sử dụng thử nghiệm", "Hệ thống giúp xếp lịch không trùng giảng viên, phòng học; học viên tự điểm danh trong khung giờ và theo dõi kết quả học tập.", true),
            ("Nghiên cứu khoa học", "Hội thảo khoa học về an ninh mạng và phòng, chống tội phạm công nghệ cao", "Các nhà khoa học, giảng viên trao đổi nhiều giải pháp nâng cao năng lực phát hiện, ngăn chặn tội phạm sử dụng công nghệ cao.", true),
            ("Học viên", "Học viên khoá D58 hoàn thành đợt huấn luyện điều lệnh", "Sau ba tuần huấn luyện, học viên khoá mới đã nắm vững điều lệnh, nề nếp, tác phong theo quy định.", false),
            ("Pháp luật", "Tuyên truyền Luật Phòng, chống tội phạm cho học viên các lớp chính quy", "Buổi sinh hoạt chuyên đề giúp học viên nắm được những điểm mới và trách nhiệm của người học trong thực thi pháp luật.", false),
            ("Thể thao – Văn hoá", "Sôi nổi giải võ thuật Công an nhân dân cấp Học viện", "Giải đấu thu hút đông đảo học viên tham gia ở nhiều nội dung đối kháng và quyền thuật.", true),
            ("Đào tạo", "Khoa Luật tổ chức kiểm tra kết thúc chuyên đề Luật Hình sự", "Kiểm tra được tổ chức nghiêm túc, đúng quy chế; điểm tổng kết tính 30% chuyên cần và 70% kiểm tra.", false),
            ("Hoạt động Học viện", "Ra mắt câu lạc bộ học thuật chuyển đổi số", "Câu lạc bộ là nơi học viên trao đổi kiến thức về dữ liệu, an toàn thông tin và lập trình ứng dụng.", false),
            ("Nghiên cứu khoa học", "Nghiệm thu đề tài cấp cơ sở về ứng dụng dữ liệu trong quản lý đào tạo", "Đề tài đề xuất mô hình quản lý lịch học, điểm danh và cảnh báo sớm học viên có nguy cơ không đạt.", false),
            ("Học viên", "Gương mặt học viên tiêu biểu trong học tập và rèn luyện", "Nhiều học viên đạt kết quả học tập xuất sắc, tích cực tham gia hoạt động phong trào của lớp và Đoàn thanh niên.", false),
            ("Pháp luật", "Tập huấn kỹ năng bảo vệ dữ liệu cá nhân trong công tác", "Cán bộ, giảng viên được hướng dẫn nguyên tắc che dữ liệu nhạy cảm và ghi nhật ký truy cập.", false),
            ("Thể thao – Văn hoá", "Hội diễn văn nghệ chào mừng ngày Nhà giáo Việt Nam", "Chương trình gồm nhiều tiết mục đặc sắc do học viên các khoa dàn dựng, tri ân thầy cô.", false),
            ("Đào tạo", "Triển khai lớp liên thông đầu tiên theo quy ước mã lớp mới", "Lớp liên thông dùng chung quy ước mã lớp với hệ chính quy, đánh số từ 16 để không trùng mã.", false),
            ("Hoạt động Học viện", "Giao ban công tác quản lý học viên tháng 11", "Cuộc họp đánh giá tình hình chuyên cần, nề nếp và thống nhất quy trình xét tư cách thi.", false),
        ];
        for (var i = 0; i < bai.Length; i++)
            yield return new TinTuc
            {
                ChuyenMuc = bai[i].Muc, TieuDe = bai[i].TieuDe, Sapo = bai[i].Sapo, NoiBat = bai[i].NoiBat, TacGia = "Ban biên tập (mẫu)",
                NoiDung = bai[i].Sapo + "\n\nĐây là bài viết mẫu trên Bản tin Học viện, phục vụ minh hoạ giao diện và chức năng của hệ thống quản lý Khoa/Lớp đào tạo. Nội dung, số liệu và nhân vật đều là giả lập, không phản ánh sự kiện thật.\n\nBan biên tập có thể sửa, xoá bài hoặc đăng bài mới tại trang Bản tin.",
                DangLuc = new DateTimeOffset(homNay.AddDays(-i / 2.0).AddHours(8 + i % 9), TimeSpan.FromHours(7)),
                LuotXem = 120 + (i * 137) % 900
            };
    }
}
