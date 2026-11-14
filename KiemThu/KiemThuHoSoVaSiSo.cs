// Vai trò tệp: Kiểm thử hồ sơ, chức vụ lãnh đạo, sĩ số và cập nhật dữ liệu đồng thời.
using System.Text.Json;
using Nhom2_BTL.Models;
using Nhom2_BTL.Services;

internal static class KiemThuHoSoVaSiSo
{
    public static async Task ChayAsync(DichVuPhanQuyenDemo quyen, Action<bool, string> dung)
    {
        var kho = new KhoDuLieuDemo();
        var dv = new DichVuQuanLyDuLieuDemo(kho, quyen);
        var soThongBao = 0;
        dv.DuLieuDaThayDoi += () => Interlocked.Increment(ref soThongBao);
        string AnhChup() => JsonSerializer.Serialize(new { khoa = dv.LayDanhSachKhoa(), to = dv.LayDanhSachTo(),
            cb = dv.LayDanhSachCanBo(), hv = dv.LayDanhSachHocVien(), lop = dv.LayDanhSachLop() });
        async Task Loi(Func<Task<KetQuaThaoTac>> luu, string thongBao, string ten)
        {
            var truoc = AnhChup(); var suKienTruoc = soThongBao;
            var kq = await luu();
            dung(!kq.ThanhCong && kq.ThongBao.Contains(thongBao, StringComparison.OrdinalIgnoreCase), ten + " — thông báo đúng");
            dung(AnhChup() == truoc && suKienTruoc == soThongBao, ten + " — hồ sơ, sĩ số và sự kiện không đổi");
        }
        void ThanhCong(KetQuaThaoTac kq, string ten) => dung(kq.ThanhCong, ten + " | " + kq.ThongBao);
        void SoLieuDung(string ten)
        {
            var cb = dv.LayDanhSachCanBo(); var hv = dv.LayDanhSachHocVien(); var tk = dv.LayThongKe();
            dung(dv.LayDanhSachKhoa().All(k => k.SiSo == cb.Count(c => c.MaKhoa == k.MaKhoa)) &&
                dv.LayDanhSachTo().All(t => t.SiSo == cb.Count(c => c.MaTo == t.MaTo)) &&
                dv.LayDanhSachLop().All(l => l.SiSo == hv.Count(h => h.MaLop == l.MaLop)) &&
                dv.LayDanhSachKhoa().Sum(k => k.SiSo) == cb.Count && dv.LayDanhSachTo().Sum(t => t.SiSo) == cb.Count &&
                dv.LayDanhSachLop().Sum(l => l.SiSo) == hv.Count && tk.SoCanBo == cb.Count && tk.SoHocVien == hv.Count,
                ten + " — sĩ số hiển thị khớp hồ sơ và thống kê");
        }
        dung(dv.LayDanhSachKhoa().All(k =>
        {
            var cb = dv.LayDanhSachCanBo().Where(c => c.MaKhoa == k.MaKhoa).ToList();
            return cb.Count(c => c.ChucVu == "Trưởng khoa") == 1 && cb.Count(c => c.ChucVu == "Phó khoa") == 2 &&
                cb.Single(c => c.MaCanBo == k.MaTruongKhoa).ChucVu == "Trưởng khoa" &&
                cb.Single(c => c.MaCanBo == k.MaPhoKhoa).ChucVu == "Phó khoa" &&
                cb.Single(c => c.MaCanBo == k.MaPhoKhoaThuHai).ChucVu == "Phó khoa" && k.MaPhoKhoa != k.MaPhoKhoaThuHai;
        }), "Mỗi khoa mẫu có đúng một trưởng và hai phó khoa, liên kết đúng cán bộ");
        dung(dv.LayDanhSachTo().All(t => dv.LayDanhSachCanBo().Single(c => c.MaCanBo == t.MaToTruong).MaTo == t.MaTo &&
            dv.LayDanhSachCanBo().Single(c => c.MaCanBo == t.MaToPho).MaTo == t.MaTo), "Lãnh đạo tổ trỏ đúng hồ sơ trong tổ");
        dung(dv.LayDanhSachCanBo().Select(c => c.SoCCCD).Concat(dv.LayDanhSachHocVien().Select(h => h.SoCCCD)).Distinct().Count() ==
            dv.LayDanhSachCanBo().Count + dv.LayDanhSachHocVien().Count, "CCCD mẫu duy nhất ở cả học viên và cán bộ");
        SoLieuDung("Khởi tạo");

        var hvGoc = new HocVien { MaHocVien = "KT-HV-1", HoTen = "Nguyễn Văn Kiểm", MaLop = "B10D57", NgaySinh = new(2001, 2, 3),
            SoCCCD = "000099990001", SoDienThoai = "0977000001", DiaChiThuongTru = "Địa chỉ mẫu", QueQuan = "Hà Nội" };
        ThanhCong(await dv.LuuHocVienAsync(hvGoc), "Thêm học viên kiểm tra trùng");
        SoLieuDung("Sau thêm học viên");
        var h = hvGoc.TaoBanSao(); h.MaHocVien = "KT-HV-2";
        await Loi(() => dv.LuuHocVienAsync(h), "trùng toàn bộ", "Học viên trùng mọi trường dù đổi mã");
        h.HoTen = "  nguyễn   văn kiểm ";
        await Loi(() => dv.LuuHocVienAsync(h), "trùng toàn bộ", "Học viên trùng sau chuẩn hóa hoa thường, khoảng trắng");
        h = hvGoc.TaoBanSao(); h.MaHocVien = "KT-HV-2"; h.NgaySinh = h.NgaySinh.AddDays(1);
        await Loi(() => dv.LuuHocVienAsync(h), "thông tin khác không khớp", "Trùng tên, CCCD, SĐT nhưng khác ngày sinh");
        h = hvGoc.TaoBanSao(); h.MaHocVien = "KT-HV-2"; h.HoTen = "Tên Người Khác";
        await Loi(() => dv.LuuHocVienAsync(h), "Số CCCD và Số điện thoại", "Khác tên nhưng trùng cả hai định danh");
        h.SoDienThoai = "0977000002";
        await Loi(() => dv.LuuHocVienAsync(h), "Số CCCD đã", "Học viên chỉ trùng CCCD");
        h.SoCCCD = "000099990002"; h.SoDienThoai = hvGoc.SoDienThoai;
        await Loi(() => dv.LuuHocVienAsync(h), "Số điện thoại đã", "Học viên chỉ trùng SĐT");
        h = hvGoc.TaoBanSao(); h.MaHocVien = "KT-HV-2"; h.SoCCCD = "000099990002"; h.SoDienThoai = "0977000002";
        ThanhCong(await dv.LuuHocVienAsync(h), "Học viên chỉ trùng họ tên được phép thêm");
        ThanhCong(await dv.LuuHocVienAsync(h, h.MaHocVien), "Sửa học viên giữ nguyên định danh của chính mình");
        h.SoDienThoai = hvGoc.SoDienThoai;
        await Loi(() => dv.LuuHocVienAsync(h, h.MaHocVien), "Số điện thoại đã", "Sửa học viên sang SĐT của người khác bị chặn");
        h.SoCCCD = hvGoc.SoCCCD;
        await Loi(() => dv.LuuHocVienAsync(h, h.MaHocVien), "trùng toàn bộ", "Sửa học viên thành bản sao của hồ sơ khác bị chặn");

        var cbGoc = new CanBoGiangVien { MaCanBo = "KT-CB-1", HoTen = "Lê Thị Kiểm", MaKhoa = "KH-11", MaTo = "KH-11-T02",
            SoCCCD = "000099990003", SoDienThoai = "0977000003", ChuyenMon = "Luật", QueQuan = "Hà Nội" };
        ThanhCong(await dv.LuuCanBoAsync(cbGoc), "Thêm cán bộ kiểm tra trùng");
        SoLieuDung("Sau thêm cán bộ");
        var c = cbGoc.TaoBanSao(); c.MaCanBo = "KT-CB-2";
        await Loi(() => dv.LuuCanBoAsync(c), "trùng toàn bộ", "Cán bộ trùng mọi trường dù đổi mã");
        c.HocVi = "Thạc sĩ";
        await Loi(() => dv.LuuCanBoAsync(c), "thông tin khác không khớp", "Trùng tên, CCCD, SĐT cán bộ nhưng khác học vị");
        c.HoTen = "Tên Cán Bộ Khác";
        await Loi(() => dv.LuuCanBoAsync(c), "Số CCCD và Số điện thoại", "Cán bộ khác tên nhưng trùng cả hai định danh");
        c.SoDienThoai = "0977000004";
        await Loi(() => dv.LuuCanBoAsync(c), "Số CCCD đã", "Cán bộ chỉ trùng CCCD");
        c.SoCCCD = "000099990004"; c.SoDienThoai = cbGoc.SoDienThoai;
        await Loi(() => dv.LuuCanBoAsync(c), "Số điện thoại đã", "Cán bộ chỉ trùng SĐT");
        c = cbGoc.TaoBanSao(); c.MaCanBo = "KT-CB-2"; c.SoCCCD = "000099990004"; c.SoDienThoai = "0977000004";
        ThanhCong(await dv.LuuCanBoAsync(c), "Cán bộ chỉ trùng họ tên được phép thêm");
        ThanhCong(await dv.LuuCanBoAsync(c, c.MaCanBo), "Sửa cán bộ giữ nguyên CCCD và SĐT của chính mình");
        c.SoDienThoai = cbGoc.SoDienThoai;
        await Loi(() => dv.LuuCanBoAsync(c, c.MaCanBo), "Số điện thoại đã", "Sửa cán bộ sang SĐT người khác bị chặn");
        c.SoCCCD = cbGoc.SoCCCD;
        await Loi(() => dv.LuuCanBoAsync(c, c.MaCanBo), "trùng toàn bộ", "Sửa cán bộ thành bản sao của người khác bị chặn");
        c = cbGoc.TaoBanSao(); c.MaCanBo = "KT-CB-3"; c.SoCCCD = "123"; c.SoDienThoai = "0977000005";
        await Loi(() => dv.LuuCanBoAsync(c), "12 chữ số", "CCCD cán bộ sai định dạng");
        c.SoCCCD = hvGoc.SoCCCD;
        await Loi(() => dv.LuuCanBoAsync(c), "Số CCCD đã", "CCCD cán bộ không trùng CCCD học viên");
        c.SoCCCD = "000099990005"; c.SoDienThoai = hvGoc.SoDienThoai;
        await Loi(() => dv.LuuCanBoAsync(c), "Số điện thoại đã", "SĐT cán bộ không trùng SĐT học viên");
        h = hvGoc.TaoBanSao(); h.MaHocVien = "KT-HV-3"; h.SoCCCD = cbGoc.SoCCCD; h.SoDienThoai = "0977000005";
        await Loi(() => dv.LuuHocVienAsync(h), "Số CCCD đã", "CCCD học viên không trùng CCCD cán bộ");
        h.SoCCCD = "000099990005"; h.SoDienThoai = cbGoc.SoDienThoai;
        await Loi(() => dv.LuuHocVienAsync(h), "Số điện thoại đã", "SĐT học viên không trùng SĐT cán bộ");
        h.SoCCCD = hvGoc.SoCCCD; h.SoDienThoai = cbGoc.SoDienThoai;
        await Loi(() => dv.LuuHocVienAsync(h), "Số CCCD và Số điện thoại", "Hai định danh trùng hai hồ sơ khác nhau vẫn bị chặn");

        // Kiểm tra trực tiếp các thuộc tính đang hiển thị ở cột sĩ số.
        var lopCu = dv.LayDanhSachLop().Single(x => x.MaLop == hvGoc.MaLop).SiSo;
        var lopDich = dv.LayDanhSachLop().Single(x => x.MaLop == "B1K6").SiSo;
        h = hvGoc.TaoBanSao(); h.MaLop = "B1K6";
        ThanhCong(await dv.LuuHocVienAsync(h, h.MaHocVien), "Chuyển học viên kiểm thử sang hệ khác");
        dung(dv.LayDanhSachLop().Single(x => x.MaLop == hvGoc.MaLop).SiSo == lopCu - 1 &&
            dv.LayDanhSachLop().Single(x => x.MaLop == h.MaLop).SiSo == lopDich + 1, "Cột sĩ số lớp cũ giảm, lớp mới tăng đúng một");
        ThanhCong(await dv.XoaHocVienAsync(h.MaHocVien), "Xóa học viên sau chuyển lớp");
        dung(dv.LayDanhSachLop().Single(x => x.MaLop == h.MaLop).SiSo == lopDich, "Xóa học viên giảm đúng sĩ số lớp mới");
        c = cbGoc.TaoBanSao(); c.MaKhoa = "KH-12"; c.MaTo = "KH-12-T02";
        var khoaCu = dv.LayDanhSachKhoa().Single(x => x.MaKhoa == cbGoc.MaKhoa).SiSo;
        var khoaDich = dv.LayDanhSachKhoa().Single(x => x.MaKhoa == c.MaKhoa).SiSo;
        var toCu = dv.LayDanhSachTo().Single(x => x.MaTo == cbGoc.MaTo).SiSo;
        var toDich = dv.LayDanhSachTo().Single(x => x.MaTo == c.MaTo).SiSo;
        ThanhCong(await dv.LuuCanBoAsync(c, c.MaCanBo), "Chuyển cán bộ kiểm thử sang khoa và tổ khác");
        dung(dv.LayDanhSachKhoa().Single(x => x.MaKhoa == cbGoc.MaKhoa).SiSo == khoaCu - 1 &&
            dv.LayDanhSachKhoa().Single(x => x.MaKhoa == c.MaKhoa).SiSo == khoaDich + 1 &&
            dv.LayDanhSachTo().Single(x => x.MaTo == cbGoc.MaTo).SiSo == toCu - 1 &&
            dv.LayDanhSachTo().Single(x => x.MaTo == c.MaTo).SiSo == toDich + 1, "Cột sĩ số hai khoa và hai tổ thay đổi đúng một");
        ThanhCong(await dv.XoaCanBoAsync(c.MaCanBo), "Xóa cán bộ sau chuyển đơn vị");
        dung(dv.LayDanhSachKhoa().Single(x => x.MaKhoa == c.MaKhoa).SiSo == khoaDich &&
            dv.LayDanhSachTo().Single(x => x.MaTo == c.MaTo).SiSo == toDich, "Xóa cán bộ giảm sĩ số cả tổ và khoa đích");
        SoLieuDung("Sau chuyển và xóa");

        // Cấp đúng hai vị trí phó khoa, đồng bộ khi sửa/xóa/thay thế.
        ThanhCong(await dv.LuuKhoaAsync(new() { MaKhoa = "KT-KHOA", TenKhoa = "Khoa kiểm thử" }), "Tạo khoa kiểm thử lãnh đạo");
        ThanhCong(await dv.LuuToAsync(new() { MaTo = "KT-TO", TenTo = "Tổ kiểm thử", MaKhoa = "KT-KHOA" }), "Tạo tổ kiểm thử lãnh đạo");
        var lanhDao = new List<CanBoGiangVien>();
        for (var i = 1; i <= 4; i++)
        {
            var nguoi = new CanBoGiangVien { MaCanBo = $"KT-LD-{i}", HoTen = $"Người Mẫu {i}", MaKhoa = "KT-KHOA", MaTo = "KT-TO",
                SoCCCD = $"00009999100{i}", SoDienThoai = $"097700010{i}", ChuyenMon = "Mẫu", ChucVu = i == 1 ? "Trưởng khoa" : "Phó khoa" };
            lanhDao.Add(nguoi);
            if (i <= 3) ThanhCong(await dv.LuuCanBoAsync(nguoi), "Phân công " + nguoi.ChucVu + " " + i);
            else await Loi(() => dv.LuuCanBoAsync(nguoi), "đủ 2", "Không cho thêm phó khoa thứ ba");
        }
        var khoa = dv.LayDanhSachKhoa().Single(x => x.MaKhoa == "KT-KHOA");
        dung(khoa.MaTruongKhoa == lanhDao[0].MaCanBo && khoa.MaPhoKhoa == lanhDao[1].MaCanBo &&
            khoa.MaPhoKhoaThuHai == lanhDao[2].MaCanBo && khoa.SiSo == 3, "Ba lãnh đạo có đúng liên kết và sĩ số");
        ThanhCong(await dv.LuuKhoaAsync(khoa, khoa.MaKhoa), "Sửa khoa giữ nguyên phân công");
        dung(dv.LayDanhSachKhoa().Single(x => x.MaKhoa == khoa.MaKhoa).MaPhoKhoaThuHai == lanhDao[2].MaCanBo, "Sửa khoa không mất phó khoa thứ hai");
        lanhDao[2].HoTen = "Họ Tên Đã Sửa";
        ThanhCong(await dv.LuuCanBoAsync(lanhDao[2], lanhDao[2].MaCanBo), "Sửa thông tin phó khoa thứ hai");
        dung(dv.LayTenCanBo(khoa.MaPhoKhoaThuHai) == "Họ Tên Đã Sửa", "Tên lãnh đạo luôn lấy từ hồ sơ cán bộ đã sửa");
        ThanhCong(await dv.XoaCanBoAsync(lanhDao[1].MaCanBo), "Xóa phó khoa thứ nhất");
        khoa = dv.LayDanhSachKhoa().Single(x => x.MaKhoa == khoa.MaKhoa);
        dung(khoa.MaPhoKhoa is null && khoa.MaPhoKhoaThuHai == lanhDao[2].MaCanBo && khoa.SiSo == 2, "Xóa một phó không làm mất phó còn lại");
        ThanhCong(await dv.LuuCanBoAsync(lanhDao[3]), "Thêm phó khoa vào chỗ vừa trống");
        khoa = dv.LayDanhSachKhoa().Single(x => x.MaKhoa == khoa.MaKhoa);
        dung(khoa.MaPhoKhoa == lanhDao[3].MaCanBo && khoa.MaPhoKhoaThuHai == lanhDao[2].MaCanBo, "Phó mới nhận đúng vị trí trống");
        lanhDao[2].ChucVu = "Tổ trưởng";
        ThanhCong(await dv.LuuCanBoAsync(lanhDao[2], lanhDao[2].MaCanBo), "Đổi phó khoa thành tổ trưởng");
        dung(dv.LayDanhSachKhoa().Single(x => x.MaKhoa == khoa.MaKhoa).MaPhoKhoaThuHai is null &&
            dv.LayDanhSachTo().Single(x => x.MaTo == "KT-TO").MaToTruong == lanhDao[2].MaCanBo, "Đổi chức vụ xóa phân công cũ và gán đúng tổ trưởng");
        ThanhCong(await dv.XoaCanBoAsync(lanhDao[2].MaCanBo), "Xóa tổ trưởng sau đổi chức vụ");
        dung(dv.LayDanhSachTo().Single(x => x.MaTo == "KT-TO").MaToTruong is null, "Xóa tổ trưởng không còn liên kết hồ sơ đã xóa");
        SoLieuDung("Sau vòng đời lãnh đạo");

        // Hai phiên lưu đồng thời phải qua cùng khóa, không lọt định danh trùng.
        var nguoiA = new HocVien { MaHocVien = "KT-SONG-1", HoTen = "Người Mẫu Đồng Thời", MaLop = "B10D57",
            SoCCCD = "000099992001", SoDienThoai = "0977000201", DiaChiThuongTru = "Địa chỉ mẫu" };
        var nguoiB = nguoiA.TaoBanSao(); nguoiB.MaHocVien = "KT-SONG-2";
        var ketQua = await Task.WhenAll(Task.Run(() => dv.LuuHocVienAsync(nguoiA)), Task.Run(() => dv.LuuHocVienAsync(nguoiB)));
        dung(ketQua.Count(x => x.ThanhCong) == 1 && dv.LayDanhSachHocVien().Count(x => x.SoCCCD == nguoiA.SoCCCD) == 1,
            "Hai yêu cầu lưu đồng thời chỉ tạo một hồ sơ và tăng sĩ số một lần");
        SoLieuDung("Sau lưu đồng thời");
    }
}

