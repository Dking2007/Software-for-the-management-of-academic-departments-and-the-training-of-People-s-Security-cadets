// Vai trò tệp: Số liệu "Cần chú ý" cho trang chủ: chỉ trả số lượng tổng hợp (không trả danh sách người), mỗi mục kèm đường dẫn tới đúng nơi xử lý.
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Số liệu "Cần chú ý" cho trang chủ: chỉ trả số lượng tổng hợp (không trả danh sách người),
/// mỗi mục kèm đường dẫn tới đúng nơi xử lý. Mục hiện theo vai trò.
/// </summary>
public sealed class DichVuTongQuan(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen, DichVuChuongTrinh chuongTrinh)
{
    public sealed record MucCanChuY(string Ten, int SoLuong, string MucDo, string DuongDan, string BieuTuong, string GoiY);

    public async Task<List<MucCanChuY>> LayCanChuYAsync()
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        var ds = new List<MucCanChuY>();
        if (!nd.DaDangNhap || nd.VaiTro == "Guest") return ds;
        var homNay = DateTime.Today;
        await using var db = await csdl.CreateDbContextAsync();
        int khoaThieuTruong, toThieuTruong, lopThieuGvcn;
        lock (kho.KhoaDongBo)
        {
            khoaThieuTruong = kho.DanhSachKhoa.Count(k => k.MaTruongKhoa is null);
            toThieuTruong = kho.DanhSachTo.Count(t => t.MaToTruong is null);
            lopThieuGvcn = kho.DanhSachLop.Count(l => string.IsNullOrWhiteSpace(l.GiaoVienChuNhiem));
        }
        if (nd.CoQuyenVung("Khoa") || nd.LaQuanLy("DaoTao"))
        {
            ds.Add(new("Khoa, tổ chưa có lãnh đạo", khoaThieuTruong + toThieuTruong, "canh-bao", "/khoa/so-do", "account_tree", "Mở sơ đồ tổ chức, bấm \"cần rà soát\"."));
            var cdThieuGv = await db.ChuyenDe.CountAsync(c => !db.ChuyenMonGiangDay.Any(m => m.MaChuyenDe == c.MaChuyenDe));
            ds.Add(new("Chuyên đề chưa có giáo viên đủ điều kiện", cdThieuGv, "canh-bao", "/dao-tao/chuyen-de", "menu_book", "Quản lý khoa chọn giáo viên đủ điều kiện dạy."));
        }
        if (nd.CoQuyenVung("DaoTao"))
        {
            var baoTri = await db.Phong.CountAsync(p => p.DangBaoTri);
            ds.Add(new("Phòng đang bảo trì", baoTri, "thong-tin", "/dao-tao/phong", "meeting_room", "Không xếp được lịch vào các phòng này."));
            var tuan = QuyTacHocVu.DauTuan(homNay);
            var lopCoLich = await db.BuoiHoc.Where(b => b.Ngay >= tuan && b.Ngay < tuan.AddDays(7)).Select(b => b.MaLop).Distinct().CountAsync();
            int tongLopKhoaMoi; lock (kho.KhoaDongBo) tongLopKhoaMoi = kho.DanhSachLop.Count(l => l.KhoaHoc is "D58" or "K6");
            ds.Add(new("Lớp khoá mới chưa có lịch tuần này", Math.Max(0, tongLopKhoaMoi - lopCoLich), "canh-bao", "/dao-tao/lich", "event_busy", "Xếp lịch ở Quản lý đào tạo → Lịch đào tạo."));
        }
        if (nd.CoQuyenVung("Lop") || nd.LaGiangVien)
        {
            var q = db.BuoiHoc.Where(b => b.Ngay < homNay && b.Ngay >= homNay.AddDays(-14));
            if (nd.LaGiangVien) q = q.Where(b => b.MaCanBo == nd.MaCanBo);
            var chuaDd = await q.CountAsync(b => !db.DiemDanh.Any(d => d.BuoiHocId == b.Id));
            ds.Add(new("Buổi đã qua chưa điểm danh (2 tuần)", chuaDd, "canh-bao", nd.LaGiangVien ? "/giang-day" : "/dao-tao/lich", "how_to_reg", "Mở buổi học và ghi điểm danh."));
            // Học viên đã học chuyên đề nhưng chưa có điểm kiểm tra (đếm theo cặp học viên – chuyên đề).
            var capDaHoc = await (from b in q.Select(b => new { b.MaLop, b.MaChuyenDe }).Distinct()
                                  join h in db.HocVien on b.MaLop equals h.MaLop
                                  select new { h.MaHocVien, b.MaChuyenDe }).Distinct().ToListAsync();
            var coDiem = (await db.KetQua.Where(k => k.DiemKiemTra != null).Select(k => new { k.MaHocVien, k.MaChuyenDe }).ToListAsync()).ToHashSet();
            ds.Add(new("Lượt học viên thiếu điểm kiểm tra", capDaHoc.Count(c => !coDiem.Contains(c)), "thong-tin", nd.LaGiangVien ? "/giang-day" : "/quan-ly-hoc-vien/lop", "grading", "Nhập điểm ở tab Điểm của trang lớp."));
        }
        if (nd.CoQuyenVung("DaoTao")) ds.Add(new("Lớp khoá mới chưa phân công giảng dạy", (await chuongTrinh.LopChuaPhanCongAsync()).Count, "canh-bao", "/dao-tao/chuong-trinh", "assignment_ind", "Phân công giảng viên trước khi xếp lịch."));
        // Hồ sơ xét tư cách thi đang chờ đúng vai trò xử lý.
        var choXuLy = nd.LaQuanLy("Khoa") ? TrangThaiXetTuCach.ChoKhoa : nd.LaQuanLy("DaoTao") ? TrangThaiXetTuCach.ChoDaoTao : nd.LaQuanLy("Lop") ? TrangThaiXetTuCach.TraLai : null;
        if (choXuLy is not null) ds.Add(new($"Hồ sơ tư cách thi: {choXuLy.ToLowerInvariant()}", await db.XetTuCach.CountAsync(x => x.TrangThai == choXuLy), "canh-bao", "/dao-tao/thi", "fact_check", "Mở Xét tư cách & lịch thi để xử lý."));
        if (nd.CoQuyenVung("Lop")) ds.Add(new("Lớp chưa có giáo viên chủ nhiệm", lopThieuGvcn, "canh-bao", "/quan-ly-hoc-vien/lop", "person_off", "Sửa lớp để bổ sung giáo viên chủ nhiệm."));
        return ds;
    }

    /// <summary>Một dòng "Hoạt động gần đây": bản ghi nhật ký kèm nhãn dễ đọc (tiêu đề/họ tên thay cho mã GUID).</summary>
    public sealed record HoatDong(NhatKy NhatKy, string Ten);

    public async Task<List<HoatDong>> LayHoatDongGanDayAsync(int soLuong = 6)
    {
        var nd = await phanQuyen.LayNguoiDungAsync();
        if (!nd.LaAdmin) return [];
        await using var db = await csdl.CreateDbContextAsync();
        var ds = await db.NhatKy.AsNoTracking().OrderByDescending(x => x.Id).Take(soLuong).ToListAsync();
        // Sửa một vài trường (ví dụ chỉ đổi ảnh) thì nhật ký không chứa tiêu đề: tra tiêu đề hiện tại của thông báo theo mã.
        var maTb = ds.Where(x => x.DoiTuong == "Thông báo" && Guid.TryParse(x.MaDoiTuong, out _)).Select(x => Guid.Parse(x.MaDoiTuong)).Distinct().ToList();
        var tieuDe = maTb.Count == 0 ? [] : await db.ThongBao.AsNoTracking().Where(t => maTb.Contains(t.Ma)).ToDictionaryAsync(t => t.Ma.ToString(), t => t.TieuDe);
        return ds.Select(x =>
        {
            var ten = DichVuNhatKy.TenHienThi(x);
            if (ten == x.DoiTuong && tieuDe.TryGetValue(x.MaDoiTuong, out var td)) ten = $"{x.DoiTuong}: {td}";
            return new HoatDong(x, ten);
        }).ToList();
    }
}
