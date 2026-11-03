using System.ComponentModel.DataAnnotations;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Đọc bản sao của dữ liệu và kiểm tra quyền trước mọi thao tác ghi.</summary>
public sealed class DichVuQuanLyDuLieuDemo(KhoDuLieuDemo kho, DichVuPhanQuyenDemo phanQuyen)
{
    // Các trang đăng ký sự kiện này để số liệu cập nhật giữa các tab, rồi hủy khi rời trang.
    public event Action? DuLieuDaThayDoi { add => kho.DuLieuDaThayDoi += value; remove => kho.DuLieuDaThayDoi -= value; }

    // Chụp thống kê trong cùng một khóa, không lưu bộ đếm riêng và không nối khoa với lớp.
    public ThongKeQuanLy LayThongKe()
    {
        lock (kho.KhoaDongBo)
        {
            var lop = kho.DanhSachLop.ToDictionary(x => x.MaLop);
            var to = kho.DanhSachTo.ToDictionary(x => x.MaTo);
            return new(kho.DanhSachKhoa.Count, to.Count, kho.DanhSachCanBo.Count, lop.Count, kho.DanhSachHocVien.Count,
                kho.DanhSachHocVien.GroupBy(x => x.MaLop).ToDictionary(g => g.Key, g => g.Count()),
                kho.DanhSachHocVien.GroupBy(x => lop[x.MaLop].KhoaHoc).ToDictionary(g => g.Key, g => g.Count()),
                kho.DanhSachHocVien.GroupBy(x => lop[x.MaLop].HeDaoTao).ToDictionary(g => g.Key, g => g.Count()),
                kho.DanhSachCanBo.GroupBy(x => x.MaTo ?? "").ToDictionary(g => g.Key, g => g.Count()),
                kho.DanhSachCanBo.GroupBy(x => to[x.MaTo!].MaKhoa).ToDictionary(g => g.Key, g => g.Count()));
        }
    }

    public IReadOnlyList<Khoa> LayDanhSachKhoa() { lock (kho.KhoaDongBo) return kho.DanhSachKhoa.Select(x => { var b = x.TaoBanSao(); b.SiSo = kho.DanhSachCanBo.Count(c => c.MaKhoa == x.MaKhoa); return b; }).ToList(); }
    public IReadOnlyList<ToChuyenMon> LayDanhSachTo() { lock (kho.KhoaDongBo) return kho.DanhSachTo.Select(x => { var b = x.TaoBanSao(); b.SiSo = kho.DanhSachCanBo.Count(c => c.MaTo == x.MaTo); return b; }).ToList(); }
    public IReadOnlyList<CanBoGiangVien> LayDanhSachCanBo()
    {
        // Kiểm tra một lần cho mỗi lần đọc, không truy vấn tài khoản cho từng hồ sơ.
        var duocXem = phanQuyen.DuocXemThongTinMat("Khoa");
        lock (kho.KhoaDongBo)
        {
            var tenKhoa = kho.DanhSachKhoa.ToDictionary(x => x.MaKhoa, x => x.TenKhoa);
            var tenTo = kho.DanhSachTo.ToDictionary(x => x.MaTo, x => x.TenTo);
            return kho.DanhSachCanBo.Select(x => {
                var b = x.TaoBanSao(); b.MaTraCuuNoiBo = x.MaCanBo;
                b.TenKhoa = tenKhoa.GetValueOrDefault(b.MaKhoa, "Không xác định");
                b.TenTo = tenTo.GetValueOrDefault(b.MaTo ?? "", "Không xác định");
                if (!duocXem) { b.MaCanBo = ""; b.SoDienThoai = ""; b.SoCCCD = ""; b.Email = null; }
                return b;
            }).ToList();
        }
    }
    public IReadOnlyList<LopDaoTao> LayDanhSachLop() { lock (kho.KhoaDongBo) return kho.DanhSachLop.Select(x => { var banSao = x.TaoBanSao(); banSao.SiSo = kho.DanhSachHocVien.Count(h => h.MaLop == x.MaLop); return banSao; }).ToList(); }
    public IReadOnlyList<HocVien> LayDanhSachHocVien()
    {
        var duocXem = phanQuyen.DuocXemThongTinMat("Lop");
        lock (kho.KhoaDongBo) return kho.DanhSachHocVien.Select(x => {
            var b = x.TaoBanSao();
            if (!duocXem) { b.MaHocVien = ""; b.SoDienThoai = ""; b.SoCCCD = ""; b.DiaChiThuongTru = ""; b.Email = null; }
            return b;
        }).ToList();
    }
    public string LayTenKhoa(string? ma) { lock (kho.KhoaDongBo) return kho.DanhSachKhoa.FirstOrDefault(x => x.MaKhoa == ma)?.TenKhoa ?? "Không xác định"; }
    public string LayTenTo(string? ma) { lock (kho.KhoaDongBo) return kho.DanhSachTo.FirstOrDefault(x => x.MaTo == ma)?.TenTo ?? "Không xác định"; }
    public string LayTenLop(string? ma) { lock (kho.KhoaDongBo) return kho.DanhSachLop.FirstOrDefault(x => x.MaLop == ma)?.MaLop ?? "Không xác định"; }
    public string LayTenCanBo(string? ma) { lock (kho.KhoaDongBo) return kho.DanhSachCanBo.FirstOrDefault(x => x.MaCanBo == ma)?.HoTen ?? "Chưa phân công"; }

