using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Sinh dữ liệu đào tạo mẫu theo công thức cố định: 24 chuyên đề (2 mỗi khoa), phòng học,
/// chuyên môn giảng dạy, lịch 4 tuần cho các lớp khoá D58 và K6, điểm danh các buổi đã qua và một phần điểm kiểm tra.
/// Mọi tên chuyên đề, phòng, lịch đều là dữ liệu giả lập.
/// </summary>
public static class KhoiTaoDaoTaoMau
{
    /// <summary>Cán bộ dùng cho tài khoản mẫu "giangvien" (luôn được xếp dạy lớp B1D58).</summary>
    public const string MaGiangVienMau = "CB006";
    /// <summary>Lớp năm thứ tư có lịch học hiện hành; tài khoản mẫu "hocvien" thuộc lớp này để có đủ các năm học, học kỳ.</summary>
    public const string LopNamCuoiMau = "B1D55";

    private static readonly (string Khoa, string Ten1, string Ten2)[] ChuyenDeTheoKhoa =
    [
        ("KH-01", "Triết học Mác – Lênin", "Tư tưởng Hồ Chí Minh"),
        ("KH-02", "Võ thuật Công an nhân dân", "Bắn súng quân dụng"),
        ("KH-03", "Nghiệp vụ cơ bản Công an nhân dân", "Kỹ năng điều tra cơ bản"),
        ("KH-04", "Trinh sát an ninh nội địa", "Công tác phản gián"),
        ("KH-05", "Điều tra hình sự", "Kỹ thuật hình sự"),
        ("KH-06", "Quản lý xuất nhập cảnh", "Kiểm soát xuất nhập cảnh"),
        ("KH-07", "An ninh mạng", "Phòng, chống tội phạm công nghệ cao"),
        ("KH-08", "Bảo vệ an ninh kinh tế", "An ninh văn hoá – tư tưởng"),
        ("KH-09", "Công nghệ thông tin trong Công an nhân dân", "Cơ sở dữ liệu nghiệp vụ"),
        ("KH-10", "An toàn thông tin", "Mật mã ứng dụng"),
        ("KH-11", "Luật Hình sự", "Luật Tố tụng hình sự"),
        ("KH-12", "Tiếng Anh chuyên ngành", "Tiếng Trung cơ sở"),
    ];

    // Chương trình mẫu: phần chung + phần chuyên ngành (mã CD-xx theo thứ tự ở trên).
    private static readonly string[] PhanChung = ["CD-01", "CD-02", "CD-03", "CD-05", "CD-21", "CD-23"];
    private static string[] PhanChuyenNganh(string chuyenNganh) => chuyenNganh switch
    {
        "Trinh sát an ninh" => ["CD-07", "CD-08"],
        "Điều tra hình sự" or "Điều tra" => ["CD-09", "CD-10"],
        "Quản lý xuất nhập cảnh" => ["CD-11", "CD-12"],
        "ANM & PCTPCNC" => ["CD-13", "CD-14"],
        _ => ["CD-17", "CD-18"]
    };

    // Một buổi mỗi ngày từ thứ Hai đến thứ Sáu: sáng tiết 1–4 hoặc chiều tiết 6–9.
    private static readonly int[] TietTheoThu = [1, 6, 1, 6, 1];

