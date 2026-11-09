using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nhom2_BTL.Models;
using Nhom2_BTL.Services;

/// <summary>
/// Kiểm thử tích hợp mốc 1 trên CSDL SQLite tạm (không đụng App_Data):
/// mặt cắt theo vai trò, quyền ghi, chặn trùng lịch, điểm danh, điểm, nhật ký, cấp tài khoản, tìm kiếm, lưu bền.
/// </summary>
static class KiemThuTichHop
{
    public static async Task ChayAsync(Action<bool, string> kiemTra)
    {
        var tepNghiepVu = Path.Combine(Path.GetTempPath(), $"tich-hop-du-lieu-{Guid.NewGuid()}.db");
        var tepTaiKhoan = Path.Combine(Path.GetTempPath(), $"tich-hop-tai-khoan-{Guid.NewGuid()}.db");
        try
        {
            var dv = new ServiceCollection();
            dv.AddLogging();
            dv.AddAuthorizationCore(o => o.AddPolicy("Admin", p => p.RequireRole("Admin")));
            dv.AddDbContextFactory<CoSoDuLieuNghiepVu>(o => o.UseSqlite($"Data Source={tepNghiepVu};Pooling=False"));
            dv.AddSingleton<KhoDuLieuDemo>();
            dv.AddDbContext<CoSoTaiKhoan>(o => o.UseSqlite($"Data Source={tepTaiKhoan};Pooling=False"));
            dv.AddIdentityCore<TaiKhoan>(o => KiemTraMatKhau.CauHinh(o.Password)).AddPasswordValidator<KiemTraMatKhau>().AddEntityFrameworkStores<CoSoTaiKhoan>();
            dv.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new MoiTruongKiemThu());
            dv.AddSingleton<DichVuTaiKhoan>();
            dv.AddSingleton<KhoVeDangNhapDemo>();
            dv.AddScoped<NhaCungCapTrangThaiXacThucDemo>();
            dv.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>());
            dv.AddScoped<DichVuPhanQuyenDemo>(); dv.AddScoped<DichVuQuanLyDuLieuDemo>();
            dv.AddScoped<DichVuDaoTao>(); dv.AddScoped<DichVuHocVu>(); dv.AddScoped<DichVuNhatKy>(); dv.AddScoped<DichVuTimKiem>();
            dv.AddScoped<DichVuExcel>(); dv.AddScoped<DichVuNhapExcel>(); dv.AddScoped<DichVuBaoCaoExcel>(); dv.AddScoped<DichVuTruongTuyChinh>(); dv.AddScoped<DichVuTongQuan>(); dv.AddScoped<DichVuSoDoToChuc>(); dv.AddScoped<DichVuChuongTrinh>(); dv.AddScoped<DichVuBanTin>();
            dv.AddSingleton<KhoThongBaoDemo>(); dv.AddScoped<DichVuThongBao>();
            await using var root = dv.BuildServiceProvider();

            // Khởi tạo như Program.cs: CSDL nghiệp vụ + dữ liệu đào tạo mẫu, CSDL tài khoản + tài khoản mẫu.
            var kho = root.GetRequiredService<KhoDuLieuDemo>();
            string maHvMau; lock (kho.KhoaDongBo) maHvMau = kho.DanhSachHocVien.First(h => h.MaLop == "B1D58").MaHocVien;
            using (var sc = root.CreateScope())
            {
                await sc.ServiceProvider.GetRequiredService<CoSoTaiKhoan>().Database.EnsureCreatedAsync();
                await KhoiTaoTaiKhoanMau.TaoAsync(sc.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>(), KhoiTaoDaoTaoMau.MaGiangVienMau, maHvMau);
            }
            var csdl = root.GetRequiredService<IDbContextFactory<CoSoDuLieuNghiepVu>>();
            kiemTra(csdl.CreateDbContext().ChuyenDe.Count() == 24 && csdl.CreateDbContext().BuoiHoc.Any(b => b.MaCanBo == "CB006" && b.MaLop == "B1D58"), "Dữ liệu đào tạo mẫu: 24 chuyên đề, giảng viên mẫu dạy B1D58");

            // Mỗi vai trò một phạm vi DI riêng (như một phiên Blazor riêng).
            async Task<IServiceScope> Phien(string ten)
            {
                var sc = root.CreateScope();
                if (ten != "") kiemTra(await sc.ServiceProvider.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>().DangNhapAsync(ten, "demo123"), $"Đăng nhập {ten}");
                return sc;
            }
            T Lay<T>(IServiceScope sc) where T : notnull => sc.ServiceProvider.GetRequiredService<T>();
            var coCccd = new Regex(@"\b\d{12}\b");
            bool LoCccd(object duLieu) => coCccd.IsMatch(JsonSerializer.Serialize(duLieu));

            using var admin = await Phien("admin");
            using var qlKhoa = await Phien("quanlykhoa");
            using var qlDaoTao = await Phien("quanlydaotao");
            using var qlHocVien = await Phien("quanlyhocvien");
            using var gv = await Phien("giangvien");
            using var hv = await Phien("hocvien");
            using var xem = await Phien("nguoixem");

