// Vai trò tệp: Quản lý văn bản, nơi nhận, trạng thái đã đọc và quyền thao tác.
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

public sealed class DichVuThongBao(KhoThongBaoDemo kho, KhoDuLieuDemo duLieu,
    AuthenticationStateProvider xacThuc, DichVuTaiKhoan taiKhoan)
{
    public const int GioiHanAnh = 3 * 1024 * 1024;
    public static readonly string[] CacLoaiVanBan = ["Thông báo", "Kế hoạch", "Quyết định", "Công văn", "Hướng dẫn", "Lịch công tác"];
    public static readonly string[] CacMucDo = ["Thường", "Quan trọng", "Khẩn"];
    /// <summary>Ký hiệu viết tắt theo loại văn bản, để gợi ý số/ký hiệu (ví dụ 126/KH-HV).</summary>
    public static string KyHieuLoai(string loai) => loai switch { "Kế hoạch" => "KH", "Quyết định" => "QĐ", "Công văn" => "CV", "Hướng dẫn" => "HD", "Lịch công tác" => "LCT", _ => "TB" };
    public string GoiYSoKyHieu(string loai)
    {
        var kh = KyHieuLoai(loai);
        lock (kho.Khoa)
        {
            var lonNhat = kho.DanhSach.Where(x => x.SoKyHieu.Contains("/" + kh + "-")).Select(x => int.TryParse(x.SoKyHieu.Split('/')[0], out var so) ? so : 0).DefaultIfEmpty(0).Max();
            return $"{lonNhat + 1}/{kh}-HV";
        }
    }
    private static int HangMucDo(string mucDo) => mucDo switch { "Khẩn" => 0, "Quan trọng" => 1, _ => 2 };

    // ---------- Nơi nhận, đã đọc ----------

    private async Task<NguoiDungHienTai> NguoiDungAsync() => NguoiDungHienTai.Tu((await xacThuc.GetAuthenticationStateAsync()).User);

    /// <summary>
    /// Thông báo gửi tới người đang đăng nhập: thông báo chung (không tag) luôn thấy; học viên thấy thêm thông báo
    /// đúng lớp/khoá/hệ của mình; giảng viên thấy thêm thông báo của khoa mình và lớp mình dạy; vai trò khác thấy tất cả.
    /// Hết hiệu lực thì ẩn với mọi người trừ Admin. Ghim lên đầu, rồi theo ngày đăng.
    /// </summary>
    public async Task<List<ThongBao>> LayDanhSachChoToiAsync()
    {
        var nd = await NguoiDungAsync();
        if (!nd.DaDangNhap) return [];
        var bayGio = DateTimeOffset.UtcNow;
        List<LopDaoTao> lopCuaToi = [];
        string? khoaCuaToi = null;
        lock (duLieu.KhoaDongBo)
        {
            if (nd.LaHocVien && duLieu.DanhSachHocVien.FirstOrDefault(h => h.MaHocVien == nd.MaHocVien) is { } hv)
                lopCuaToi = duLieu.DanhSachLop.Where(l => l.MaLop == hv.MaLop).ToList();
            if (nd.LaGiangVien) khoaCuaToi = duLieu.DanhSachCanBo.FirstOrDefault(c => c.MaCanBo == nd.MaCanBo)?.MaKhoa;
        }
        if (nd.LaGiangVien) { var day = duLieu.LopGiangVienDay(nd.MaCanBo!); lock (duLieu.KhoaDongBo) lopCuaToi = duLieu.DanhSachLop.Where(l => day.Contains(l.MaLop)).ToList(); }
        bool GuiToi(ThongBao x)
        {
            if (!nd.LaHocVien && !nd.LaGiangVien) return true;
            if (x.Tags.Count == 0) return true;
            if (khoaCuaToi is not null && x.Tags.Contains("khoa:" + khoaCuaToi)) return true;
            return x.Tags.Any(t => lopCuaToi.Any(l => ThuocPhamVi(t, l)));
        }
        return LayDanhSach().Where(x => (nd.LaAdmin || !x.HetHieuLuc(bayGio)) && GuiToi(x))
            .OrderByDescending(x => x.Ghim).ThenByDescending(x => x.DangLuc).ToList();
    }

    /// <summary>Ba thông báo ưu tiên cho trang chủ: ghim, khẩn, quan trọng, rồi mới nhất.</summary>
    public async Task<List<ThongBao>> LayUuTienAsync(int soLuong = 3)
        => (await LayDanhSachChoToiAsync()).OrderByDescending(x => x.Ghim).ThenBy(x => HangMucDo(x.MucDo)).ThenByDescending(x => x.DangLuc).Take(soLuong).ToList();

    public async Task<HashSet<Guid>> LayDaDocAsync()
    {
        var nd = await NguoiDungAsync();
        if (!nd.DaDangNhap || kho.Csdl is null) return [];
        await using var db = await kho.Csdl.CreateDbContextAsync();
        return (await db.ThongBaoDaDoc.Where(x => x.TenDangNhap == nd.Ten).Select(x => x.MaThongBao).ToListAsync()).ToHashSet();
    }

    public async Task<int> DemChuaDocAsync()
    {
        var daDoc = await LayDaDocAsync();
        return (await LayDanhSachChoToiAsync()).Count(x => !daDoc.Contains(x.Ma));
    }

    /// <summary>Ghi "đã đọc" khi người dùng mở chi tiết (một lần cho mỗi người, mỗi thông báo).</summary>
    public async Task DanhDauDaDocAsync(Guid ma)
    {
        var nd = await NguoiDungAsync();
        if (!nd.DaDangNhap || kho.Csdl is null) return;
        await using var db = await kho.Csdl.CreateDbContextAsync();
        if (await db.ThongBaoDaDoc.AnyAsync(x => x.MaThongBao == ma && x.TenDangNhap == nd.Ten) || !await db.ThongBao.AnyAsync(x => x.Ma == ma)) return;
        db.ThongBaoDaDoc.Add(new ThongBaoDaDoc { MaThongBao = ma, TenDangNhap = nd.Ten, Luc = DateTimeOffset.UtcNow });
        try { await db.SaveChangesAsync(); kho.BaoDaDoc(); } catch (DbUpdateException) { }
    }

    /// <summary>Người đăng (Admin) xem ai đã đọc.</summary>
    public async Task<List<ThongBaoDaDoc>> LayNguoiDaDocAsync(Guid ma)
    {
        await KiemTraAdmin();
        if (kho.Csdl is null) return [];
        await using var db = await kho.Csdl.CreateDbContextAsync();
        return (await db.ThongBaoDaDoc.AsNoTracking().Where(x => x.MaThongBao == ma).ToListAsync()).OrderByDescending(x => x.Luc).ToList();
    }
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
        var loai = CacLoaiVanBan.Contains(banNhap.LoaiVanBan) ? banNhap.LoaiVanBan : throw new ArgumentException("Loại văn bản không hợp lệ.");
        var mucDo = CacMucDo.Contains(banNhap.MucDo) ? banNhap.MucDo : throw new ArgumentException("Mức độ không hợp lệ.");
        var soKyHieu = (banNhap.SoKyHieu ?? "").Trim();
        var coQuan = string.IsNullOrWhiteSpace(banNhap.CoQuanBanHanh) ? "Học viện An ninh nhân dân" : banNhap.CoQuanBanHanh.Trim();
        var nguoiKy = (banNhap.NguoiKy ?? "").Trim();
        var trichYeu = (banNhap.TrichYeu ?? "").Trim();
        if (soKyHieu.Length > 40 || coQuan.Length > 150 || nguoiKy.Length > 100 || trichYeu.Length > 300)
            throw new ArgumentException("Số/ký hiệu tối đa 40 ký tự, cơ quan 150, người ký 100, trích yếu 300.");
        if (soKyHieu.Length > 0) lock (kho.Khoa)
            if (kho.DanhSach.Any(x => x.Ma != banNhap.Ma && string.Equals(x.SoKyHieu, soKyHieu, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Số/ký hiệu {soKyHieu} đã được dùng cho văn bản khác.");
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
            bai.LoaiVanBan = loai; bai.MucDo = mucDo; bai.SoKyHieu = soKyHieu; bai.CoQuanBanHanh = coQuan; bai.NguoiKy = nguoiKy;
            bai.TrichYeu = trichYeu; bai.HieuLucDen = banNhap.HieuLucDen; bai.Ghim = banNhap.Ghim;
            if (xoaAnh) bai.Anh = null;
            if (anhMoi is not null) bai.Anh = anh;
            ma = bai.Ma;
        }
        kho.ThongBaoThayDoi(u.Identity?.Name);
        return ma;
    }
    /// <summary>
    /// Đăng hoặc cập nhật một thông báo do nghiệp vụ khác phát sinh (ví dụ Phòng đào tạo gửi thông báo thi tới một lớp).
    /// Dịch vụ gọi đã kiểm quyền; ở đây chỉ kiểm độ dài, loại, mức độ và phạm vi gửi.
    /// </summary>
    internal Guid DangTuNghiepVu(ThongBao banNhap, string tacGia)
    {
        if (banNhap.TieuDe.Trim().Length is 0 or > 200 || banNhap.NoiDung.Trim().Length is 0 or > 20000) throw new ArgumentException("Tiêu đề hoặc nội dung thông báo không hợp lệ.");
        if (!CacLoaiVanBan.Contains(banNhap.LoaiVanBan) || !CacMucDo.Contains(banNhap.MucDo)) throw new ArgumentException("Loại hoặc mức độ văn bản không hợp lệ.");
        Guid ma;
        lock (kho.Khoa)
        {
            var bai = banNhap.Ma == Guid.Empty ? null : kho.DanhSach.SingleOrDefault(x => x.Ma == banNhap.Ma);
            if (bai is null)
            {
                bai = new ThongBao { Ma = Guid.NewGuid(), TacGia = tacGia, DangLuc = DateTimeOffset.UtcNow };
                kho.DanhSach.Add(bai);
            }
            else bai.SuaLuc = DateTimeOffset.UtcNow;
            bai.TieuDe = banNhap.TieuDe.Trim(); bai.NoiDung = banNhap.NoiDung.Trim(); bai.Tags = banNhap.Tags.Distinct().ToList();
            bai.LoaiVanBan = banNhap.LoaiVanBan; bai.MucDo = banNhap.MucDo; bai.SoKyHieu = string.IsNullOrWhiteSpace(bai.SoKyHieu) ? banNhap.SoKyHieu : bai.SoKyHieu;
            bai.CoQuanBanHanh = banNhap.CoQuanBanHanh; bai.NguoiKy = banNhap.NguoiKy; bai.TrichYeu = banNhap.TrichYeu; bai.HieuLucDen = banNhap.HieuLucDen; bai.Anh = banNhap.Anh ?? bai.Anh;
            ma = bai.Ma;
        }
        kho.ThongBaoThayDoi(tacGia);
        return ma;
    }

    public async Task XoaAsync(Guid ma)
    {
        var u = await KiemTraAdmin();
        lock (kho.Khoa)
            if (kho.DanhSach.RemoveAll(x => x.Ma == ma) == 0) throw new ArgumentException("Thông báo đã bị xóa.");
        kho.ThongBaoThayDoi(u.Identity?.Name);
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