    public static void Tao(CoSoDuLieuNghiepVu db, IReadOnlyList<Khoa> cacKhoa, IReadOnlyList<CanBoGiangVien> cacCanBo,
        IReadOnlyList<LopDaoTao> cacLop, IReadOnlyList<HocVien> cacHocVien, DateTime homNay)
    {
        // 1. Chuyên đề
        var chuyenDe = new List<ChuyenDe>();
        var so = 1;
        foreach (var (khoa, ten1, ten2) in ChuyenDeTheoKhoa)
        {
            if (!cacKhoa.Any(k => k.MaKhoa == khoa)) { so += 2; continue; }
            chuyenDe.Add(new() { MaChuyenDe = $"CD-{so++:00}", TenChuyenDe = ten1, MaKhoa = khoa, SoTiet = 45, MoTa = "Chuyên đề mẫu, nội dung giả lập." });
            chuyenDe.Add(new() { MaChuyenDe = $"CD-{so++:00}", TenChuyenDe = ten2, MaKhoa = khoa, SoTiet = 30, MoTa = "Chuyên đề mẫu, nội dung giả lập." });
        }
        db.ChuyenDe.AddRange(chuyenDe);

        // 2. Chuyên môn giảng dạy: mọi cán bộ của khoa đủ điều kiện dạy các chuyên đề của khoa.
        var chuyenMon = chuyenDe.SelectMany(cd => cacCanBo.Where(c => c.MaKhoa == cd.MaKhoa)
            .Select(c => new ChuyenMonGiangDay { MaCanBo = c.MaCanBo, MaChuyenDe = cd.MaChuyenDe })).ToList();
        db.ChuyenMonGiangDay.AddRange(chuyenMon);

        // 3. Phòng: 18 giảng đường, 2 phòng máy, phòng thi, sân tập, phòng hội thảo nhỏ và một phòng đang bảo trì.
        var phong = new List<Phong>();
        for (var i = 1; i <= 18; i++)
            phong.Add(new() { MaPhong = $"GD-{100 * (1 + (i - 1) / 6) + (i - 1) % 6 + 1}", TenPhong = $"Giảng đường {100 * (1 + (i - 1) / 6) + (i - 1) % 6 + 1}", LoaiPhong = "Giảng đường", SucChua = i % 3 == 0 ? 120 : i % 3 == 1 ? 60 : 45 });
        phong.Add(new() { MaPhong = "PM-01", TenPhong = "Phòng máy 01", LoaiPhong = "Phòng máy", SucChua = 45 });
        phong.Add(new() { MaPhong = "PM-02", TenPhong = "Phòng máy 02", LoaiPhong = "Phòng máy", SucChua = 45 });
        phong.Add(new() { MaPhong = "PT-01", TenPhong = "Phòng thi lớn", LoaiPhong = "Phòng thi", SucChua = 120 });
        phong.Add(new() { MaPhong = "ST-01", TenPhong = "Sân tập võ thuật", LoaiPhong = "Sân tập", SucChua = 200 });
        phong.Add(new() { MaPhong = "HT-01", TenPhong = "Phòng hội thảo nhỏ", LoaiPhong = "Giảng đường", SucChua = 30 });
        phong.Add(new() { MaPhong = "GD-401", TenPhong = "Giảng đường 401", LoaiPhong = "Giảng đường", SucChua = 80, DangBaoTri = true });
        db.Phong.AddRange(phong);
        db.SaveChanges();

        // 4. Lịch 4 tuần (tuần trước, tuần này, hai tuần sau) cho lớp D58 và K6.
        var lopCoLich = cacLop.Where(l => l.KhoaHoc is "D58" or "K6").ToList();
        var siSo = cacHocVien.GroupBy(h => h.MaLop).ToDictionary(g => g.Key, g => g.Count());
        var giangVienTheoCd = chuyenMon.GroupBy(x => x.MaChuyenDe).ToDictionary(g => g.Key, g => g.Select(x => x.MaCanBo).ToList());
        var phongDung = phong.Where(p => !p.DangBaoTri && p.LoaiPhong is "Giảng đường" or "Phòng máy").OrderBy(p => p.SucChua).ToList();
        var soBuoiCuaGv = new Dictionary<string, int>();
        var buoiHoc = new List<BuoiHoc>();
        var dauTuan = QuyTacHocVu.DauTuan(homNay).AddDays(-7);
        for (var tuan = 0; tuan < 4; tuan++)
        for (var thu = 0; thu < 5; thu++)
        {
            var ngay = dauTuan.AddDays(tuan * 7 + thu);
            var gvBan = new HashSet<string>();
            var phongBan = new HashSet<string>();
            for (var i = 0; i < lopCoLich.Count; i++)
            {
                var lop = lopCoLich[i];
                var chuongTrinh = PhanChung.Concat(PhanChuyenNganh(lop.ChuyenNganh)).Where(giangVienTheoCd.ContainsKey).ToArray();
                var maCd = chuongTrinh[(i + thu + tuan * 5) % chuongTrinh.Length];
                // Ưu tiên giảng viên mẫu, sau đó người đang ít buổi nhất để chia đều.
                var gv = giangVienTheoCd[maCd].Where(m => !gvBan.Contains(m))
                    .OrderBy(m => m == MaGiangVienMau ? 0 : 1).ThenBy(m => soBuoiCuaGv.GetValueOrDefault(m)).ThenBy(m => m).FirstOrDefault();
                var canPhong = maCd == "CD-03" ? phong.First(p => p.MaPhong == "ST-01") : phongDung.FirstOrDefault(p => !phongBan.Contains(p.MaPhong) && p.SucChua >= siSo.GetValueOrDefault(lop.MaLop));
                if (gv is null || canPhong is null || phongBan.Contains(canPhong.MaPhong)) continue;
                gvBan.Add(gv); phongBan.Add(canPhong.MaPhong);
                soBuoiCuaGv[gv] = soBuoiCuaGv.GetValueOrDefault(gv) + 1;
                buoiHoc.Add(new() { MaLop = lop.MaLop, MaChuyenDe = maCd, MaCanBo = gv, MaPhong = canPhong.MaPhong, Ngay = ngay, TietBatDau = TietTheoThu[thu], SoTiet = 4 });
            }
        }
        db.BuoiHoc.AddRange(buoiHoc);
        db.SaveChanges();

        // 5. Điểm danh cho các buổi đã qua; phần lớn có mặt, một số đi muộn hoặc vắng.
        var hocVienTheoLop = cacHocVien.GroupBy(h => h.MaLop).ToDictionary(g => g.Key, g => g.Select(h => h.MaHocVien).ToList());
        var diemDanh = new List<DiemDanh>();
        foreach (var b in buoiHoc.Where(b => b.Ngay.Date < homNay.Date))
        foreach (var maHv in hocVienTheoLop.GetValueOrDefault(b.MaLop, []))
        {
            var h = (Bam(maHv) + b.Id * 7) % 100;
            diemDanh.Add(new()
            {
                BuoiHocId = b.Id, MaHocVien = maHv, NguoiGhi = b.MaCanBo,
                TrangThai = h < 88 ? TrangThaiDiemDanh.CoMat : h < 93 ? TrangThaiDiemDanh.DiMuon : h < 97 ? TrangThaiDiemDanh.VangCoPhep : TrangThaiDiemDanh.VangKhongPhep,
                ThoiDiem = new DateTimeOffset(QuyTacHocVu.BatDau(b)),
            });
        }
        db.DiemDanh.AddRange(diemDanh);

        // 6. Điểm kiểm tra cho các chuyên đề lớp đã học; khoảng 15% để trống để thấy trường hợp "thiếu điểm".
        var ketQua = new List<KetQua>();
        foreach (var nhom in buoiHoc.Where(b => b.Ngay.Date < homNay.Date).Select(b => (b.MaLop, b.MaChuyenDe)).Distinct())
        foreach (var maHv in hocVienTheoLop.GetValueOrDefault(nhom.MaLop, []))
        {
            var h = Bam(maHv + nhom.MaChuyenDe);
            if (h % 100 < 15) continue;
            ketQua.Add(new() { MaHocVien = maHv, MaChuyenDe = nhom.MaChuyenDe, DiemKiemTra = 4.0 + h % 60 / 10.0, NguoiNhap = "Dữ liệu mẫu", CapNhatLuc = DateTimeOffset.Now });
        }
        db.KetQua.AddRange(ketQua);
        db.SaveChanges();
    }