    public async Task<KetQuaThaoTac> LuuKhoaAsync(Khoa doiTuong, string? maCu = null)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        // Giữ mã định danh ổn định để không làm mất quan hệ của các bản ghi phụ thuộc.
        doiTuong = doiTuong.TaoBanSao();
        doiTuong.MaKhoa = (doiTuong.MaKhoa ?? string.Empty).Trim().ToUpperInvariant();
        if (maCu is not null && doiTuong.MaKhoa != maCu) return new(false, "Không được đổi mã định danh khi sửa.");
        var loi = KiemTra(doiTuong); if (loi is not null) return new(false, loi);
        var ma = doiTuong.MaKhoa.Trim();
        lock (kho.KhoaDongBo)
        {
            if (kho.DanhSachKhoa.Any(x => x.MaKhoa.Equals(ma, StringComparison.OrdinalIgnoreCase) && !x.MaKhoa.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return new(false, "Mã khoa đã tồn tại.");
            var banSao = doiTuong.TaoBanSao(); banSao.MaKhoa = ma; banSao.TenKhoa = doiTuong.TenKhoa.Trim(); banSao.MoTa = doiTuong.MoTa?.Trim() ?? string.Empty;
            if (maCu is null) { banSao.MaTruongKhoa = null; banSao.MaPhoKhoa = null; banSao.MaPhoKhoaThuHai = null; kho.DanhSachKhoa.Add(banSao); }
            else { var viTri = kho.DanhSachKhoa.FindIndex(x => x.MaKhoa.Equals(maCu, StringComparison.OrdinalIgnoreCase)); if (viTri < 0) return NotFound("khoa"); banSao.MaTruongKhoa = kho.DanhSachKhoa[viTri].MaTruongKhoa; banSao.MaPhoKhoa = kho.DanhSachKhoa[viTri].MaPhoKhoa; banSao.MaPhoKhoaThuHai = kho.DanhSachKhoa[viTri].MaPhoKhoaThuHai; kho.DanhSachKhoa[viTri] = banSao; }
        }
        kho.ThongBaoThayDoi();
        return new(true, maCu is null ? "Đã thêm khoa." : "Đã cập nhật khoa.");
    }

    public async Task<KetQuaThaoTac> XoaKhoaAsync(string ma)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        lock (kho.KhoaDongBo)
        {
            if (kho.DanhSachTo.Any(x => x.MaKhoa == ma) || kho.DanhSachCanBo.Any(x => x.MaKhoa == ma)) return new(false, "Không thể xóa khoa còn tổ hoặc cán bộ liên kết.");
            var doiTuong = kho.DanhSachKhoa.FirstOrDefault(x => x.MaKhoa == ma); if (doiTuong is null) return NotFound("khoa");
            kho.DanhSachKhoa.Remove(doiTuong);
        }
        kho.ThongBaoThayDoi();
        return new(true, "Đã xóa khoa.");
    }

