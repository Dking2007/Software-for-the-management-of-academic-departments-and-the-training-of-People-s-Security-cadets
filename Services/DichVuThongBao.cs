using Microsoft.AspNetCore.Components.Authorization;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

public sealed class DichVuThongBao(KhoThongBaoDemo kho, KhoDuLieuDemo duLieu,
    AuthenticationStateProvider xacThuc, DichVuTaiKhoan taiKhoan)
{
    public const int GioiHanAnh = 3 * 1024 * 1024;
    public List<LopDaoTao> LayLop()
    {
        lock (duLieu.KhoaDongBo) return duLieu.DanhSachLop.Select(x => x.TaoBanSao()).ToList();
    }
    public static string TagKhoaHoc(LopDaoTao lop) => "khoahoc:" + Uri.EscapeDataString(lop.HeDaoTao) + "/" + Uri.EscapeDataString(lop.KhoaHoc);
    public static bool ThuocPhamVi(string tag, LopDaoTao lop) => tag == "lop:*" || tag == "he:" + lop.HeDaoTao || tag == TagKhoaHoc(lop) || tag == "lop:" + lop.MaLop;
    public static bool KhopPhamVi(IEnumerable<string> tags, string phamVi, IEnumerable<LopDaoTao> cacLop)
        => tags.Contains(phamVi) || cacLop.Any(lop => ThuocPhamVi(phamVi, lop) && tags.Any(tag => ThuocPhamVi(tag, lop)));
    public List<ThongBao> LayDanhSach()
    {
        lock (kho.Khoa) return kho.DanhSach.OrderByDescending(x => x.DangLuc).ThenBy(x => x.Ma).Select(x => x.TaoBanSao()).ToList();
    }
    public List<TagThongBao> LayTags()
    {
        lock (duLieu.KhoaDongBo)
            return duLieu.DanhSachKhoa.Select(x => new TagThongBao("khoa:" + x.MaKhoa, "Khoa · " + x.TenKhoa))
                .Append(new TagThongBao("lop:*", "Tất cả lớp đào tạo"))
                .Concat(duLieu.DanhSachLop.Select(x => x.HeDaoTao).Distinct().OrderBy(x => x).Select(x => new TagThongBao("he:" + x, "Hệ · " + x)))
                .Concat(duLieu.DanhSachLop.DistinctBy(x => new { x.HeDaoTao, x.KhoaHoc }).Select(x => new TagThongBao(TagKhoaHoc(x), "Khóa · " + x.KhoaHoc + " · " + x.HeDaoTao)))
                .Concat(duLieu.DanhSachLop.OrderBy(x => x.MaLop).Select(x => new TagThongBao("lop:" + x.MaLop, "Lớp · " + x.MaLop))).ToList();
    }
    private async Task<System.Security.Claims.ClaimsPrincipal> KiemTraAdmin()
    {
        var u = (await xacThuc.GetAuthenticationStateAsync()).User;
        if (u.Identity?.IsAuthenticated != true || !u.IsInRole("Admin") || !taiKhoan.HopLe(u))
            throw new UnauthorizedAccessException("Chỉ quản trị viên có phiên hợp lệ được thay đổi thông báo.");
        return u;
    }
    public async Task<Guid> LuuAsync(ThongBao banNhap, byte[]? anhMoi = null, bool xoaAnh = false)
    {
        var u = await KiemTraAdmin();
        var tieuDe = banNhap.TieuDe.Trim();
        var noiDung = banNhap.NoiDung.Trim();
        if (tieuDe.Length is 0 or > 200 || noiDung.Length is 0 or > 20000)
            throw new ArgumentException("Tiêu đề cần 1–200 ký tự; nội dung cần 1–20.000 ký tự.");
        var tags = banNhap.Tags.Distinct().ToList();
        var hopLe = LayTags().Select(x => x.Ma).ToHashSet();
        if (tags.Any(x => !hopLe.Contains(x))) throw new ArgumentException("Tag không còn tồn tại. Vui lòng chọn lại.");
        var anh = anhMoi is null ? null : TaoAnh(anhMoi);
        Guid ma;
        lock (kho.Khoa)
        {
            var bai = kho.DanhSach.SingleOrDefault(x => x.Ma == banNhap.Ma);
            if (banNhap.Ma != Guid.Empty && bai is null) throw new ArgumentException("Thông báo đã bị xóa. Vui lòng tải lại.");
            if (bai is null)
            {
                bai = new ThongBao { Ma = Guid.NewGuid(), TacGia = u.Identity!.Name ?? "Admin", DangLuc = DateTimeOffset.UtcNow };
                kho.DanhSach.Add(bai);
            }
            else bai.SuaLuc = DateTimeOffset.UtcNow;
            bai.TieuDe = tieuDe; bai.NoiDung = noiDung; bai.Tags = tags;
            if (xoaAnh) bai.Anh = null;
            if (anhMoi is not null) bai.Anh = anh;
            ma = bai.Ma;
        }
        kho.ThongBaoThayDoi();
        return ma;
    }
    public async Task XoaAsync(Guid ma)
    {
        await KiemTraAdmin();
        lock (kho.Khoa)
            if (kho.DanhSach.RemoveAll(x => x.Ma == ma) == 0) throw new ArgumentException("Thông báo đã bị xóa.");
        kho.ThongBaoThayDoi();
    }
    public static string TaoAnh(byte[] anh)
    {
        if (anh.Length is 0 or > GioiHanAnh) throw new ArgumentException("Ảnh phải có dung lượng không quá 3 MB.");
        // Kiểm tra chữ ký tệp phía máy chủ, không tin phần mở rộng hoặc MIME do trình duyệt gửi.
        var mime = anh.Length >= 8 && anh.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png"
            : anh.Length >= 3 && anh[0] == 255 && anh[1] == 216 && anh[2] == 255 ? "image/jpeg"
            : anh.Length >= 12 && System.Text.Encoding.ASCII.GetString(anh, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(anh, 8, 4) == "WEBP" ? "image/webp"
            : throw new ArgumentException("Chỉ chấp nhận tệp ảnh PNG, JPEG hoặc WebP.");
        return $"data:{mime};base64,{Convert.ToBase64String(anh)}";
    }
}