    /// <summary>
    /// Bổ sung lịch học hiện hành cho lớp năm cuối mẫu (B1D55) nếu chưa có — chạy được trên CSDL cũ.
    /// Xếp vào ca đối diện ca chung của các lớp khác trong cùng ngày (sáng ↔ chiều) và vẫn kiểm tra trùng giảng viên, phòng.
    /// Sinh kèm phân công, điểm danh và điểm kiểm tra các buổi đã qua, sổ đầu bài mẫu.
    /// </summary>
    public static void BoSungLichLopNamCuoi(CoSoDuLieuNghiepVu db, IReadOnlyList<LopDaoTao> cacLop, IReadOnlyList<HocVien> cacHocVien, DateTime homNay)
    {
        var lop = cacLop.FirstOrDefault(l => l.MaLop == LopNamCuoiMau);
        if (lop is null || !db.ChuyenDe.Any() || db.BuoiHoc.Any(b => b.MaLop == LopNamCuoiMau)) return;
        var hocVien = cacHocVien.Where(h => h.MaLop == lop.MaLop).Select(h => h.MaHocVien).ToList();
        var chuyenMon = db.ChuyenMonGiangDay.AsNoTracking().ToList().GroupBy(x => x.MaChuyenDe).ToDictionary(g => g.Key, g => g.Select(x => x.MaCanBo).ToList());
        var chuongTrinh = PhanChung.Concat(PhanChuyenNganh(lop.ChuyenNganh)).Where(chuyenMon.ContainsKey).ToArray();
        if (chuongTrinh.Length == 0) return;
        var phong = db.Phong.AsNoTracking().Where(p => !p.DangBaoTri && p.LoaiPhong == "Giảng đường" && p.SucChua >= hocVien.Count).OrderBy(p => p.SucChua).ToList();
        var daCo = db.BuoiHoc.AsNoTracking().Select(b => new { b.Ngay, b.TietBatDau, b.MaCanBo, b.MaPhong }).ToList();
        var soBuoiGv = daCo.GroupBy(b => b.MaCanBo).ToDictionary(g => g.Key, g => g.Count());
        var dauTuan = daCo.Count > 0 ? QuyTacHocVu.DauTuan(daCo.Min(b => b.Ngay)) : QuyTacHocVu.DauTuan(homNay).AddDays(-7);
        var moi = new List<BuoiHoc>();
        for (var tuan = 0; tuan < 4; tuan++)
        for (var thu = 0; thu < 5; thu++)
        {
            var ngay = dauTuan.AddDays(tuan * 7 + thu);
            var tiet = TietTheoThu[thu] == 1 ? 6 : 1;
            var cd = chuongTrinh[(thu + tuan * 5 + 2) % chuongTrinh.Length];
            bool Ban(string? gv, string? ph) => daCo.Any(b => b.Ngay == ngay && b.TietBatDau == tiet && (b.MaCanBo == gv || b.MaPhong == ph));
            var gvChon = chuyenMon[cd].Where(m => !Ban(m, null)).OrderBy(m => m == MaGiangVienMau ? 0 : 1).ThenBy(m => soBuoiGv.GetValueOrDefault(m)).ThenBy(m => m).FirstOrDefault();
            var phChon = cd == "CD-03" ? "ST-01" : phong.FirstOrDefault(p => !Ban(null, p.MaPhong))?.MaPhong;
            if (gvChon is null || phChon is null) continue;
            soBuoiGv[gvChon] = soBuoiGv.GetValueOrDefault(gvChon) + 1;
            daCo.Add(new { Ngay = ngay, TietBatDau = tiet, MaCanBo = gvChon, MaPhong = phChon });
            moi.Add(new BuoiHoc { MaLop = lop.MaLop, MaChuyenDe = cd, MaCanBo = gvChon, MaPhong = phChon, Ngay = ngay, TietBatDau = tiet, SoTiet = 4 });
        }
        db.BoQuaNhatKy = true;
        db.BuoiHoc.AddRange(moi);
        db.SaveChanges();
        foreach (var cap in moi.GroupBy(b => b.MaChuyenDe))
            if (!db.PhanCong.Any(p => p.MaLop == lop.MaLop && p.MaChuyenDe == cap.Key))
                db.PhanCong.Add(new PhanCongGiangDay { MaLop = lop.MaLop, MaChuyenDe = cap.Key, MaCanBo = cap.GroupBy(b => b.MaCanBo).OrderByDescending(g => g.Count()).First().Key, GhiChu = "Phân công ban đầu (dữ liệu mẫu)" });
        var tenCd = db.ChuyenDe.AsNoTracking().ToDictionary(c => c.MaChuyenDe, c => c.TenChuyenDe);
        var thuTu = new Dictionary<string, int>();
        foreach (var b in moi.Where(b => b.Ngay.Date < homNay.Date).OrderBy(b => b.Ngay))
        {
            foreach (var maHv in hocVien)
            {
                var h = (Bam(maHv) + b.Id * 7) % 100;
                db.DiemDanh.Add(new DiemDanh
                {
                    BuoiHocId = b.Id, MaHocVien = maHv, NguoiGhi = b.MaCanBo, ThoiDiem = new DateTimeOffset(QuyTacHocVu.BatDau(b)),
                    TrangThai = h < 88 ? TrangThaiDiemDanh.CoMat : h < 93 ? TrangThaiDiemDanh.DiMuon : h < 97 ? TrangThaiDiemDanh.VangCoPhep : TrangThaiDiemDanh.VangKhongPhep,
                });
            }
            thuTu[b.MaChuyenDe] = thuTu.GetValueOrDefault(b.MaChuyenDe) + 1;
            db.SoDauBai.Add(new SoDauBai { BuoiHocId = b.Id, NoiDung = $"Bài {thuTu[b.MaChuyenDe]}: {tenCd.GetValueOrDefault(b.MaChuyenDe)} – nội dung giảng dạy mẫu.", NhanXet = "Lớp nghiêm túc, chủ động trao đổi.", XepLoaiTiet = b.Id % 4 == 0 ? "B" : "A", NguoiGhi = b.MaCanBo, ThoiDiem = new DateTimeOffset(QuyTacHocVu.KetThuc(b)) });
        }
        foreach (var cd in moi.Where(b => b.Ngay.Date < homNay.Date).Select(b => b.MaChuyenDe).Distinct())
        foreach (var maHv in hocVien)
        {
            var h = Bam(maHv + cd + "nam4");
            if (h % 100 < 15 || db.KetQua.Any(k => k.MaHocVien == maHv && k.MaChuyenDe == cd)) continue;
            db.KetQua.Add(new KetQua { MaHocVien = maHv, MaChuyenDe = cd, DiemKiemTra = 5.0 + h % 50 / 10.0, NguoiNhap = "Dữ liệu mẫu", CapNhatLuc = DateTimeOffset.Now });
        }
        db.SaveChanges();
        db.BoQuaNhatKy = false;
    }