    public async Task<KetQuaThaoTac> LuuToAsync(ToChuyenMon doiTuong, string? maCu = null)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        // Giữ mã định danh ổn định để không làm mất quan hệ của các bản ghi phụ thuộc.
        doiTuong = doiTuong.TaoBanSao();
        doiTuong.MaTo = (doiTuong.MaTo ?? string.Empty).Trim().ToUpperInvariant();
        if (maCu is not null && doiTuong.MaTo != maCu) return new(false, "Không được đổi mã định danh khi sửa.");
        var loi = KiemTra(doiTuong); if (loi is not null) return new(false, loi);
        var ma = doiTuong.MaTo.Trim();
        lock (kho.KhoaDongBo)
        {
            if (!kho.DanhSachKhoa.Any(x => x.MaKhoa == doiTuong.MaKhoa)) return new(false, "Khoa được chọn không tồn tại.");
            if (kho.DanhSachTo.Any(x => x.MaTo.Equals(ma, StringComparison.OrdinalIgnoreCase) && !x.MaTo.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return new(false, "Mã tổ đã tồn tại.");
            if (maCu is not null)
            {
                var toHienTai = kho.DanhSachTo.FirstOrDefault(x => x.MaTo.Equals(maCu, StringComparison.OrdinalIgnoreCase));
                if (toHienTai is null) return NotFound("tổ");
                if (toHienTai.MaKhoa != doiTuong.MaKhoa && kho.DanhSachCanBo.Any(x => x.MaTo == maCu)) return new(false, "Không thể chuyển tổ sang khoa khác khi còn cán bộ đang trực thuộc.");
            }
            var banSao = doiTuong.TaoBanSao(); banSao.MaTo = ma; banSao.TenTo = doiTuong.TenTo.Trim();
            if (maCu is null) { banSao.MaToTruong = null; banSao.MaToPho = null; kho.DanhSachTo.Add(banSao); }
            else { var viTri = kho.DanhSachTo.FindIndex(x => x.MaTo.Equals(maCu, StringComparison.OrdinalIgnoreCase)); if (viTri < 0) return NotFound("tổ"); banSao.MaToTruong = kho.DanhSachTo[viTri].MaToTruong; banSao.MaToPho = kho.DanhSachTo[viTri].MaToPho; kho.DanhSachTo[viTri] = banSao; }
        }
        kho.ThongBaoThayDoi();
        return new(true, maCu is null ? "Đã thêm tổ." : "Đã cập nhật tổ.");
    }

    public async Task<KetQuaThaoTac> XoaToAsync(string ma)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        lock (kho.KhoaDongBo)
        {
            if (kho.DanhSachCanBo.Any(x => x.MaTo == ma)) return new(false, "Không thể xóa tổ còn cán bộ liên kết.");
            var doiTuong = kho.DanhSachTo.FirstOrDefault(x => x.MaTo == ma); if (doiTuong is null) return NotFound("tổ");
            kho.DanhSachTo.Remove(doiTuong);
        }
        kho.ThongBaoThayDoi();
        return new(true, "Đã xóa tổ.");
    }