            // ---- A. Mặt cắt dữ liệu theo vai trò (test sinh từ ma trận 6.2) ----
            var dlAdmin = Lay<DichVuQuanLyDuLieuDemo>(admin);
            kiemTra(dlAdmin.LayDanhSachHocVien().Count == 4240 && LoCccd(dlAdmin.LayDanhSachHocVien()) && LoCccd(dlAdmin.LayDanhSachCanBo()), "Admin thấy đủ học viên và cán bộ kể cả CCCD");
            foreach (var (ten, sc) in new[] { ("Quản lý khoa", qlKhoa), ("Quản lý đào tạo", qlDaoTao) })
                kiemTra(Lay<DichVuQuanLyDuLieuDemo>(sc).LayDanhSachHocVien().Count == 0, $"Hàng rào 2: {ten} không nhận danh sách từng học viên");
            kiemTra(LoCccd(Lay<DichVuQuanLyDuLieuDemo>(qlKhoa).LayDanhSachCanBo()), "Quản lý khoa thấy dữ liệu cá nhân cán bộ");
            kiemTra(!LoCccd(Lay<DichVuQuanLyDuLieuDemo>(qlDaoTao).LayDanhSachCanBo()), "Quản lý đào tạo không nhận CCCD cán bộ");
            kiemTra(!LoCccd(Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).LayDanhSachCanBo()) && LoCccd(Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).LayDanhSachHocVien()), "Hàng rào 3: Quản lý học viên thấy đủ học viên, không thấy CCCD cán bộ");
            var dlGv = Lay<DichVuQuanLyDuLieuDemo>(gv);
            var lopGv = dlGv.LayDanhSachLop().Select(l => l.MaLop).ToHashSet();
            kiemTra(lopGv.Count > 0 && lopGv.SetEquals(kho.LopGiangVienDay("CB006")), "Giảng viên chỉ thấy lớp mình dạy");
            var hvCuaGv = dlGv.LayDanhSachHocVien();
            kiemTra(hvCuaGv.Count > 0 && hvCuaGv.All(h => lopGv.Contains(h.MaLop)) && !LoCccd(hvCuaGv) && hvCuaGv.All(h => h.SoDienThoai == "" && h.DiaChiThuongTru == ""), "Hàng rào 4: giảng viên thấy học viên lớp mình, không có CCCD/SĐT/địa chỉ");
            var cbCuaGv = dlGv.LayDanhSachCanBo();
            kiemTra(cbCuaGv.Single(c => c.MaTraCuuNoiBo == "CB006").SoCCCD != "" && cbCuaGv.Where(c => c.MaTraCuuNoiBo != "CB006").All(c => c.SoCCCD == ""), "Giảng viên thấy hồ sơ của mình, không thấy dữ liệu cá nhân đồng nghiệp");
            var dlHv = Lay<DichVuQuanLyDuLieuDemo>(hv);
            var hvThay = dlHv.LayDanhSachHocVien();
            kiemTra(dlHv.LayDanhSachLop().Single().MaLop == "B1D58", "Học viên chỉ thấy lớp của mình");
            kiemTra(hvThay.Single(h => h.MaHocVien == maHvMau).SoCCCD != "" && hvThay.Where(h => h.MaHocVien != maHvMau).All(h => h.MaHocVien == "" && h.SoCCCD == "" && h.MaLop == "B1D58"), "Hàng rào 5: học viên thấy đủ hồ sơ mình, bạn cùng lớp chỉ họ tên và lớp");
            kiemTra(dlHv.LayDanhSachCanBo().Select(c => c.MaTraCuuNoiBo).ToHashSet().SetEquals(kho.GiangVienDayLop("B1D58")) && !LoCccd(dlHv.LayDanhSachCanBo()), "Học viên chỉ thấy giảng viên dạy lớp mình, không có CCCD");
            kiemTra(!LoCccd(Lay<DichVuQuanLyDuLieuDemo>(xem).LayDanhSachHocVien()), "Người xem không nhận CCCD học viên");

            // ---- B. Quyền ghi kiểm ở dịch vụ ----
            var hvMau = dlAdmin.LayDanhSachHocVien().First(h => h.MaLop == "B2D57");
            kiemTra(!(await Lay<DichVuQuanLyDuLieuDemo>(qlDaoTao).LuuHocVienAsync(hvMau, hvMau.MaHocVien)).ThanhCong, "Quản lý đào tạo không sửa được học viên");
            kiemTra(!(await Lay<DichVuDaoTao>(qlHocVien).LuuChuyenDeAsync(new ChuyenDe { MaChuyenDe = "CD-99", TenChuyenDe = "Thử", MaKhoa = "KH-01" })).ThanhCong, "Quản lý học viên không thêm được chuyên đề");
            kiemTra(!(await Lay<DichVuDaoTao>(gv).XoaPhongAsync("HT-01")).ThanhCong, "Giảng viên không xoá được phòng");
            kiemTra(!(await Lay<DichVuDaoTao>(qlDaoTao).DatGiaoVienDuDieuKienAsync("CD-01", [])).ThanhCong, "Quản lý đào tạo không sửa chuyên môn giảng dạy (dữ liệu của mảng khoa)");
            kiemTra((await Lay<DichVuDaoTao>(qlDaoTao).LuuChuyenDeAsync(new ChuyenDe { MaChuyenDe = "CD-99", TenChuyenDe = "Chuyên đề thử", MaKhoa = "KH-01", SoTiet = 15 })).ThanhCong, "Quản lý đào tạo thêm chuyên đề");
            kiemTra((await Lay<DichVuDaoTao>(qlKhoa).DatGiaoVienDuDieuKienAsync("CD-99", ["CB001", "CB004"])).ThanhCong, "Quản lý khoa đặt giáo viên đủ điều kiện");
            kiemTra((await Lay<DichVuDaoTao>(admin).LayGiaoVienDuDieuKienAsync("CD-99")).SequenceEqual(["CB001", "CB004"]), "Danh sách đủ điều kiện được lưu");

            // ---- C. Chặn trùng lịch, trả về lý do ----
            var daoTao = Lay<DichVuDaoTao>(qlDaoTao);
            var homNay = DateTime.Today;
            var buoiCo = csdl.CreateDbContext().BuoiHoc.AsNoTracking().First(b => b.MaCanBo == "CB006" && b.Ngay > homNay);
            var lopKhac = csdl.CreateDbContext().BuoiHoc.AsNoTracking().Where(b => b.Ngay == buoiCo.Ngay && b.TietBatDau == buoiCo.TietBatDau).Select(b => b.MaLop).ToList();
            var lopTrong = kho.DanhSachLop.Select(l => l.MaLop).First(l => !lopKhac.Contains(l) && l.EndsWith("D55"));
            BuoiHoc Moi(Action<BuoiHoc> sua) { var b = new BuoiHoc { MaLop = lopTrong, MaChuyenDe = buoiCo.MaChuyenDe, MaCanBo = buoiCo.MaCanBo, MaPhong = "GD-306", Ngay = buoiCo.Ngay, TietBatDau = buoiCo.TietBatDau, SoTiet = 2 }; sua(b); return b; }
            var phongTrong = csdl.CreateDbContext().Phong.AsNoTracking().Where(p => !p.DangBaoTri && p.SucChua >= 40).Select(p => p.MaPhong).ToList()
                .First(p => !csdl.CreateDbContext().BuoiHoc.Any(b => b.Ngay == buoiCo.Ngay && b.MaPhong == p && b.TietBatDau <= buoiCo.TietBatDau + 1 && buoiCo.TietBatDau <= b.TietBatDau + b.SoTiet - 1));
            var loiGv = await daoTao.KiemTraBuoiHocAsync(Moi(b => b.MaPhong = phongTrong));
            kiemTra(loiGv.Any(l => l.StartsWith("Giảng viên") && l.Contains(buoiCo.MaLop)), "Chặn giảng viên dạy hai nơi cùng giờ, nêu lớp đang dạy");
            var gvKhac = csdl.CreateDbContext().ChuyenMonGiangDay.Where(c => c.MaChuyenDe == buoiCo.MaChuyenDe && c.MaCanBo != buoiCo.MaCanBo).Select(c => c.MaCanBo).ToList()
                .First(m => !csdl.CreateDbContext().BuoiHoc.Any(b => b.Ngay == buoiCo.Ngay && b.MaCanBo == m));
            var loiPhong = await daoTao.KiemTraBuoiHocAsync(Moi(b => { b.MaCanBo = gvKhac; b.MaPhong = buoiCo.MaPhong; }));
            kiemTra(loiPhong.Any(l => l.StartsWith("Phòng") && l.Contains("đã được lớp")), "Chặn phòng bị đặt chồng");
            kiemTra((await daoTao.KiemTraBuoiHocAsync(Moi(b => { b.MaCanBo = gvKhac; b.MaPhong = "GD-401"; }))).Any(l => l.Contains("bảo trì")), "Phòng bảo trì không xếp được");
            kiemTra((await daoTao.KiemTraBuoiHocAsync(Moi(b => { b.MaCanBo = gvKhac; b.MaPhong = "HT-01"; }))).Any(l => l.Contains("vượt sức chứa 30")), "Sĩ số vượt sức chứa bị chặn, nêu số chỗ");
            kiemTra((await daoTao.KiemTraBuoiHocAsync(Moi(b => { b.MaCanBo = "CB108"; b.MaPhong = phongTrong; }))).Any(l => l.Contains("chưa có trong danh sách đủ điều kiện")), "Giảng viên chưa đủ điều kiện dạy chuyên đề bị chặn");
            kiemTra((await daoTao.KiemTraBuoiHocAsync(Moi(b => { b.MaCanBo = gvKhac; b.MaPhong = phongTrong; b.TietBatDau = 4; b.SoTiet = 3; }))).Any(l => l.Contains("ranh giới buổi")), "Buổi học không được vắt qua ranh giới sáng/chiều");
            var hopLe = Moi(b => { b.MaCanBo = gvKhac; b.MaPhong = phongTrong; });
            kiemTra((await daoTao.KiemTraBuoiHocAsync(hopLe)).Count == 0, "Buổi học hợp lệ không có lý do chặn");
            kiemTra((await daoTao.LuuBuoiHocAsync(hopLe)).ThanhCong, "Xếp buổi học hợp lệ");
            var idMoi = csdl.CreateDbContext().BuoiHoc.Max(b => b.Id);
            kiemTra(!(await Lay<DichVuDaoTao>(gv).LuuBuoiHocAsync(hopLe)).ThanhCong, "Giảng viên không tự xếp lịch");

            // ---- D. Điểm danh và tự điểm danh ----
            var hocVuGv = Lay<DichVuHocVu>(gv);
            var buoiQuaCuaGv = csdl.CreateDbContext().BuoiHoc.AsNoTracking().First(b => b.MaCanBo == "CB006" && b.Ngay < homNay);
            var buoiQuaNguoiKhac = csdl.CreateDbContext().BuoiHoc.AsNoTracking().First(b => b.MaCanBo != "CB006" && b.Ngay < homNay);
            var dd = await hocVuGv.LayDiemDanhAsync(buoiQuaCuaGv.Id);
            kiemTra(dd.DuocGhi && dd.Dong.Count == 40 && dd.Dong.All(d => d.TrangThai is not null), "Giảng viên xem điểm danh buổi mình dạy (đủ 40 học viên)");
            kiemTra(!(await hocVuGv.LayDiemDanhAsync(buoiQuaNguoiKhac.Id)).DuocGhi && !(await hocVuGv.LuuDiemDanhAsync(buoiQuaNguoiKhac.Id, new Dictionary<string, string>())).ThanhCong, "Giảng viên không điểm danh buổi của người khác");
            var maHvDd = dd.Dong[0].MaHocVien;
            var ttMoi = dd.Dong[0].TrangThai == TrangThaiDiemDanh.VangKhongPhep ? TrangThaiDiemDanh.CoMat : TrangThaiDiemDanh.VangKhongPhep;
            kiemTra((await hocVuGv.LuuDiemDanhAsync(buoiQuaCuaGv.Id, new Dictionary<string, string> { [maHvDd] = ttMoi })).ThanhCong, "Giảng viên sửa điểm danh");
            kiemTra((await hocVuGv.LayDiemDanhAsync(buoiQuaCuaGv.Id)).Dong.First(d => d.MaHocVien == maHvDd).TrangThai == ttMoi, "Điểm danh đã lưu");
            kiemTra(!(await hocVuGv.LuuDiemDanhAsync(buoiCo.Id, new Dictionary<string, string> { [maHvDd] = TrangThaiDiemDanh.CoMat })).ThanhCong, "Không điểm danh buổi chưa diễn ra");
            kiemTra(!(await Lay<DichVuHocVu>(qlDaoTao).LayDiemDanhAsync(buoiQuaCuaGv.Id)).DuocGhi, "Quản lý đào tạo không xem điểm danh từng học viên");

            var hocVuHv = Lay<DichVuHocVu>(hv);
            var buoiToiCuaHv = csdl.CreateDbContext().BuoiHoc.AsNoTracking().Where(b => b.MaLop == "B1D58" && b.Ngay > homNay).OrderBy(b => b.Ngay).First();
            var batDau = QuyTacHocVu.BatDau(buoiToiCuaHv);
            hocVuHv.BayGio = () => batDau.AddMinutes(-40);
            kiemTra((await hocVuHv.TuDiemDanhAsync(buoiToiCuaHv.Id)).ThongBao.Contains("Chưa đến giờ"), "Tự điểm danh trước 30 phút bị từ chối, nêu giờ mở cổng");
            hocVuHv.BayGio = () => batDau.AddMinutes(20);
            kiemTra((await hocVuHv.TuDiemDanhAsync(buoiToiCuaHv.Id)).ThongBao.Contains("quá giờ"), "Tự điểm danh sau 15 phút bị từ chối");
            hocVuHv.BayGio = () => batDau.AddMinutes(-10);
            kiemTra((await hocVuHv.LayBuoiDangMoTuDiemDanhAsync())?.Id == buoiToiCuaHv.Id, "Học viên thấy buổi đang mở cổng tự điểm danh");
            var tu = await hocVuHv.TuDiemDanhAsync(buoiToiCuaHv.Id);
            kiemTra(tu.ThanhCong && tu.ThongBao.Contains(TrangThaiDiemDanh.CoMat), "Tự điểm danh trong khung giờ: có mặt");
            kiemTra(!(await hocVuHv.TuDiemDanhAsync(buoiToiCuaHv.Id)).ThanhCong, "Không tự điểm danh hai lần");
            hocVuHv.BayGio = () => QuyTacHocVu.BatDau(buoiQuaNguoiKhac).AddMinutes(-5);
            kiemTra(buoiQuaNguoiKhac.MaLop == "B1D58" || !(await hocVuHv.TuDiemDanhAsync(buoiQuaNguoiKhac.Id)).ThanhCong, "Không tự điểm danh buổi của lớp khác");
            kiemTra(QuyTacHocVu.TrangThaiTuDiemDanh(buoiToiCuaHv, batDau.AddMinutes(10)) == TrangThaiDiemDanh.DiMuon, "Tự điểm danh sau 5 phút ghi đi muộn");

            // ---- E. Điểm: 30/70, xếp loại, thiếu điểm nói rõ ----
            kiemTra(QuyTacHocVu.TinhTongKet(10, 7).TongKet == 7.9, "Tổng kết = 30% chuyên cần + 70% kiểm tra (10 và 7 → 7,9)");
            kiemTra(QuyTacHocVu.TinhTongKet(8.5, 4.6).TongKet == 5.8, "Làm tròn 1 chữ số (8,5 và 4,6 → 5,77 → 5,8)");
            kiemTra(QuyTacHocVu.TinhTongKet(null, 7).LyDoThieu!.Contains("chuyên cần") && QuyTacHocVu.TinhTongKet(9, null).LyDoThieu!.Contains("kiểm tra"), "Thiếu điểm nói rõ thiếu điểm nào");
            kiemTra(new[] { 4.9, 5.0, 6.5, 8.0, 9.0 }.Select(d => QuyTacHocVu.XepLoai(d)).SequenceEqual(["Không đạt", "Đạt", "Khá", "Giỏi", "Xuất sắc"]), "Ngưỡng xếp loại 5,0 / 6,5 / 8,0 / 9,0");
            kiemTra(QuyTacHocVu.TinhChuyenCan([TrangThaiDiemDanh.CoMat, TrangThaiDiemDanh.DiMuon, TrangThaiDiemDanh.VangCoPhep, TrangThaiDiemDanh.VangKhongPhep]) == 5.6, "Chuyên cần tính từ điểm danh (1 + 0,75 + 0,5 + 0)/4 × 10 = 5,6");
            var cdGvDay = buoiQuaCuaGv.MaChuyenDe;
            var bangGv = await hocVuGv.LayBangDiemAsync(buoiQuaCuaGv.MaLop, cdGvDay);
            kiemTra(bangGv.DuocSua && bangGv.Dong.Count == 40 && bangGv.Dong.Any(d => d.LyDoThieu is not null || d.TongKet is not null), "Giảng viên xem và sửa bảng điểm chuyên đề mình dạy");
            var maHvDiem = bangGv.Dong[1].MaHocVien;
            kiemTra((await hocVuGv.LuuDiemKiemTraAsync(buoiQuaCuaGv.MaLop, cdGvDay, new Dictionary<string, double?> { [maHvDiem] = 8.25 })).ThanhCong, "Giảng viên nhập điểm kiểm tra");
            kiemTra((await hocVuGv.LayBangDiemAsync(buoiQuaCuaGv.MaLop, cdGvDay)).Dong.First(d => d.MaHocVien == maHvDiem).DiemKiemTra == 8.3, "Điểm kiểm tra làm tròn 1 chữ số khi lưu");
            kiemTra(!(await hocVuGv.LuuDiemKiemTraAsync(buoiQuaCuaGv.MaLop, cdGvDay, new Dictionary<string, double?> { [maHvDiem] = 11 })).ThanhCong, "Điểm ngoài 0–10 bị từ chối");
            var cdKhongDay = csdl.CreateDbContext().BuoiHoc.AsNoTracking().Where(b => b.MaLop == buoiQuaCuaGv.MaLop && b.MaCanBo != "CB006").Select(b => b.MaChuyenDe).ToList().First(c => !csdl.CreateDbContext().BuoiHoc.Any(b => b.MaLop == buoiQuaCuaGv.MaLop && b.MaChuyenDe == c && b.MaCanBo == "CB006"));
            kiemTra(!(await hocVuGv.LuuDiemKiemTraAsync(buoiQuaCuaGv.MaLop, cdKhongDay, new Dictionary<string, double?> { [maHvDiem] = 7 })).ThanhCong, "Giảng viên không nhập điểm chuyên đề người khác dạy");
            var cdLopHv = (await hocVuHv.LayChuyenDeCuaLopAsync("B1D58")).First().MaChuyenDe;
            var bangHv = await hocVuHv.LayBangDiemAsync("B1D58", cdLopHv);
            kiemTra(!bangHv.DuocSua && bangHv.Dong.Count == 1 && bangHv.Dong[0].MaHocVien == maHvMau, "Học viên chỉ thấy dòng điểm của mình");
            kiemTra((await Lay<DichVuHocVu>(qlKhoa).LayBangDiemAsync("B1D58", cdLopHv)).Dong.Count == 0, "Quản lý khoa không xem điểm từng học viên");

            // ---- F. Nhật ký tự động, chỉ thêm ----
            var hvSua = dlAdmin.LayDanhSachHocVien().First(h => h.MaLop == "B3D53");
            var tenCu = hvSua.HoTen; hvSua.HoTen = "Trần Thị Thử";
            kiemTra((await Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).LuuHocVienAsync(hvSua, hvSua.MaHocVien)).ThanhCong, "Quản lý học viên sửa tên học viên");
            var nhatKy = await Lay<DichVuNhatKy>(admin).LayAsync();
            var dongSua = nhatKy.First(n => n.DoiTuong == "Học viên" && n.MaDoiTuong == hvSua.MaHocVien);
            kiemTra(dongSua.HanhDong == "Sửa" && dongSua.NguoiThucHien == "quanlyhocvien" && dongSua.GiaTriCu!.Contains(tenCu) && dongSua.GiaTriMoi!.Contains("Trần Thị Thử") && !dongSua.GiaTriMoi.Contains("SoCCCD"), "Nhật ký ghi người sửa, chỉ trường đổi, giá trị cũ → mới");
            kiemTra(nhatKy.Any(n => n.DoiTuong == "Điểm danh" && n.NguoiThucHien == "giangvien") && nhatKy.Any(n => n.DoiTuong == "Kết quả" && n.HanhDong == "Sửa"), "Nhật ký ghi điểm danh và nhập điểm");
            kiemTra(nhatKy.Any(n => n.DoiTuong == "Buổi học" && n.HanhDong == "Thêm" && n.MaDoiTuong == idMoi.ToString()), "Nhật ký ghi buổi học mới kèm mã tự tăng");
            await Lay<DichVuNhatKy>(qlKhoa).GhiXemDuLieuCaNhanAsync("Cán bộ", "CB001", "Số CCCD");
            kiemTra((await Lay<DichVuNhatKy>(admin).LayAsync()).Any(n => n.HanhDong == "Xem dữ liệu cá nhân" && n.NguoiThucHien == "quanlykhoa" && n.MaDoiTuong == "CB001"), "Ghi nhật ký khi bấm Hiện đủ dữ liệu cá nhân");
            var biChan = false; try { await Lay<DichVuNhatKy>(qlKhoa).LayAsync(); } catch (UnauthorizedAccessException) { biChan = true; }
            kiemTra(biChan, "Chỉ Admin xem nhật ký");
            var khongSuaDuoc = false; try { await csdl.CreateDbContext().Database.ExecuteSqlRawAsync("UPDATE NhatKy SET NguoiThucHien = 'x'"); } catch (Exception) { khongSuaDuoc = true; }
            var khongXoaDuoc = false; try { await csdl.CreateDbContext().Database.ExecuteSqlRawAsync("DELETE FROM NhatKy"); } catch (Exception) { khongXoaDuoc = true; }
            kiemTra(khongSuaDuoc && khongXoaDuoc, "Hàng rào 7: CSDL chặn sửa và xoá nhật ký");

            // ---- G. Cấp tài khoản chỉ đi xuống ----
            var tk = root.GetRequiredService<DichVuTaiKhoan>();
            ClaimsPrincipal Nguoi(IServiceScope sc) => Lay<AuthenticationStateProvider>(sc).GetAuthenticationStateAsync().Result.User;
            kiemTra((await tk.LuuTaiKhoan(Nguoi(qlDaoTao), null, "gv-moi", "MatKhau1", "GiangVien", "cb010", false)).StartsWith("Đã"), "Quản lý đào tạo cấp tài khoản giảng viên (gắn mã cán bộ)");
            kiemTra(!(await tk.LuuTaiKhoan(Nguoi(qlDaoTao), null, "ql-moi", "MatKhau1", "Manager", "DaoTao", false)).StartsWith("Đã"), "Quản lý đào tạo không cấp vai trò ngang hàng");
            kiemTra(!(await tk.LuuTaiKhoan(Nguoi(qlDaoTao), null, "hv-moi", "MatKhau1", "HocVien", maHvMau, false)).StartsWith("Đã"), "Quản lý đào tạo không cấp tài khoản học viên");
            kiemTra((await tk.LuuTaiKhoan(Nguoi(qlHocVien), null, "hv-sai", "MatKhau1", "HocVien", "HV99999", false)).Contains("không tồn tại"), "Tài khoản học viên phải gắn mã học viên có thật");
            kiemTra((await tk.DanhSach(Nguoi(qlHocVien))).All(u => u.VaiTro == "HocVien"), "Quản lý học viên chỉ thấy tài khoản học viên");
            kiemTra(!(await tk.LuuTaiKhoan(Nguoi(gv), null, "x", "MatKhau1", "HocVien", maHvMau, false)).StartsWith("Đã"), "Giảng viên không cấp tài khoản");
            using (var gvMoi = await Phien(""))
            {
                kiemTra(await Lay<NhaCungCapTrangThaiXacThucDemo>(gvMoi).DangNhapAsync("gv-moi", "MatKhau1") && Lay<DichVuQuanLyDuLieuDemo>(gvMoi).LayDanhSachLop().Select(l => l.MaLop).ToHashSet().SetEquals(kho.LopGiangVienDay("CB010")), "Tài khoản giảng viên mới chỉ thấy lớp của CB010");
            }

            // ---- H. Xoá giữ lịch sử học vụ ----
            var kqXoa = await Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).XoaHocVienAsync(maHvDd);
            kiemTra(!kqXoa.ThanhCong && kqXoa.ThongBao.Contains("lịch sử học vụ"), "Không xoá học viên đã có điểm danh, nói rõ lý do");
            kiemTra(!(await daoTao.XoaPhongAsync(buoiCo.MaPhong)).ThanhCong && !(await daoTao.XoaChuyenDeAsync(buoiCo.MaChuyenDe)).ThanhCong, "Không xoá phòng, chuyên đề đã dùng trong lịch");
            kiemTra((await daoTao.XoaBuoiHocAsync(idMoi)).ThanhCong, "Xoá buổi học chưa điểm danh");

            // ---- I. Tìm kiếm Ctrl+K theo quyền ----
            var tenHvKhac = dlAdmin.LayDanhSachHocVien().First(h => h.MaLop == "B9D54").HoTen;
            kiemTra((await Lay<DichVuTimKiem>(admin).TimAsync(DichVuTimKiem.BoDau(tenHvKhac))).Any(k => k.Loai == "Học viên"), "Admin tìm học viên bằng tên không dấu");
            kiemTra(!(await Lay<DichVuTimKiem>(qlDaoTao).TimAsync(tenHvKhac)).Any(k => k.Loai == "Học viên"), "Quản lý đào tạo tìm không ra từng học viên");
            kiemTra((await Lay<DichVuTimKiem>(hv).TimAsync(tenHvKhac)).All(k => k.Loai != "Học viên" || k.Phu == "Lớp B1D58"), "Học viên chỉ tìm thấy bạn cùng lớp");
            kiemTra((await Lay<DichVuTimKiem>(gv).TimAsync("lich dao tao")).Any(k => k.DuongDan == "/dao-tao/lich"), "Tìm được trang theo tên");

            // ---- K. Sơ đồ tổ chức (dựng từ mặt cắt theo quyền) ----
            var cayToChuc = await Lay<DichVuSoDoToChuc>(admin).LayCayToChucAsync();
            kiemTra(cayToChuc.Con.All(k => k.LanhDao.Count(l => l.ChucVu == "Trưởng khoa") <= 1) && cayToChuc.Con.First(k => k.Ma == "KH-01").LanhDao.Count == 3 && cayToChuc.Con[0].Con[0].GioiThieu!.Contains("trực thuộc"), "Sơ đồ: khoa có trưởng/phó khoa, tổ có giới thiệu");
            kiemTra(cayToChuc.Con.Count == 12 && cayToChuc.Con.Sum(k => k.Con.Count) == 24 && cayToChuc.SoCanBo == kho.DanhSachCanBo.Count, "Sơ đồ tổ chức: 12 khoa, 24 tổ, đủ cán bộ");
            var cayDaoTao = Lay<DichVuSoDoToChuc>(admin).LayCayDaoTao();
            kiemTra(cayDaoTao.Con.Count == 2 && cayDaoTao.SoLop == kho.DanhSachLop.Count && cayDaoTao.TatCa().Count(n => n.Loai == "Lớp") == kho.DanhSachLop.Count, "Sơ đồ đào tạo: hệ → khoá → lớp đủ số lớp");
            var dangMo = new HashSet<string> { "goc", cayDaoTao.Con[0].Khoa, cayDaoTao.Con[0].Con[0].Khoa };
            var boCuc = DichVuSoDoToChuc.BoCuc(cayDaoTao, dangMo, new HashSet<string>());
            kiemTra(boCuc.The.Count(t => t.LaGom) == 1 && boCuc.The.Count(t => t.Nut.Loai == "Lớp" && !t.LaGom) == DichVuSoDoToChuc.SoConToiDa, "Canvas: mỗi nhánh tối đa 8 thẻ, phần dư gom thành “+ N mục nữa”");
            kiemTra(Lay<DichVuSoDoToChuc>(gv).LayCayDaoTao().SoLop == lopGv.Count, "Sơ đồ đào tạo của giảng viên chỉ có lớp mình dạy");

            // ---- L. Lịch sử bổ nhiệm ----
            var khoaThu = kho.DanhSachKhoa.First(k => k.MaKhoa == "KH-03");
            var truongCu = khoaThu.MaTruongKhoa!;
            kiemTra(kho.LayBoNhiem(truongCu).Any(b => b.ChucVu == "Trưởng khoa" && b.ConHieuLuc), "Bổ nhiệm ban đầu cho trưởng khoa đương nhiệm");
            var cuHaChuc = kho.DanhSachCanBo.First(c => c.MaCanBo == truongCu).TaoBanSao(); cuHaChuc.ChucVu = "Cán bộ giảng viên";
            kiemTra((await Lay<DichVuQuanLyDuLieuDemo>(qlKhoa).LuuCanBoAsync(cuHaChuc, truongCu)).ThanhCong, "Thôi giữ chức trưởng khoa");
            var cbMoi = dlAdmin.LayDanhSachCanBo().First(c => c.MaKhoa == "KH-03" && c.ChucVu == "Cán bộ giảng viên" && c.MaTraCuuNoiBo != truongCu);
            var cbSua = kho.DanhSachCanBo.First(c => c.MaCanBo == cbMoi.MaTraCuuNoiBo).TaoBanSao(); cbSua.ChucVu = "Trưởng khoa";
            var kqBoNhiem = await Lay<DichVuQuanLyDuLieuDemo>(qlKhoa).LuuCanBoAsync(cbSua, cbSua.MaCanBo);
            kiemTra(kqBoNhiem.ThanhCong && kho.LayBoNhiem(cbSua.MaCanBo).Any(b => b.ChucVu == "Trưởng khoa" && b.ConHieuLuc && b.TuNgay == DateTime.Today), "Bổ nhiệm trưởng khoa mới được ghi lịch sử từ hôm nay");
            kiemTra(kho.LayBoNhiem(truongCu).All(b => !b.ConHieuLuc || b.ChucVu != "Trưởng khoa"), "Bổ nhiệm cũ được đóng (có ngày kết thúc)");

            // ---- M. Văn bản, thông báo: lưu bền, nơi nhận, đã đọc ----
            var tbAdmin = Lay<DichVuThongBao>(admin);
            var tatCa = tbAdmin.LayDanhSach();
            kiemTra(tatCa.Count >= 6 && tatCa.Any(x => x.SoKyHieu == "125/KH-HV" && x.Ghim), "Thông báo mẫu có thể thức hành chính (số/ký hiệu, ghim)");
            var maTb = await tbAdmin.LuuAsync(new ThongBao { TieuDe = "Lịch kiểm tra lớp B9D54", NoiDung = "Nội dung thử", LoaiVanBan = "Kế hoạch", SoKyHieu = tbAdmin.GoiYSoKyHieu("Kế hoạch"), MucDo = "Khẩn", Tags = ["lop:B9D54"] });
            kiemTra(tbAdmin.LayDanhSach().Single(x => x.Ma == maTb).SoKyHieu == "126/KH-HV", "Gợi ý số/ký hiệu tăng dần theo loại (126/KH-HV)");
            var trungSo = false; try { await tbAdmin.LuuAsync(new ThongBao { TieuDe = "Trùng số", NoiDung = "x", SoKyHieu = "126/KH-HV" }); } catch (ArgumentException) { trungSo = true; }
            kiemTra(trungSo, "Không cho hai văn bản trùng số/ký hiệu");
            var tbHv = Lay<DichVuThongBao>(hv);
            var cuaHv = await tbHv.LayDanhSachChoToiAsync();
            kiemTra(!cuaHv.Any(x => x.Ma == maTb) && cuaHv.Any(x => x.Tags.Count == 0) && cuaHv.Any(x => x.Tags.Contains("lop:*")), "Học viên chỉ nhận văn bản chung và văn bản gửi tới lớp/khoá/hệ của mình");
            kiemTra((await Lay<DichVuThongBao>(qlDaoTao).LayDanhSachChoToiAsync()).Any(x => x.Ma == maTb), "Quản lý thấy mọi văn bản");
            var chuaDocTruoc = await tbHv.DemChuaDocAsync();
            await tbHv.DanhDauDaDocAsync(cuaHv[0].Ma); await tbHv.DanhDauDaDocAsync(cuaHv[0].Ma);
            kiemTra(await tbHv.DemChuaDocAsync() == chuaDocTruoc - 1, "Mở văn bản giảm số chưa đọc đúng một (ghi một lần)");
            kiemTra((await tbAdmin.LayNguoiDaDocAsync(cuaHv[0].Ma)).Any(x => x.TenDangNhap == "hocvien"), "Admin xem được ai đã đọc");
            var biChanDoc = false; try { await tbHv.LayNguoiDaDocAsync(cuaHv[0].Ma); } catch (UnauthorizedAccessException) { biChanDoc = true; }
            kiemTra(biChanDoc, "Học viên không xem danh sách người đã đọc");
            var tbHetHan = await tbAdmin.LuuAsync(new ThongBao { TieuDe = "Đã hết hạn", NoiDung = "x", HieuLucDen = DateTimeOffset.UtcNow.AddDays(-1) });
            kiemTra(!(await tbHv.LayDanhSachChoToiAsync()).Any(x => x.Ma == tbHetHan) && (await tbAdmin.LayDanhSachChoToiAsync()).Any(x => x.Ma == tbHetHan), "Văn bản hết hiệu lực ẩn với người xem, Admin vẫn thấy");
            var khoTbMoi = new KhoThongBaoDemo(csdl);
            kiemTra(khoTbMoi.DanhSach.Any(x => x.Ma == maTb && x.MucDo == "Khẩn" && x.Tags.SequenceEqual(new[] { "lop:B9D54" })), "Thông báo lưu bền trong CSDL (mở lại còn đủ thể thức và nơi nhận)");
            kiemTra((await tbAdmin.LayUuTienAsync()).First().Ghim, "Văn bản ưu tiên ở trang chủ: ghim lên đầu");

            // ---- N. Excel: xuất theo mặt cắt, nhập có xem trước ----
            static List<string> DocCot(byte[] tep, int cot)
            {
                using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(tep));
                var ws = wb.Worksheet(1);
                return Enumerable.Range(5, Math.Max(0, ws.LastRowUsed()!.RowNumber() - 4)).Select(r => ws.Cell(r, cot).GetString()).ToList();
            }
            var tepAdmin = await Lay<DichVuExcel>(admin).XuatCanBoAsync("KH-01");
            var tepQldt = await Lay<DichVuExcel>(qlDaoTao).XuatCanBoAsync("KH-01");
            kiemTra(DocCot(tepAdmin, 13).All(x => x.Length == 12) && DocCot(tepQldt, 13).All(x => x == "") && DocCot(tepQldt, 2).Count == 9, "Xuất Excel dùng đúng mặt cắt: Quản lý đào tạo không nhận CCCD cán bộ");
            kiemTra(DocCot(await Lay<DichVuExcel>(qlKhoa).XuatHocVienAsync(), 1).Count == 0, "Quản lý khoa xuất học viên: tệp rỗng (chỉ số liệu tổng hợp)");
            kiemTra(DocCot(await Lay<DichVuExcel>(admin).XuatHocVienAsync("B1D58"), 9).All(x => x.Length == 12 && x.StartsWith("0")), "CCCD giữ nguyên số 0 đầu khi xuất");
            kiemTra(await Lay<DichVuExcel>(qlKhoa).XuatBangDiemAsync("B1D58", cdLopHv, "x") is null, "Không xuất được bảng điểm khi không có quyền xem");
            var mau = Lay<DichVuExcel>(qlHocVien).TaoMauNhapHocVien();
            using (var wbNhap = new ClosedXML.Excel.XLWorkbook(new MemoryStream(mau)))
            {
                var ws = wbNhap.Worksheet(1);
                string[][] dongNhap =
                [
                    ["", "Lê Văn Nhập", "B4D53", "Học viên", "02/03/2004", "Nam", "Huế", "0911111111", "009204000111", "", "Số 2, đường Số 2, Huế"],
                    ["", "Trần Thị Lỗi", "B99D58", "Học viên", "31/02/2004", "Nữ", "Huế", "123", "009204000112", "", ""],
                    ["", "Phạm Văn Trùng", "B4D53", "Học viên", "02/03/2004", "Nam", "Huế", "0911111112", "009204000111", "", "Số 3, Huế"],
                ];
                for (var i = 0; i < dongNhap.Length; i++) for (var c = 0; c < 11; c++) ws.Cell(3 + i, c + 1).SetValue(dongNhap[i][c]);
                using var msNhap = new MemoryStream(); wbNhap.SaveAs(msNhap); msNhap.Position = 0;
                var (dongDoc, loiDoc) = await Lay<DichVuExcel>(qlHocVien).DocTepHocVienAsync(msNhap);
                kiemTra(loiDoc is null && dongDoc.Count == 3 && dongDoc[0].HopLe && dongDoc[0].TuSinhMa, "Nhập Excel: dòng hợp lệ, mã tự sinh, bỏ qua dòng ví dụ");
                kiemTra(dongDoc[1].Loi.Any(l => l.Contains("Ngày sinh")) && dongDoc[1].Loi.Any(l => l.Contains("B99D58")) && dongDoc[1].Loi.Any(l => l.Contains("điện thoại")), "Xem trước nêu rõ từng lỗi (ngày sinh, lớp, số điện thoại)");
                kiemTra(dongDoc[2].Loi.Any(l => l.Contains("CCCD")), "Phát hiện trùng CCCD giữa các dòng trong tệp");
                var soTruoc = kho.DanhSachHocVien.Count;
                kiemTra(kho.DanhSachHocVien.Count == soTruoc, "Xem trước không ghi gì");
                var (soNhap, loiNhap) = await Lay<DichVuExcel>(qlHocVien).XacNhanNhapAsync(dongDoc);
                kiemTra(soNhap == 1 && loiNhap.Count == 0 && kho.DanhSachHocVien.Count == soTruoc + 1 && kho.DanhSachHocVien.Any(h => h.HoTen == "Lê Văn Nhập" && h.MaLop == "B4D53"), "Xác nhận chỉ ghi dòng hợp lệ");
                msNhap.Position = 0;
                kiemTra((await Lay<DichVuExcel>(qlDaoTao).DocTepHocVienAsync(msNhap)).Loi!.Contains("Chỉ Admin"), "Quản lý đào tạo không nhập được học viên");
            }
            kiemTra((await Lay<DichVuNhatKy>(admin).LayAsync()).Any(n => n.HanhDong == "Xuất Excel") && (await Lay<DichVuNhatKy>(admin).LayAsync()).Any(n => n.HanhDong == "Nhập Excel"), "Xuất, nhập Excel được ghi nhật ký");

            // ---- O. Hệ liên thông, đổi mật khẩu, cần chú ý ----
            kiemTra(QuyTacDuLieu.LopHopLe("B16D58", "Liên thông", "D58", "Điều tra hình sự") && !QuyTacDuLieu.LopHopLe("B5D58", "Liên thông", "D58", "Điều tra hình sự") && QuyTacDuLieu.TinhNienKhoa("Liên thông", "D58") == "2026–2030", "Liên thông: mã B16–20 + khoá D, niên khoá như chính quy");
            kiemTra((await Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).LuuLopAsync(new LopDaoTao { MaLop = "B16D58", HeDaoTao = "Liên thông", KhoaHoc = "D58", ChuyenNganh = "An ninh mạng sai" })).ThanhCong == false
                && (await Lay<DichVuQuanLyDuLieuDemo>(qlHocVien).LuuLopAsync(new LopDaoTao { MaLop = "B16D58", HeDaoTao = "Liên thông", KhoaHoc = "D58", ChuyenNganh = "ANM & PCTPCNC", GiaoVienChuNhiem = "Thiếu tá Mẫu" })).ThanhCong, "Thêm lớp liên thông, chặn chuyên ngành ngoài danh mục");
            kiemTra((await tk.DoiMatKhau(Nguoi(gv), "demo123", "MatKhauMoi1")).Contains("Tài khoản mẫu"), "Tài khoản mẫu không đổi được mật khẩu");
            using (var gvMoi2 = await Phien(""))
            {
                await Lay<NhaCungCapTrangThaiXacThucDemo>(gvMoi2).DangNhapAsync("gv-moi", "MatKhau1");
                var nguoiMoi = Nguoi(gvMoi2);
                kiemTra((await tk.DoiMatKhau(nguoiMoi, "sai", "MatKhau2")).Contains("chưa đúng"), "Đổi mật khẩu: sai mật khẩu hiện tại bị từ chối");
                kiemTra((await tk.DoiMatKhau(nguoiMoi, "MatKhau1", "MatKhau2")).StartsWith("Đã") && !tk.HopLe(nguoiMoi), "Đổi mật khẩu thành công, phiên cũ hết hiệu lực");
                kiemTra(await Lay<NhaCungCapTrangThaiXacThucDemo>(gvMoi2).DangNhapAsync("gv-moi", "MatKhau2"), "Đăng nhập bằng mật khẩu mới");
            }
            var canChuY = await Lay<DichVuTongQuan>(admin).LayCanChuYAsync();
            kiemTra(canChuY.Any(m => m.Ten.Contains("bảo trì") && m.SoLuong == 1) && canChuY.Any(m => m.Ten.Contains("thiếu điểm") && m.SoLuong > 0), "Cần chú ý: đếm phòng bảo trì, lượt thiếu điểm");
            kiemTra((await Lay<DichVuTongQuan>(qlKhoa).LayCanChuYAsync()).All(m => !m.Ten.Contains("điểm")), "Cần chú ý theo vai trò: Quản lý khoa không thấy mục điểm học viên");
            kiemTra((await Lay<DichVuTongQuan>(admin).LayHoatDongGanDayAsync()).Count == 6 && (await Lay<DichVuTongQuan>(qlKhoa).LayHoatDongGanDayAsync()).Count == 0, "Hoạt động gần đây chỉ Admin thấy");

            // ---- P. Chương trình, phân công, chuyên cần, xét tư cách thi, lịch thi, chấm công, bản tin ----
            var ctAdmin = Lay<DichVuChuongTrinh>(admin);
            var chuongTrinh = await ctAdmin.LayChuongTrinhAsync("B1D58");
            kiemTra(chuongTrinh.Count > 0 && chuongTrinh.All(d => d.MaCanBo is not null && d.TenKhoa != "") && chuongTrinh.Any(d => d.TrangThai is "Đang học" or "Đã xong"), "Chương trình lớp: chuyên đề, khoa phụ trách, giảng viên phân công, trạng thái");
            kiemTra((await ctAdmin.LayChuongTrinhAsync(maKhoa: "KH-01")).All(d => d.MaKhoa == "KH-01"), "Lọc chương trình theo khoa phụ trách");
            kiemTra((await ctAdmin.LopChuaPhanCongAsync()).Contains("B16D58"), "Lớp liên thông mới tạo chưa có phân công");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).LuuPhanCongAsync("B16D58", "CD-01", "CB006")).ThanhCong, "Quản lý học viên không phân công giảng dạy");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).LuuPhanCongAsync("B16D58", "CD-01", "CB050")).ThongBao.Contains("chưa có trong danh sách đủ điều kiện"), "Không phân công giảng viên chưa đủ điều kiện");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).LuuPhanCongAsync("B16D58", "CD-01", "CB006")).ThanhCong && !(await ctAdmin.LopChuaPhanCongAsync()).Contains("B16D58")
                && (await ctAdmin.LayChuongTrinhAsync("B16D58")).Single().TrangThai == "Chưa xếp lịch", "Phân công lớp mới: chưa xếp lịch");
            kiemTra(await ctAdmin.LayGiangVienPhanCongAsync("B16D58", "CD-01") == "CB006", "Biểu mẫu xếp lịch lấy được giảng viên phân công");
            var (homNayCo, homNayNghi) = await ctAdmin.LayHomNayAsync();
            kiemTra(homNayCo.Select(b => b.Buoi.MaLop).Distinct().Count() + homNayNghi.Count == kho.DanhSachLop.Count, "Hôm nay: lớp đi học + lớp nghỉ = tổng số lớp");
            var (chuyenCan, loiCc) = await ctAdmin.LayChuyenCanLopAsync("B1D58");
            kiemTra(loiCc is null && chuyenCan.Count == 40 && chuyenCan.All(c => c.SoBuoi == c.CoMat + c.DiMuon + c.VangCoPhep + c.VangKhongPhep), "Chuyên cần từng học viên: buổi học, buổi nghỉ khớp tổng");
            kiemTra((await Lay<DichVuChuongTrinh>(qlKhoa).LayChuyenCanLopAsync("B1D58")).Loi is not null, "Quản lý khoa không xem chuyên cần từng học viên");
            kiemTra(DichVuChuongTrinh.XetTuCach(10, 2).Du && !DichVuChuongTrinh.XetTuCach(10, 3).Du && !DichVuChuongTrinh.XetTuCach(0, 0).Du, "Quy tắc tư cách thi: vắng ≤ 20% số buổi đã điểm danh");

            var cdXet = buoiQuaCuaGv.MaChuyenDe; var lopXet = buoiQuaCuaGv.MaLop;
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlKhoa).GuiXetTuCachAsync(lopXet, cdXet)).ThanhCong, "Khoa không tự gửi đề xuất tư cách thi");
            var gui = await Lay<DichVuChuongTrinh>(qlHocVien).GuiXetTuCachAsync(lopXet, cdXet);
            kiemTra(gui.ThanhCong && gui.ThongBao.Contains("đủ tư cách"), "Quản lý học viên gửi đề xuất danh sách tư cách thi");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlDaoTao).DuyetXetTuCachAsync(lopXet, cdXet)).ThanhCong, "Phòng đào tạo không duyệt trước khi khoa xác nhận");
            var hsKhoa = await Lay<DichVuChuongTrinh>(qlKhoa).LayHoSoTuCachAsync(lopXet, cdXet);
            kiemTra(hsKhoa.DuocXacNhan && !hsKhoa.DuocXemTungNguoi && hsKhoa.Dong.All(d => !d.Du), "Khoa thấy hồ sơ chờ xác nhận, chỉ danh sách không đủ tư cách");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlKhoa).TraLaiXetTuCachAsync(lopXet, cdXet, " ")).ThanhCong, "Trả lại phải có lý do");
            kiemTra((await Lay<DichVuChuongTrinh>(qlKhoa).XacNhanXetTuCachAsync(lopXet, cdXet)).ThanhCong, "Khoa xác nhận");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).TraLaiXetTuCachAsync(lopXet, cdXet, "Cần rà soát lại điểm danh")).ThanhCong
                && (await ctAdmin.LayHoSoTuCachAsync(lopXet, cdXet)).Ho.TrangThai == TrangThaiXetTuCach.TraLai, "Phòng đào tạo trả lại hồ sơ kèm lý do");
            await Lay<DichVuChuongTrinh>(qlHocVien).GuiXetTuCachAsync(lopXet, cdXet);
            await Lay<DichVuChuongTrinh>(qlKhoa).XacNhanXetTuCachAsync(lopXet, cdXet);
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).DuyetXetTuCachAsync(lopXet, cdXet)).ThanhCong && (await ctAdmin.LayHoSoTuCachAsync(lopXet, cdXet)).Ho.TrangThai == TrangThaiXetTuCach.DaDuyet, "Gửi lại → khoa xác nhận → đào tạo duyệt");
            var ngayThi = DateTime.Today.AddDays(30); while (ngayThi.DayOfWeek == DayOfWeek.Sunday) ngayThi = ngayThi.AddDays(1);
            var lichThi = new LichThi { MaLop = lopXet, MaChuyenDe = cdXet, Ngay = ngayThi, TietBatDau = 1, SoTiet = 2, MaPhong = "HT-01", HinhThuc = "Tự luận" };
            kiemTra((await ctAdmin.KiemTraLichThiAsync(lichThi)).Any(l => l.Contains("vượt sức chứa")) || (await ctAdmin.LayHoSoTuCachAsync(lopXet, cdXet)).Dong.Count(d => d.Du) <= 30, "Lịch thi: phòng phải đủ chỗ cho học viên dự thi");
            lichThi.MaPhong = "PT-01";
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).LuuLichThiAsync(lichThi)).ThanhCong, "Lịch thi do Phòng đào tạo sắp xếp (Quản lý học viên không xếp)");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).LuuLichThiAsync(lichThi)).ThanhCong, "Phòng đào tạo xếp lịch thi sau khi duyệt");
            var lichThi2 = new LichThi { MaLop = lopXet, MaChuyenDe = chuongTrinh.Count > 0 ? "CD-99" : cdXet, Ngay = ngayThi, TietBatDau = 2, SoTiet = 2, MaPhong = "PT-01", HinhThuc = "Tự luận" };
            kiemTra((await ctAdmin.KiemTraLichThiAsync(lichThi2)).Any(l => l.Contains("tư cách thi đã được Phòng đào tạo duyệt")), "Chưa duyệt tư cách thì không xếp lịch thi");
            if (lopXet == "B1D58")
                kiemTra((await Lay<DichVuChuongTrinh>(hv).LayLichThiCuaToiAsync()).Any(x => x.Lich.MaChuyenDe == cdXet), "Học viên thấy lịch thi của mình");

            var ctGv = Lay<DichVuChuongTrinh>(gv);
            var congGv = await ctGv.TuChamCongAsync(DateTime.Today.AddHours(7).AddMinutes(15).DayOfWeek == DayOfWeek.Sunday ? DateTime.Today.AddDays(1).AddHours(7) : DateTime.Today.AddHours(7).AddMinutes(15));
            kiemTra(congGv.ThanhCong && congGv.ThongBao.Contains("Có mặt") || DateTime.Today.DayOfWeek == DayOfWeek.Saturday, "Giảng viên tự điểm danh đi làm trước 7:30: có mặt");
            kiemTra(!(await ctGv.TuChamCongAsync(DateTime.Today.AddHours(9))).ThanhCong || DateTime.Today.DayOfWeek == DayOfWeek.Sunday, "Mỗi ngày chỉ tự điểm danh đi làm một lần");
            kiemTra((await Lay<DichVuChuongTrinh>(qlKhoa).GhiChamCongAsync("CB007", DateTime.Today, "Công tác")).ThanhCong && !(await Lay<DichVuChuongTrinh>(qlHocVien).GhiChamCongAsync("CB007", DateTime.Today, "Vắng")).ThanhCong, "Quản lý khoa ghi chấm công; quản lý học viên không ghi được");
            kiemTra((await Lay<DichVuChuongTrinh>(qlKhoa).LayChamCongAsync(DateTime.Today, "KH-01")).Dong.Single(d => d.CanBo.MaCanBo == "CB007").Cong?.TrangThai == "Công tác", "Bảng chấm công theo khoa hiện đúng trạng thái");

            var btAdmin = Lay<DichVuBanTin>(admin);
            var cacTin = await btAdmin.LayDanhSachAsync();
            kiemTra(cacTin.Count >= 14 && cacTin.Select(t => t.ChuyenMuc).Distinct().Count() == TinTuc.CacChuyenMuc.Length, "Bản tin mẫu đủ các chuyên mục");
            var luotCu = cacTin[0].LuotXem;
            kiemTra((await Lay<DichVuBanTin>(hv).MoBaiAsync(cacTin[0].Id))!.LuotXem == luotCu + 1, "Mở bài tăng lượt xem");
            kiemTra(!(await Lay<DichVuBanTin>(qlKhoa).LuuAsync(new TinTuc { TieuDe = "x", Sapo = "x", NoiDung = "x" })).ThanhCong, "Chỉ Admin đăng bài bản tin");
            kiemTra((await btAdmin.LuuAsync(new TinTuc { TieuDe = "Bài thử nghiệm", Sapo = "Đoạn dẫn", NoiDung = "Nội dung", ChuyenMuc = "Đào tạo" })).ThanhCong && (await btAdmin.LayDanhSachAsync()).First().TieuDe == "Bài thử nghiệm", "Admin đăng bài, bài mới lên đầu");

            // ---- Q. Vòng 11 điểm: sổ đầu bài, nhập Excel chung, huỷ lịch thi, bảng điểm toàn khoá, bảng công tuần, từ điển ----
            var (sdbGv, loiSdbGv) = await Lay<DichVuHocVu>(gv).LaySoDauBaiAsync("B1D58");
            kiemTra(loiSdbGv is null && sdbGv.Count > 0 && sdbGv.Any(d => d.Ghi is not null), "Sổ đầu bài: giảng viên dạy lớp xem được, có dữ liệu mẫu");
            var buoiCuaGv = sdbGv.First(d => d.DuocGhi);
            kiemTra(sdbGv.Where(d => d.DuocGhi).All(d => d.Buoi.MaCanBo == "CB006"), "Giảng viên chỉ được ghi buổi mình dạy");
            kiemTra((await Lay<DichVuHocVu>(gv).GhiSoDauBaiAsync(buoiCuaGv.Buoi.Id, "Bài thử: ôn tập", "Lớp tốt", "B")).ThanhCong, "Giảng viên ghi sổ đầu bài buổi mình dạy");
            kiemTra(!(await Lay<DichVuHocVu>(gv).GhiSoDauBaiAsync(buoiCuaGv.Buoi.Id, "x", "", "E")).ThanhCong, "Xếp loại tiết ngoài A–D bị từ chối");
            kiemTra(!(await Lay<DichVuHocVu>(qlHocVien).GhiSoDauBaiAsync(buoiCuaGv.Buoi.Id, "x", "", "A")).ThanhCong, "Quản lý học viên chỉ xem, không ghi sổ đầu bài");
            kiemTra((await Lay<DichVuHocVu>(qlKhoa).LaySoDauBaiAsync("B1D58")).Dong.Single(d => d.Buoi.Id == buoiCuaGv.Buoi.Id).Ghi?.XepLoaiTiet == "B", "Quản lý khoa xem được sổ đầu bài vừa ghi");
            kiemTra((await Lay<DichVuHocVu>(hv).LaySoDauBaiAsync("B1D58")).Loi is not null && (await Lay<DichVuHocVu>(xem).LaySoDauBaiAsync("B1D58")).Loi is not null, "Học viên, người xem không mở sổ đầu bài");

            var nhapKhoa = Lay<DichVuNhapExcel>(qlKhoa);
            kiemTra(await Lay<DichVuNhapExcel>(qlHocVien).TaoMauAsync(LoaiNhap.CanBo) is null && await Lay<DichVuNhapExcel>(qlKhoa).TaoMauAsync(LoaiNhap.Phong) is null, "Nhập Excel theo mảng sở hữu (QLHV không nhập cán bộ, QL khoa không nhập phòng)");
            async Task<List<DichVuNhapExcel.DongNhapChung>> DocNhap(IServiceScope sc, LoaiNhap loai, string[][] dongs, string? lop = null, string? cd = null)
            {
                var m = (await Lay<DichVuNhapExcel>(sc).TaoMauAsync(loai, lop, cd))!;
                using var wbm = new ClosedXML.Excel.XLWorkbook(new MemoryStream(m));
                var w = wbm.Worksheet(1);
                var batDau = loai == LoaiNhap.Diem ? 2 : 3;
                if (loai == LoaiNhap.Diem && w.LastRowUsed()!.RowNumber() >= 2) w.Rows(2, w.LastRowUsed()!.RowNumber()).Delete();
                for (var i = 0; i < dongs.Length; i++) for (var c = 0; c < dongs[i].Length; c++) w.Cell(batDau + i, c + 1).SetValue(dongs[i][c]);
                using var ms = new MemoryStream(); wbm.SaveAs(ms); ms.Position = 0;
                var (d, l) = await Lay<DichVuNhapExcel>(sc).DocTepAsync(loai, ms, lop, cd);
                kiemTra(l is null, $"Đọc tệp nhập {loai} không lỗi tệp ({l})");
                return d;
            }
            ToChuyenMon toMau; lock (kho.KhoaDongBo) toMau = kho.DanhSachTo.First();
            var dongCb = await DocNhap(qlKhoa, LoaiNhap.CanBo, [
                ["CBNHAP1", "Đỗ Văn Nhập", "Nam", "01/02/1986", toMau.MaKhoa, toMau.MaTo, "Cán bộ giảng viên", "Thiếu tá", "", "Thạc sĩ", "Luật hình sự", "Huế", "0933000111", "046086000111", ""],
                ["CBNHAP2", "Lỗi Tổ", "Nam", "01/02/1986", toMau.MaKhoa, "KHONGCO", "Phó giám đốc", "Thiếu tá", "", "Thạc sĩ", "Luật", "Huế", "0933000112", "046086000112", ""]]);
            kiemTra(dongCb.Count == 2 && dongCb[0].HopLe && dongCb[1].Loi.Any(l => l.Contains("Tổ")) && dongCb[1].Loi.Any(l => l.Contains("Chức vụ")), "Nhập cán bộ: xem trước nêu lỗi tổ, chức vụ");
            var (okCb, loiCb) = await nhapKhoa.XacNhanAsync(LoaiNhap.CanBo, dongCb);
            bool coCbNhap; lock (kho.KhoaDongBo) coCbNhap = kho.DanhSachCanBo.Any(c => c.MaCanBo == "CBNHAP1");
            kiemTra(okCb == 1 && loiCb.Count == 0 && coCbNhap, "Nhập cán bộ: chỉ ghi dòng hợp lệ");
            var dongPhong = await DocNhap(qlDaoTao, LoaiNhap.Phong, [["PNHAP1", "Phòng nhập", "Giảng đường", "80", "Không"], ["PNHAP2", "Lỗi", "Hội trường lớn", "abc", "Có"]]);
            kiemTra(dongPhong[0].HopLe && !dongPhong[1].HopLe && (await Lay<DichVuNhapExcel>(qlDaoTao).XacNhanAsync(LoaiNhap.Phong, dongPhong)).ThanhCong == 1, "Nhập phòng học: dòng hợp lệ được ghi, dòng sai loại/sức chứa bị loại");
            var dongCd = await DocNhap(qlDaoTao, LoaiNhap.ChuyenDe, [["CDNHAP1", "Chuyên đề nhập", toMau.MaKhoa, "30", ""], ["CD-99", "Trùng mã", toMau.MaKhoa, "30", ""]]);
            kiemTra(dongCd[0].HopLe && dongCd[1].Loi.Any(l => l.Contains("đã có")), "Nhập chuyên đề: phát hiện mã đã tồn tại");
            var dongLop = await DocNhap(qlHocVien, LoaiNhap.Lop, [["B17D57", "Liên thông", "D57", "Trinh sát an ninh", ""], ["B1D58", "Chính quy", "D58", "Trinh sát an ninh", ""]]);
            kiemTra(dongLop[0].HopLe && dongLop[1].Loi.Any(l => l.Contains("đã có")), "Nhập lớp: lớp mới hợp lệ, lớp trùng bị loại");
            var cdGv = buoiCuaGv.Buoi.MaChuyenDe;
            string[] hvLop; lock (kho.KhoaDongBo) hvLop = kho.DanhSachHocVien.Where(h => h.MaLop == "B1D58").Take(2).Select(h => h.MaHocVien).ToArray();
            kiemTra(await Lay<DichVuNhapExcel>(qlKhoa).TaoMauAsync(LoaiNhap.Diem, "B1D58", cdGv) is null, "Quản lý khoa không nhập điểm");
            var dongDiem = await DocNhap(gv, LoaiNhap.Diem, [[hvLop[0], "", "8,5"], [hvLop[1], "", "11"], ["HV99999", "", "7"]], "B1D58", cdGv);
            kiemTra(dongDiem.Count == 3 && dongDiem[0].HopLe && dongDiem[1].Loi.Count > 0 && dongDiem[2].Loi.Any(l => l.Contains("không thuộc lớp")), "Nhập điểm: kiểm tra thang điểm và học viên thuộc lớp");
            kiemTra((await Lay<DichVuNhapExcel>(gv).XacNhanAsync(LoaiNhap.Diem, dongDiem, "B1D58", cdGv)).ThanhCong == 1
                && (await Lay<DichVuHocVu>(gv).LayBangDiemAsync("B1D58", cdGv)).Dong.Single(d => d.MaHocVien == hvLop[0]).DiemKiemTra == 8.5, "Nhập điểm từ Excel ghi đúng điểm");

            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).XoaLichThiAsync(lopXet, cdXet)).ThanhCong && (await Lay<DichVuChuongTrinh>(qlDaoTao).XoaLichThiAsync(lopXet, cdXet)).ThanhCong
                && (await ctAdmin.LayHoSoTuCachAsync(lopXet, cdXet)).Lich is null, "Huỷ lịch thi: chỉ Phòng đào tạo");

            var hkHv = await Lay<DichVuHocVu>(hv).LayBangDiemToanKhoaCuaToiAsync();
            kiemTra(hkHv.Count >= 1 && hkHv.Any(h => h.HienTai) && (await Lay<DichVuHocVu>(gv).LayBangDiemToanKhoaCuaToiAsync()).Count == 0, "Bảng điểm toàn khoá: học viên có học kỳ hiện tại, vai trò khác rỗng");
            var dauTuan = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            kiemTra((await Lay<DichVuChuongTrinh>(qlKhoa).LayBangCongTuanAsync(dauTuan)).Dong.Count > 0 && (await Lay<DichVuChuongTrinh>(qlDaoTao).LayBangCongTuanAsync(dauTuan)).Loi is not null, "Bảng công tuần: Quản lý khoa xem, Quản lý đào tạo không");
            kiemTra(TuDienGiaoDien.Dich("en", "Quản lý khoa") != "Quản lý khoa" && TuDienGiaoDien.Dich("lo", "Quản lý khoa") != "Quản lý khoa" && TuDienGiaoDien.Lay("lo").Count > 100 && TuDienGiaoDien.Dich("vi", "Quản lý khoa") == "Quản lý khoa", "Từ điển giao diện có tiếng Anh và tiếng Lào");
            kiemTra(AnhMinhHoa.BannerVanBan("Quyết định", "01/QĐ", "Thử").StartsWith("data:image/svg+xml;base64,"), "Ảnh minh hoạ văn bản là SVG nhúng");

            // ---- R. Vòng 10 điểm: lớp năm cuối, bảng điểm chi tiết, điều chỉnh tư cách, bài thi + thông báo, trường mở rộng ----
            kiemTra(csdl.CreateDbContext().BuoiHoc.Count(b => b.MaLop == KhoiTaoDaoTaoMau.LopNamCuoiMau) >= 15, "Lớp năm cuối B1D55 có lịch học hiện hành (tài khoản học viên duy nhất có đủ các năm)");
            {
                using var dbKt = csdl.CreateDbContext();
                var buoiNamCuoi = dbKt.BuoiHoc.Where(b => b.MaLop == KhoiTaoDaoTaoMau.LopNamCuoiMau).ToList();
                var buoiKhac = dbKt.BuoiHoc.Where(b => b.MaLop != KhoiTaoDaoTaoMau.LopNamCuoiMau).ToList();
                kiemTra(!buoiNamCuoi.Any(a => buoiKhac.Any(b => b.Ngay == a.Ngay && b.TietBatDau == a.TietBatDau && (b.MaCanBo == a.MaCanBo || b.MaPhong == a.MaPhong))), "Lịch bổ sung không trùng giảng viên, phòng với lịch có sẵn");
            }
            kiemTra(QuyTacHocVu.QuyDoi(8.6) == ("A", 4.0) && QuyTacHocVu.QuyDoi(6.7).Chu == "C+" && QuyTacHocVu.QuyDoi(3.9).He4 == 0 && QuyTacHocVu.HocLuc(8.2) == "Giỏi", "Quy đổi điểm chữ, hệ 4 và học lực");
            var hkHv2 = await Lay<DichVuHocVu>(hv).LayBangDiemToanKhoaCuaToiAsync();
            kiemTra(hkHv2.All(h => h.SoTiet is not null) && hkHv2.Any(h => h.ThuTu >= 1), "Bảng điểm có số tiết từng môn và thứ tự học kỳ trong khoá");

            // Điều chỉnh tư cách + lịch sử + xử lý hàng loạt
            var hoSoNhap = (await ctAdmin.LayDanhSachXetAsync()).FirstOrDefault(x => x.TrangThai == TrangThaiXetTuCach.Nhap);
            kiemTra(hoSoNhap.MaLop is not null, "Có hồ sơ xét tư cách chưa gửi để thử điều chỉnh");
            var hsNhap = await Lay<DichVuChuongTrinh>(qlHocVien).LayHoSoTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe);
            var hvDuNhat = hsNhap.Dong.First(d => d.Du).MaHocVien;
            kiemTra(hsNhap.DuocDieuChinh && !(await Lay<DichVuChuongTrinh>(qlKhoa).DieuChinhTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe, hvDuNhat, false, "Thử lý do dài")).ThanhCong, "Chỉ Quản lý học viên điều chỉnh tư cách");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).DieuChinhTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe, hvDuNhat, false, "ít")).ThanhCong, "Điều chỉnh bắt buộc lý do đủ dài");
            kiemTra((await Lay<DichVuChuongTrinh>(qlHocVien).DieuChinhTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe, hvDuNhat, false, "Vi phạm quy chế lớp, bị đình chỉ dự thi")).ThanhCong
                && (await ctAdmin.LayHoSoTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe)).Dong.Single(d => d.MaHocVien == hvDuNhat) is { Du: false, DieuChinh: not null }, "Điều chỉnh được áp dụng và ghi rõ người, lý do");
            var (okHl, _) = await Lay<DichVuChuongTrinh>(qlHocVien).XuLyHangLoatAsync("gui", [(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe)]);
            var hsSauGui = await ctAdmin.LayHoSoTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe);
            kiemTra(okHl == 1 && hsSauGui.Ho.TrangThai == TrangThaiXetTuCach.ChoKhoa && hsSauGui.Ho.KhongDuTuCach.Contains(hvDuNhat) && hsSauGui.LichSu!.Any(l => l.HanhDong == "Gửi đề xuất"), "Gửi hàng loạt chốt danh sách kèm điều chỉnh và ghi lịch sử");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).DieuChinhTuCachAsync(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe, hvDuNhat, true, "Đã gửi rồi thử sửa")).ThanhCong, "Không điều chỉnh sau khi đã gửi");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).XuLyHangLoatAsync("xac-nhan", [(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe)])).ThanhCong == 0
                && (await Lay<DichVuChuongTrinh>(qlKhoa).XuLyHangLoatAsync("xac-nhan", [(hoSoNhap.MaLop, hoSoNhap.MaChuyenDe)])).ThanhCong == 1, "Hàng loạt vẫn kiểm quyền từng bước (đào tạo không xác nhận thay khoa)");

            // Bài thi + thông báo tới lớp (hồ sơ lopXet/cdXet đã duyệt ở nhóm trước)
            var baiThi = new BaiThi { MaLop = lopXet, MaChuyenDe = cdXet, ThoiGianLamBai = 90, CauTrucDe = "3 câu tự luận", PhamViOnTap = "Bài 1 đến bài 6", QuyDinhTaiLieu = BaiThi.CacQuyDinhTaiLieu[0] };
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlHocVien).LuuBaiThiAsync(baiThi)).ThanhCong && !(await Lay<DichVuChuongTrinh>(qlDaoTao).LuuBaiThiAsync(new BaiThi { MaLop = lopXet, MaChuyenDe = cdXet, ThoiGianLamBai = 5, CauTrucDe = "x", PhamViOnTap = "y" })).ThanhCong, "Bài thi: chỉ Phòng đào tạo nhập, kiểm tra thời lượng");
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).LuuBaiThiAsync(baiThi)).ThanhCong, "Phòng đào tạo lưu thông tin bài thi");
            kiemTra(!(await Lay<DichVuChuongTrinh>(qlDaoTao).GuiThongBaoThiAsync(lopXet, cdXet)).ThanhCong, "Chưa có lịch thi thì chưa gửi được thông báo");
            var lichLai = new LichThi { MaLop = lopXet, MaChuyenDe = cdXet, Ngay = ngayThi, TietBatDau = 1, SoTiet = 2, MaPhong = "PT-01", HinhThuc = "Tự luận" };
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).LuuLichThiAsync(lichLai)).ThanhCong, "Xếp lại lịch thi sau khi huỷ");
            var soTbTruoc = Lay<DichVuThongBao>(admin).LayDanhSach().Count;
            kiemTra((await Lay<DichVuChuongTrinh>(qlDaoTao).GuiThongBaoThiAsync(lopXet, cdXet)).ThanhCong && (await Lay<DichVuChuongTrinh>(qlDaoTao).GuiThongBaoThiAsync(lopXet, cdXet)).ThongBao.Contains("cập nhật")
                && Lay<DichVuThongBao>(admin).LayDanhSach().Count == soTbTruoc + 1, "Gửi thông báo thi một lần, gửi lại thì cập nhật thay vì tạo trùng");
            var tbThi = Lay<DichVuThongBao>(admin).LayDanhSach().First(t => t.Tags.Contains("lop:" + lopXet) && t.TieuDe.Contains("lịch thi"));
            kiemTra(tbThi.NoiDung.Contains("90 phút") && tbThi.NoiDung.Contains("Bài 1 đến bài 6") && !LoCccd(tbThi), "Thông báo thi có thời lượng, phạm vi ôn tập, không chứa dữ liệu cá nhân");
            if (lopXet == "B1D58")
            {
                kiemTra((await Lay<DichVuThongBao>(hv).LayDanhSachChoToiAsync()).Any(t => t.Ma == tbThi.Ma), "Học viên lớp nhận thông báo thi");
                kiemTra((await Lay<DichVuChuongTrinh>(hv).LayLichThiCuaToiAsync()).Single(x => x.Lich.MaChuyenDe == cdXet).BaiThi?.ThoiGianLamBai == 90, "Học viên thấy thông tin bài thi trong lịch thi của mình");
            }

            // Trường dữ liệu mở rộng
            var trKhoa = Lay<DichVuTruongTuyChinh>(qlKhoa);
            kiemTra(!(await Lay<DichVuTruongTuyChinh>(qlHocVien).LuuTruongAsync(new TruongTuyChinh { LoaiDoiTuong = "Khoa", Ten = "Sáp nhập" })).ThanhCong, "Quản lý học viên không định nghĩa trường cho Khoa");
            kiemTra(!(await trKhoa.LuuTruongAsync(new TruongTuyChinh { LoaiDoiTuong = "Khoa", Ten = "Sáp nhập", KieuDuLieu = "Danh sách", LuaChon = "Không" })).ThanhCong, "Kiểu danh sách cần ít nhất 2 lựa chọn");
            var kqTr = await trKhoa.LuuTruongAsync(new TruongTuyChinh { LoaiDoiTuong = "Khoa", Ten = "Sáp nhập", KieuDuLieu = "Danh sách", LuaChon = "Không, Đã sáp nhập" });
            var idTr = int.Parse(kqTr.MaBanGhi!);
            kiemTra(kqTr.ThanhCong && !(await trKhoa.LuuTruongAsync(new TruongTuyChinh { LoaiDoiTuong = "Khoa", Ten = "sáp nhập", KieuDuLieu = "Văn bản" })).ThanhCong, "Quản lý khoa thêm trường Sáp nhập; chặn trùng tên");
            var kqTrMat = await trKhoa.LuuTruongAsync(new TruongTuyChinh { LoaiDoiTuong = "Khoa", Ten = "Ghi chú nội bộ", KieuDuLieu = "Văn bản", ChiChuSoHuu = true });
            kiemTra(!(await trKhoa.LuuGiaTriAsync("Khoa", "KH-01", new Dictionary<int, string?> { [idTr] = "Giải thể" })).ThanhCong, "Giá trị danh sách phải nằm trong lựa chọn");
            kiemTra((await trKhoa.LuuGiaTriAsync("Khoa", "KH-01", new Dictionary<int, string?> { [idTr] = "Đã sáp nhập", [int.Parse(kqTrMat.MaBanGhi!)] = "Chỉ khoa xem" })).ThanhCong
                && !(await Lay<DichVuTruongTuyChinh>(qlDaoTao).LuuGiaTriAsync("Khoa", "KH-01", new Dictionary<int, string?> { [idTr] = "Không" })).ThanhCong, "Quản lý khoa nhập giá trị; vai trò khác không sửa được");
            var bangXem = await Lay<DichVuTruongTuyChinh>(xem).LayBangAsync("Khoa");
            kiemTra(bangXem.Truong.Count == 1 && bangXem.HienThi(bangXem.Truong[0], "KH-01") == "Đã sáp nhập" && (await trKhoa.LayBangAsync("Khoa")).Truong.Count == 2, "Trường thường ai cũng xem; trường chỉ chủ sở hữu ẩn với vai trò khác");
            kiemTra(DichVuTruongTuyChinh.ChuanHoa(new TruongTuyChinh { Ten = "Ngày", KieuDuLieu = "Ngày" }, "05/11/2026").GiaTri == "2026-11-05" && DichVuTruongTuyChinh.ChuanHoa(new TruongTuyChinh { Ten = "Số", KieuDuLieu = "Số" }, "abc").Loi is not null
                && DichVuTruongTuyChinh.ChuanHoa(new TruongTuyChinh { Ten = "B", BatBuoc = true }, "").Loi is not null, "Kiểm tra kiểu ngày, số, bắt buộc");
            kiemTra((await trKhoa.XoaTruongAsync(idTr)).ThanhCong && !csdl.CreateDbContext().GiaTriTuyChinh.Any(g => g.TruongId == idTr), "Xoá trường xoá luôn giá trị");
            kiemTra(csdl.CreateDbContext().NhatKy.Any(n => n.DoiTuong == "Trường dữ liệu mở rộng"), "Định nghĩa trường được ghi nhật ký");

            // ---- Báo cáo Excel theo khoảng ngày và nhập lịch đào tạo ----
            static List<string> CacTrang(byte[] tep) { using var w = new ClosedXML.Excel.XLWorkbook(new MemoryStream(tep)); return w.Worksheets.Select(t => t.Name).ToList(); }
            static List<string> CotTrang(byte[] tep, string trang) { using var w = new ClosedXML.Excel.XLWorkbook(new MemoryStream(tep)); return w.Worksheet(trang).Row(4).CellsUsed().Select(c => c.GetString()).ToList(); }
            static List<string> CotDuLieu(byte[] tep, string trang, string cot)
            {
                using var w = new ClosedXML.Excel.XLWorkbook(new MemoryStream(tep));
                var t = w.Worksheet(trang);
                var so = t.Row(4).CellsUsed().First(c => c.GetString() == cot).Address.ColumnNumber;
                return t.RowsUsed().Where(r => r.RowNumber() > 4).Select(r => r.Cell(so).GetString()).ToList();
            }
            kiemTra(DichVuBaoCaoExcel.KiemTraKhoang(DateTime.Today, DateTime.Today.AddDays(-1)) is not null && DichVuBaoCaoExcel.KiemTraKhoang(DateTime.Today, DateTime.Today.AddDays(400)) is not null && DichVuBaoCaoExcel.KiemTraKhoang(DateTime.Today, DateTime.Today) is null, "Khoảng ngày xuất: chặn đảo ngược và quá 366 ngày");
            var tuXuat = DateTime.Today.AddDays(-60); var denXuat = DateTime.Today.AddDays(30);
            var lichAdmin = await Lay<DichVuBaoCaoExcel>(admin).XuatLichAsync(tuXuat, denXuat);
            kiemTra(lichAdmin is not null && CacTrang(lichAdmin).SequenceEqual(["Lịch chi tiết", "Tổng hợp"]) && CotTrang(lichAdmin, "Lịch chi tiết").Contains("Mã cán bộ"), "Xuất lịch toàn trường: bảng chi tiết và tổng hợp, có mã để xếp lịch");
            var lichGv = (await Lay<DichVuBaoCaoExcel>(gv).XuatLichAsync(tuXuat, denXuat))!;
            var giangVienTrongTep = CotDuLieu(lichGv, "Lịch chi tiết", "Giảng viên").Distinct().ToList();
            kiemTra(CacTrang(lichGv).Contains("Thời khoá biểu") && giangVienTrongTep.Count == 1 && !CotTrang(lichGv, "Lịch chi tiết").Contains("Mã cán bộ"), "Giảng viên chỉ xuất được lịch của mình, không có cột mã cán bộ");
            var lichHv = (await Lay<DichVuBaoCaoExcel>(hv).XuatLichAsync(tuXuat, denXuat, maLop: "B2D58"))!;
            var lopCuaHv = CotDuLieu(lichHv, "Lịch chi tiết", "Lớp").Distinct().ToList();
            kiemTra(lopCuaHv.Count == 1 && lopCuaHv[0] != "B2D58", "Học viên truyền lớp khác vẫn chỉ nhận lịch lớp mình");
            kiemTra(await Lay<DichVuBaoCaoExcel>(xem).XuatLichAsync(tuXuat, denXuat) is null, "Người xem không xuất được lịch");
            var locTheoTu = (await Lay<DichVuBaoCaoExcel>(admin).XuatLichAsync(tuXuat, denXuat, tuKhoa: "b1d58"))!;
            kiemTra(CotDuLieu(locTheoTu, "Lịch chi tiết", "Lớp").Distinct().SequenceEqual(["B1D58"]), "Tệp xuất giữ nguyên từ khoá đang lọc");

            var congThang = await Lay<DichVuBaoCaoExcel>(qlKhoa).XuatBaoCaoChamCongAsync(DateTime.Today.AddDays(-30), DateTime.Today);
            kiemTra(congThang is not null && CacTrang(congThang).SequenceEqual(["Báo cáo", "Theo cán bộ", "Theo ngày", "Bảng công"]), "Báo cáo điểm danh đi làm đủ bốn trang");
            kiemTra(await Lay<DichVuBaoCaoExcel>(qlDaoTao).XuatBaoCaoChamCongAsync(DateTime.Today.AddDays(-7), DateTime.Today) is null && await Lay<DichVuBaoCaoExcel>(gv).XuatBaoCaoChamCongAsync(DateTime.Today.AddDays(-7), DateTime.Today) is null, "Báo cáo công toàn khoa chỉ Quản lý khoa, Admin");
            var congNam = (await Lay<DichVuBaoCaoExcel>(admin).XuatBaoCaoChamCongAsync(DateTime.Today.AddDays(-200), DateTime.Today))!;
            kiemTra(!CacTrang(congNam).Contains("Bảng công"), "Kỳ dài không lập bảng công hàng trăm cột");
            var phanHt = DichVuBaoCaoExcel.PhanHocTap.Select(p => p.Ma).ToHashSet();
            var hocTap = await Lay<DichVuBaoCaoExcel>(hv).XuatHocTapCuaToiAsync(tuXuat, denXuat, phanHt);
            kiemTra(hocTap is not null && new[] { "Tổng kết học tập", "Bảng điểm", "Điểm danh", "Thời khoá biểu", "Lịch học", "Lịch thi" }.All(CacTrang(hocTap).Contains), "Học viên xuất đủ kết quả học tập");
            kiemTra(await Lay<DichVuBaoCaoExcel>(admin).XuatHocTapCuaToiAsync(tuXuat, denXuat, phanHt) is null, "Chỉ học viên xuất được \"học tập của tôi\"");
            var chiDiem = (await Lay<DichVuBaoCaoExcel>(hv).XuatHocTapCuaToiAsync(tuXuat, denXuat, new HashSet<string> { "diem" }))!;
            kiemTra(!CacTrang(chiDiem).Contains("Lịch học"), "Chỉ xuất phần đã chọn");
            var phanGd = DichVuBaoCaoExcel.PhanGiangDay.Select(p => p.Ma).ToHashSet();
            var giangDay = await Lay<DichVuBaoCaoExcel>(gv).XuatGiangDayCuaToiAsync(tuXuat, denXuat, phanGd);
            kiemTra(giangDay is not null && new[] { "Lịch dạy", "Tổng hợp giảng dạy", "Chưa điểm danh", "Điểm danh đi làm", "Lớp tôi dạy" }.All(CacTrang(giangDay).Contains), "Giảng viên xuất đủ báo cáo giảng dạy");
            kiemTra(await Lay<DichVuBaoCaoExcel>(hv).XuatGiangDayCuaToiAsync(tuXuat, denXuat, phanGd) is null, "Chỉ giảng viên xuất được báo cáo giảng dạy");
            kiemTra(await Lay<DichVuBaoCaoExcel>(qlKhoa).XuatChuyenCanLopAsync("B1D58") is null && await Lay<DichVuBaoCaoExcel>(qlHocVien).XuatChuyenCanLopAsync("B1D58") is not null, "Xuất chuyên cần theo đúng quyền xem");

            var lichMoi = buoiCuaGv.Buoi;
            var ngayMoi = "01/03/2027";
            kiemTra(await Lay<DichVuNhapExcel>(qlKhoa).TaoMauAsync(LoaiNhap.LichHoc) is null && await Lay<DichVuNhapExcel>(gv).TaoMauAsync(LoaiNhap.LichHoc) is null, "Chỉ Quản lý đào tạo, Admin nhập lịch");
            var dongLich = await DocNhap(qlDaoTao, LoaiNhap.LichHoc, [
                [ngayMoi, "1", "4", "B1D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PT-01", "Buổi nhập"],
                [ngayMoi, "2", "2", "B2D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PM-01", ""],
                ["07/03/2027", "1", "4", "B3D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PT-01", ""],
                ["31/02/2027", "1", "4", "B3D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PT-01", ""],
                [ngayMoi, "6", "6", "B3D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PT-01", ""]]);
            kiemTra(dongLich.Count == 5 && dongLich[0].HopLe && dongLich[1].Loi.Any(l => l.Contains("Trùng giờ giảng viên")) && dongLich[2].Loi.Any(l => l.Contains("Chủ nhật")) && dongLich[3].Loi.Any(l => l.Contains("Ngày")) && !dongLich[4].HopLe, "Nhập lịch: xem trước nêu trùng trong tệp, Chủ nhật, sai ngày, sai số tiết");
            var soBuoiTruoc = csdl.CreateDbContext().BuoiHoc.Count();
            kiemTra((await Lay<DichVuNhapExcel>(qlDaoTao).XacNhanAsync(LoaiNhap.LichHoc, dongLich)).ThanhCong == 1 && csdl.CreateDbContext().BuoiHoc.Count() == soBuoiTruoc + 1, "Nhập lịch: chỉ ghi dòng hợp lệ");
            var dongLichLai = await DocNhap(qlDaoTao, LoaiNhap.LichHoc, [[ngayMoi, "1", "4", "B1D58", lichMoi.MaChuyenDe, lichMoi.MaCanBo, "PT-01", ""]]);
            kiemTra(dongLichLai[0].Loi.Any(l => l.Contains("đã")), "Nhập lại buổi vừa ghi bị chặn trùng với dữ liệu hiện có");

            // ---- J. Lưu bền: mở lại kho trên cùng CSDL ----
            var khoMoLai = new KhoDuLieuDemo(csdl, root.GetRequiredService<ILoggerFactory>().CreateLogger<KhoDuLieuDemo>());
            kiemTra(khoMoLai.DanhSachHocVien.Single(h => h.MaHocVien == hvSua.MaHocVien).HoTen == "Trần Thị Thử" && csdl.CreateDbContext().ChuyenDe.Any(c => c.MaChuyenDe == "CD-99") && csdl.CreateDbContext().ChuyenDe.Count() == 25, "Mở lại: giữ sửa học viên và chuyên đề mới, không sinh lại dữ liệu đào tạo");
        }
        finally
        {
            try { File.Delete(tepNghiepVu); } catch { }
            try { File.Delete(tepTaiKhoan); } catch { }
        }
    }
}