    /// <summary>
    /// Kết quả các học kỳ đã qua cho các khoá cũ (tối đa 8 học kỳ, 3 chuyên đề mỗi học kỳ), để học viên
    /// xem được bảng điểm theo năm học, học kỳ. Học kỳ hiện tại không sinh ở đây (tính từ điểm danh, điểm).
    /// </summary>
    public static void TaoKetQuaLuuTru(CoSoDuLieuNghiepVu db, IReadOnlyList<LopDaoTao> cacLop, IReadOnlyList<HocVien> cacHocVien, DateTime homNay)
    {
        if (db.KetQuaLuuTru.Any() || !db.ChuyenDe.Any()) return;
        var (namHocNay, hkNay) = QuyTacHocVu.NamHocHocKy(homNay);
        var namDauNay = int.Parse(namHocNay[..4]);
        var hocVienTheoLop = cacHocVien.GroupBy(h => h.MaLop).ToDictionary(g => g.Key, g => g.Select(h => h.MaHocVien).ToList());
        var ds = new List<KetQua_TamThoi>();
        foreach (var lop in cacLop)
        {
            if (!int.TryParse(lop.NienKhoa.Split('–')[0], out var namNhap)) continue;
            var chuongTrinh = PhanChung.Concat(PhanChuyenNganh(lop.ChuyenNganh)).ToArray();
            var thuTu = 0;
            for (var nam = namNhap; nam <= namDauNay && thuTu < 8; nam++)
            for (var hk = 1; hk <= 2 && thuTu < 8; hk++)
            {
                if (nam == namDauNay && hk >= hkNay) break;
                var namHoc = $"{nam}–{nam + 1}";
                var ngayThi = hk == 1 ? new DateTime(nam + 1, 1, 10) : new DateTime(nam + 1, 6, 15);
                for (var k = 0; k < 3; k++)
                {
                    var cd = chuongTrinh[(thuTu * 3 + k) % chuongTrinh.Length];
                    foreach (var maHv in hocVienTheoLop.GetValueOrDefault(lop.MaLop, []))
                    {
                        var h = Bam(maHv + namHoc + hk + cd);
                        var cc = Math.Round(6 + h % 41 / 10.0, 1);
                        double? kt = h % 100 < 3 ? null : Math.Round(4 + h % 57 / 10.0, 1);
                        var (tk, _) = QuyTacHocVu.TinhTongKet(cc, kt);
                        ds.Add(new(maHv, cd, namHoc, hk, cc, kt, tk, ngayThi.AddDays(k)));
                    }
                }
                thuTu++;
            }
        }
        db.BoQuaNhatKy = true;
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        foreach (var lo in ds.Chunk(5000))
        {
            db.KetQuaLuuTru.AddRange(lo.Select(x => new KetQuaLuuTru { MaHocVien = x.MaHv, MaChuyenDe = x.Cd, NamHoc = x.NamHoc, HocKy = x.Hk, ChuyenCan = x.Cc, DiemKiemTra = x.Kt, TongKet = x.Tk, NgayThi = x.NgayThi }));
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }
        db.ChangeTracker.AutoDetectChangesEnabled = true;
        db.BoQuaNhatKy = false;
    }
    private sealed record KetQua_TamThoi(string MaHv, string Cd, string NamHoc, int Hk, double Cc, double? Kt, double? Tk, DateTime NgayThi);

    // Băm ổn định (không dùng string.GetHashCode vì thay đổi giữa các lần chạy).
    private static int Bam(string s)
    {
        unchecked
        {
            var h = 17;
            foreach (var c in s) h = h * 31 + c;
            return Math.Abs(h % 1_000_000);
        }
    }
}