    public async Task<KetQuaThaoTac> LuuCanBoAsync(CanBoGiangVien doiTuong, string? maCu = null)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        // Giữ mã định danh ổn định để không làm mất quan hệ của các bản ghi phụ thuộc.
        doiTuong = doiTuong.TaoBanSao();
        doiTuong.SoCCCD = (doiTuong.SoCCCD ?? "").Trim();
        doiTuong.SoDienThoai = (doiTuong.SoDienThoai ?? "").Trim();
        doiTuong.MaCanBo = (doiTuong.MaCanBo ?? string.Empty).Trim().ToUpperInvariant();
        if (maCu is not null && doiTuong.MaCanBo != maCu) return new(false, "Không được đổi mã định danh khi sửa.");
        var loi = KiemTra(doiTuong); if (loi is not null) return new(false, loi);
        var ma = doiTuong.MaCanBo.Trim();
        lock (kho.KhoaDongBo)
        {
            if (!kho.DanhSachKhoa.Any(x => x.MaKhoa == doiTuong.MaKhoa)) return new(false, "Khoa được chọn không tồn tại.");
            if (string.IsNullOrWhiteSpace(doiTuong.MaTo) || !kho.DanhSachTo.Any(x => x.MaTo == doiTuong.MaTo && x.MaKhoa == doiTuong.MaKhoa)) return new(false, "Tổ không thuộc khoa đã chọn.");
            if (maCu is not null && !kho.DanhSachCanBo.Any(x => x.MaCanBo == maCu)) return NotFound("cán bộ");
            var loiTrung = KiemTraTrungHoSo.KiemTra(doiTuong,
                kho.DanhSachCanBo.Where(x => x.MaCanBo != maCu), kho.DanhSachHocVien);
            if (loiTrung is not null) return new(false, loiTrung);
            if (kho.DanhSachCanBo.Any(x => x.MaCanBo.Equals(ma, StringComparison.OrdinalIgnoreCase) && !x.MaCanBo.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return new(false, "Mã cán bộ đã tồn tại.");
            if (!new[] { "Trưởng khoa", "Phó khoa", "Tổ trưởng", "Tổ phó", "Cán bộ giảng viên" }.Contains(doiTuong.ChucVu)) return new(false, "Chức vụ cán bộ không hợp lệ.");
            if ((doiTuong.ChucVu is "Tổ trưởng" or "Tổ phó") && doiTuong.MaTo is null) return new(false, "Vui lòng chọn tổ cho chức vụ tổ trưởng hoặc tổ phó.");
            if (doiTuong.ChucVu is "Trưởng khoa" or "Phó khoa" or "Tổ trưởng" or "Tổ phó")
            {
                var gioiHan = doiTuong.ChucVu == "Phó khoa" ? 2 : 1;
                var soNguoi = kho.DanhSachCanBo.Count(x => x.ChucVu == doiTuong.ChucVu &&
                    (doiTuong.ChucVu is "Trưởng khoa" or "Phó khoa" ? x.MaKhoa == doiTuong.MaKhoa : x.MaTo == doiTuong.MaTo) && x.MaCanBo != maCu);
                if (soNguoi >= gioiHan) return new(false, $"Đơn vị đã đủ {gioiHan} vị trí {doiTuong.ChucVu}; hãy cập nhật hồ sơ đang giữ vị trí đó.");
            }
            var banSao = doiTuong.TaoBanSao(); banSao.MaCanBo = ma; banSao.HoTen = doiTuong.HoTen.Trim(); banSao.ChuyenMon = doiTuong.ChuyenMon.Trim();
            if (!banSao.SoDienThoai.All(char.IsDigit)) return new(false, "Số điện thoại chỉ được chứa chữ số.");
            banSao.TaoLuc = maCu is null ? DateTimeOffset.UtcNow : kho.DanhSachCanBo.FirstOrDefault(x => x.MaCanBo == maCu)?.TaoLuc;
            banSao.CapNhatLuc = maCu is null ? null : DateTimeOffset.UtcNow;
            if (maCu is null) kho.DanhSachCanBo.Add(banSao);
            else { var viTri = kho.DanhSachCanBo.FindIndex(x => x.MaCanBo.Equals(maCu, StringComparison.OrdinalIgnoreCase)); if (viTri < 0) return NotFound("cán bộ"); XoaLienKetLanhDao(maCu); kho.DanhSachCanBo[viTri] = banSao; }
            GanLanhDao(banSao);
        }
        kho.ThongBaoThayDoi();
        return new(true, maCu is null ? $"Đã thêm cán bộ vào {LayTenTo(doiTuong.MaTo)}, {LayTenKhoa(doiTuong.MaKhoa)}." : "Đã cập nhật thông tin và đơn vị công tác của cán bộ.", ma);
    }

    public async Task<KetQuaThaoTac> XoaCanBoAsync(string ma)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Khoa")) return TuChoi();
        lock (kho.KhoaDongBo)
        {

            var doiTuong = kho.DanhSachCanBo.FirstOrDefault(x => x.MaCanBo == ma); if (doiTuong is null) return NotFound("cán bộ");
            // Khi xóa lãnh đạo, bỏ phân công và để trống vị trí; không tự chọn người thay thế.
            XoaLienKetLanhDao(ma);
            kho.DanhSachCanBo.Remove(doiTuong);
        }
        kho.ThongBaoThayDoi();
        return new(true, "Đã xóa cán bộ, giảng viên.");
    }

    public async Task<KetQuaThaoTac> LuuLopAsync(LopDaoTao doiTuong, string? maCu = null)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Lop")) return TuChoi();
        // Giữ mã định danh ổn định để không làm mất quan hệ của các bản ghi phụ thuộc.
        doiTuong = doiTuong.TaoBanSao();
        doiTuong.MaLop = (doiTuong.MaLop ?? string.Empty).Trim().ToUpperInvariant();
        if (maCu is not null && doiTuong.MaLop != maCu) return new(false, "Không được đổi mã định danh khi sửa.");
        var loi = KiemTra(doiTuong); if (loi is not null) return new(false, loi);
        var ma = doiTuong.MaLop.Trim().ToUpperInvariant();
        if (!QuyTacDuLieu.LopHopLe(ma, doiTuong.HeDaoTao, doiTuong.KhoaHoc, doiTuong.ChuyenNganh)) return new(false, "Mã lớp, hệ, khóa hoặc chuyên ngành không khớp quy tắc đào tạo.");
        lock (kho.KhoaDongBo)
        {
            if (kho.DanhSachLop.Any(x => x.MaLop.Equals(ma, StringComparison.OrdinalIgnoreCase) && !x.MaLop.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return new(false, "Mã lớp đã tồn tại.");
            if (maCu is not null)
            {
                if (!kho.DanhSachLop.Any(x => x.MaLop.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return NotFound("lớp");
                if (!ma.Equals(maCu, StringComparison.OrdinalIgnoreCase) && kho.DanhSachHocVien.Any(x => x.MaLop == maCu)) return new(false, "Không thể đổi mã lớp khi đã có học viên liên kết.");
            }
            var banSao = doiTuong.TaoBanSao(); banSao.MaLop = ma; banSao.SiSo = 0; // Sĩ số được tính lại khi đọc.
            if (maCu is null) kho.DanhSachLop.Add(banSao);
            else { var viTri = kho.DanhSachLop.FindIndex(x => x.MaLop.Equals(maCu, StringComparison.OrdinalIgnoreCase)); if (viTri < 0) return NotFound("lớp"); kho.DanhSachLop[viTri] = banSao; }
        }
        kho.ThongBaoThayDoi();
        return new(true, maCu is null ? "Đã thêm lớp." : "Đã cập nhật lớp.");
    }

    public async Task<KetQuaThaoTac> XoaLopAsync(string ma)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Lop")) return TuChoi();
        lock (kho.KhoaDongBo)
        {
            if (kho.DanhSachHocVien.Any(x => x.MaLop == ma)) return new(false, "Không thể xóa lớp đang có học viên liên kết.");
            var doiTuong = kho.DanhSachLop.FirstOrDefault(x => x.MaLop == ma); if (doiTuong is null) return NotFound("lớp");
            kho.DanhSachLop.Remove(doiTuong);
        }
        kho.ThongBaoThayDoi();
        return new(true, "Đã xóa lớp.");
    }

    public async Task<KetQuaThaoTac> LuuHocVienAsync(HocVien doiTuong, string? maCu = null)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Lop")) return TuChoi();
        // Giữ mã định danh ổn định để không làm mất quan hệ của các bản ghi phụ thuộc.
        doiTuong = doiTuong.TaoBanSao();
        doiTuong.SoCCCD = (doiTuong.SoCCCD ?? "").Trim();
        doiTuong.SoDienThoai = (doiTuong.SoDienThoai ?? "").Trim();
        doiTuong.MaHocVien = (doiTuong.MaHocVien ?? string.Empty).Trim().ToUpperInvariant();
        if (maCu is not null && doiTuong.MaHocVien != maCu) return new(false, "Không được đổi mã định danh khi sửa.");
        var loi = KiemTra(doiTuong); if (loi is not null) return new(false, loi);
        var ma = doiTuong.MaHocVien.Trim().ToUpperInvariant();
        if (!doiTuong.SoDienThoai.All(char.IsDigit) || !doiTuong.SoCCCD.All(char.IsDigit)) return new(false, "Số điện thoại và Số CCCD chỉ được chứa chữ số.");
        lock (kho.KhoaDongBo)
        {
            if (!kho.DanhSachLop.Any(x => x.MaLop == doiTuong.MaLop)) return new(false, "Lớp học viên chọn không tồn tại.");
            if (maCu is not null && !kho.DanhSachHocVien.Any(x => x.MaHocVien.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return NotFound("học viên");
            var loiTrung = KiemTraTrungHoSo.KiemTra(doiTuong,
                kho.DanhSachHocVien.Where(x => x.MaHocVien != maCu), kho.DanhSachCanBo);
            if (loiTrung is not null) return new(false, loiTrung);
            if (kho.DanhSachHocVien.Any(x => x.MaHocVien.Equals(ma, StringComparison.OrdinalIgnoreCase) && !x.MaHocVien.Equals(maCu, StringComparison.OrdinalIgnoreCase))) return new(false, "Mã học viên đã tồn tại.");

            // Chỉ giới hạn số người giữ chức vụ, không giới hạn tổng học viên ở mức 40.
            if (doiTuong.ChucVu != "Học viên" && !QuyTacDuLieu.ChiTieuChucVu.ContainsKey(doiTuong.ChucVu)) return new(false, "Chức vụ học viên không hợp lệ.");
            if (QuyTacDuLieu.ChiTieuChucVu.TryGetValue(doiTuong.ChucVu, out var chiTieu) && kho.DanhSachHocVien.Count(x => x.MaLop == doiTuong.MaLop && x.ChucVu == doiTuong.ChucVu && x.MaHocVien != maCu) >= chiTieu) return new(false, $"Lớp {doiTuong.MaLop} đã đủ {chiTieu} chức vụ {doiTuong.ChucVu}.");
            var banSao = doiTuong.TaoBanSao(); banSao.TaoLuc = maCu is null ? DateTimeOffset.UtcNow : kho.DanhSachHocVien.First(x => x.MaHocVien == maCu).TaoLuc;
            banSao.CapNhatLuc = maCu is null ? null : DateTimeOffset.UtcNow;
            banSao.MaHocVien = ma; banSao.HoTen = doiTuong.HoTen.Trim();
            if (maCu is null) kho.DanhSachHocVien.Add(banSao);
            else { var viTri = kho.DanhSachHocVien.FindIndex(x => x.MaHocVien.Equals(maCu, StringComparison.OrdinalIgnoreCase)); if (viTri < 0) return NotFound("học viên"); kho.DanhSachHocVien[viTri] = banSao; }
        }
        kho.ThongBaoThayDoi();
        return new(true, maCu is null ? $"Đã thêm học viên vào lớp {doiTuong.MaLop}." : $"Đã cập nhật học viên tại lớp {doiTuong.MaLop}.", ma);
    }

    public async Task<KetQuaThaoTac> XoaHocVienAsync(string ma)
    {
        if (!await phanQuyen.CoQuyenVungAsync("Lop")) return TuChoi();
        lock (kho.KhoaDongBo)
        {
            var doiTuong = kho.DanhSachHocVien.FirstOrDefault(x => x.MaHocVien == ma); if (doiTuong is null) return NotFound("học viên");
            kho.DanhSachHocVien.Remove(doiTuong);
        }
        kho.ThongBaoThayDoi();
        return new(true, "Đã xóa học viên.");
    }

    private void XoaLienKetLanhDao(string maCanBo)
    {
        foreach (var khoa in kho.DanhSachKhoa)
        {
            if (khoa.MaTruongKhoa == maCanBo) khoa.MaTruongKhoa = null;
            if (khoa.MaPhoKhoa == maCanBo) khoa.MaPhoKhoa = null;
            if (khoa.MaPhoKhoaThuHai == maCanBo) khoa.MaPhoKhoaThuHai = null;
        }
        foreach (var to in kho.DanhSachTo)
        {
            if (to.MaToTruong == maCanBo) to.MaToTruong = null;
            if (to.MaToPho == maCanBo) to.MaToPho = null;
        }
    }

    private void GanLanhDao(CanBoGiangVien canBo)
    {
        if (canBo.ChucVu == "Trưởng khoa") { var khoa = kho.DanhSachKhoa.First(x => x.MaKhoa == canBo.MaKhoa); khoa.MaTruongKhoa = canBo.MaCanBo; }
        if (canBo.ChucVu == "Phó khoa")
        {
            var khoa = kho.DanhSachKhoa.First(x => x.MaKhoa == canBo.MaKhoa);
            if (khoa.MaPhoKhoa is null) khoa.MaPhoKhoa = canBo.MaCanBo;
            else khoa.MaPhoKhoaThuHai = canBo.MaCanBo;
        }
        if (canBo.ChucVu == "Tổ trưởng" && canBo.MaTo is not null) { var to = kho.DanhSachTo.First(x => x.MaTo == canBo.MaTo); to.MaToTruong = canBo.MaCanBo; }
        if (canBo.ChucVu == "Tổ phó" && canBo.MaTo is not null) { var to = kho.DanhSachTo.First(x => x.MaTo == canBo.MaTo); to.MaToPho = canBo.MaCanBo; }
    }

    private static string? KiemTra<T>(T doiTuong)
    {
        // Kiểm tra ở service ngay cả khi giao diện bị bỏ qua.
        var ngaySinh = doiTuong switch { HocVien h => h.NgaySinh, CanBoGiangVien c => c.NgaySinh, _ => (DateTime?)null };
        if (ngaySinh is not null && (ngaySinh > DateTime.Today || ngaySinh < new DateTime(1900, 1, 1))) return "Ngày sinh phải từ năm 1900 đến hôm nay.";
        var gioiTinh = doiTuong switch { HocVien h => h.GioiTinh, CanBoGiangVien c => c.GioiTinh, _ => "Nam" };
        if (gioiTinh is not ("Nam" or "Nữ")) return "Giới tính không hợp lệ.";
        var loi = new List<ValidationResult>();
        return Validator.TryValidateObject(doiTuong!, new ValidationContext(doiTuong!), loi, true) ? null : loi.FirstOrDefault()?.ErrorMessage ?? "Dữ liệu không hợp lệ.";
    }

    private static KetQuaThaoTac TuChoi() => new(false, "Tài khoản hiện tại không có quyền thay đổi dữ liệu.");
    private static KetQuaThaoTac NotFound(string ten) => new(false, $"Không tìm thấy {ten} cần thao tác.");
}







