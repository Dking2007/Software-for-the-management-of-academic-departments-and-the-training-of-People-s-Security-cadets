using System.Globalization;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>Kho dữ liệu giả được khởi tạo một lần và dùng chung trong tiến trình hiện tại.</summary>
public sealed class KhoDuLieuDemo
{
    // Thông báo các tab làm mới bản sao sau khi giao dịch đã hoàn tất.
    public event Action? DuLieuDaThayDoi;
    public void ThongBaoThayDoi() => DuLieuDaThayDoi?.Invoke();
    public object KhoaDongBo { get; } = new();
    public List<Khoa> DanhSachKhoa { get; }
    public List<ToChuyenMon> DanhSachTo { get; }
    public List<CanBoGiangVien> DanhSachCanBo { get; }
    public List<LopDaoTao> DanhSachLop { get; }
    public List<HocVien> DanhSachHocVien { get; }

    public KhoDuLieuDemo()
    {
        DanhSachKhoa = TaoKhoa();
        DanhSachTo = TaoTo(DanhSachKhoa);
        DanhSachCanBo = TaoCanBo(DanhSachKhoa, DanhSachTo);
        DanhSachLop = TaoLop();
        DanhSachHocVien = TaoHocVien(DanhSachLop);
        GanNguoiPhuTrach();
    }

    private void GanNguoiPhuTrach()
    {
        foreach (var khoa in DanhSachKhoa)
        {
            khoa.MaTruongKhoa = DanhSachCanBo.First(canBo => canBo.MaKhoa == khoa.MaKhoa && canBo.ChucVu == "Trưởng khoa").MaCanBo;
            var cacPhoKhoa = DanhSachCanBo.Where(canBo => canBo.MaKhoa == khoa.MaKhoa && canBo.ChucVu == "Phó khoa").ToList();
            khoa.MaPhoKhoa = cacPhoKhoa[0].MaCanBo;
            khoa.MaPhoKhoaThuHai = cacPhoKhoa[1].MaCanBo;
        }
        foreach (var to in DanhSachTo)
        {
            to.MaToTruong = DanhSachCanBo.First(canBo => canBo.MaTo == to.MaTo && canBo.ChucVu == "Tổ trưởng").MaCanBo;
            to.MaToPho = DanhSachCanBo.First(canBo => canBo.MaTo == to.MaTo && canBo.ChucVu == "Tổ phó").MaCanBo;
        }
    }

    private static List<Khoa> TaoKhoa()
    {
        var tenKhoa = new[]
        {
            "Khoa Lý luận chính trị, Khoa học xã hội và nhân văn",
            "Khoa Quân sự - Võ thuật - Thể dục thể thao",
            "Khoa Nghiệp vụ cơ bản",
            "Khoa Nghiệp vụ 2",
            "Khoa Nghiệp vụ 3",
            "Khoa Nghiệp vụ 4",
            "Khoa Nghiệp vụ 5",
            "Khoa Nghiệp vụ 6",
            "Khoa Nghiệp vụ 7",
            "Khoa Nghiệp vụ 8",
            "Khoa Luật",
            "Khoa Ngoại ngữ"
        };
        return tenKhoa.Select((ten, viTri) => new Khoa { MaKhoa = $"KH-{viTri + 1:00}", TenKhoa = ten, MoTa = "Thông tin khoa mẫu được dựng theo sơ đồ quản lý." }).ToList();
    }

    // Mỗi khoa có hai tổ minh họa; người quản trị có thể bổ sung số tổ theo thực tế.
    private static List<ToChuyenMon> TaoTo(IReadOnlyList<Khoa> danhSachKhoa)
        => danhSachKhoa.SelectMany(khoa => Enumerable.Range(1, 2).Select(so => new ToChuyenMon
        {
            MaTo = $"{khoa.MaKhoa}-T{so:00}",
            TenTo = $"Tổ {so}",
            MaKhoa = khoa.MaKhoa
        })).ToList();

    private static List<CanBoGiangVien> TaoCanBo(IReadOnlyList<Khoa> danhSachKhoa, IReadOnlyList<ToChuyenMon> danhSachTo)
    {
        var capBac = new[] { "Đại tá", "Thượng tá", "Trung tá", "Thiếu tá", "Đại úy" };
        var hocVi = new[] { "Tiến sĩ", "Thạc sĩ", "Cử nhân" };
        var chuyenMon = new[] { "Lý luận chính trị", "Quân sự - võ thuật", "Nghiệp vụ cơ bản", "Điều tra hình sự", "An ninh mạng", "Luật", "Ngoại ngữ" };
        var danhSach = new List<CanBoGiangVien>();
        var maSo = 1;


        foreach (var khoa in danhSachKhoa)
        {
            danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, danhSachTo.First(t => t.MaKhoa == khoa.MaKhoa).MaTo, "Trưởng khoa"));
            danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, danhSachTo.First(t => t.MaKhoa == khoa.MaKhoa).MaTo, "Phó khoa"));
            danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, danhSachTo.Last(t => t.MaKhoa == khoa.MaKhoa).MaTo, "Phó khoa"));
            foreach (var to in danhSachTo.Where(to => to.MaKhoa == khoa.MaKhoa))
            {
                danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, to.MaTo, "Tổ trưởng"));
                danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, to.MaTo, "Tổ phó"));
                danhSach.Add(TaoCanBo(maSo++, khoa.MaKhoa, to.MaTo, "Cán bộ giảng viên"));
            }
        }
        return danhSach;

        CanBoGiangVien TaoCanBo(int so, string maKhoa, string? maTo, string chucVu) => new()
        {
            MaCanBo = $"CB{so:000}",
            GioiTinh = so % 2 == 0 ? "Nữ" : "Nam",
            HoTen = QuyTacDuLieu.TaoHoTen(5000 + so, so % 2 == 0 ? "Nữ" : "Nam"),
            Email = $"cb{so:000}@hocvien.example.invalid",
            ChucVu = chucVu,
            CapBacHam = capBac[(so - 1) % capBac.Length],
            HocHam = so % 9 == 0 ? "Giáo sư" : so % 4 == 0 ? "Phó giáo sư" : string.Empty,
            HocVi = hocVi[(so - 1) % hocVi.Length],
            NgaySinh = new DateTime(1968 + so % 27, 1 + so % 12, 1 + so % 27),
            QueQuan = QuyTacDuLieu.TinhThanh[(so - 1) % 34],
            ChuyenMon = chuyenMon[(so - 1) % chuyenMon.Length],
            SoDienThoai = $"09{50000000 + so:00000000}",
            SoCCCD = $"000001{so:000000}",
            MaKhoa = maKhoa,
            MaTo = maTo
        };
    }

    private static List<LopDaoTao> TaoLop()
    {
        var danhSach = new List<LopDaoTao>();
        for (var khoa = 58; khoa >= 53; khoa--)
        {
            for (var soLop = 1; soLop <= 15; soLop++)
            {
                var chuyenNganh = soLop switch
                {
                    <= 3 => "Trinh sát an ninh",
                    <= 7 => "Điều tra hình sự",
                    8 => "Quản lý xuất nhập cảnh",
                    <= 12 => "ANM & PCTPCNC",
                    _ => "Công nghệ thông tin"
                };
                danhSach.Add(new LopDaoTao { MaLop = $"B{soLop}D{khoa}", HeDaoTao = "Chính quy", KhoaHoc = $"D{khoa}", ChuyenNganh = chuyenNganh });
            }
        }
        for (var khoa = 6; khoa >= 3; khoa--)
        {
            for (var soLop = 1; soLop <= 4; soLop++)
            {
                danhSach.Add(new LopDaoTao
                {
                    MaLop = $"B{soLop}K{khoa}",
                    HeDaoTao = "Văn bằng 2",
                    KhoaHoc = $"K{khoa}",
                    ChuyenNganh = soLop % 2 == 1 ? "Điều tra" : "ANM & PCTPCNC",
                });
            }
        }
        return danhSach;
    }

    private static List<HocVien> TaoHocVien(IReadOnlyList<LopDaoTao> danhSachLop)
    {
        var chucVu = new[] { "B trưởng", "B phó", "B phó", "A trưởng", "A trưởng", "A trưởng", "A trưởng", "A phó", "A phó", "A phó", "A phó", "BTĐ", "PBT", "UVĐ", "UVĐ", "UVĐ" };
        var gioiTinh = new[] { "Nam", "Nữ" };
        var danhSach = new List<HocVien>(danhSachLop.Count * 40);
        var so = 1;
        var culture = CultureInfo.GetCultureInfo("vi-VN");

        foreach (var lop in danhSachLop)
        {
            var cacTenTrongLop = Enumerable.Range(0, 40).Select(thuTu =>
            {
                var ma = so + thuTu;
                return new HocVien
                {
                    MaHocVien = $"HV{ma:00000}",
                    HoTen = QuyTacDuLieu.TaoHoTen(ma, gioiTinh[ma % gioiTinh.Length]),
                    Email = $"hv{ma:00000}@hocvien.example.invalid",
                    MaLop = lop.MaLop,
                    ChucVu = thuTu < chucVu.Length ? chucVu[thuTu] : "Học viên",
                    NgaySinh = new DateTime(1998 + ma % 8, 1 + ma % 12, 1 + ma % 27),
                    GioiTinh = gioiTinh[ma % gioiTinh.Length],
                    QueQuan = QuyTacDuLieu.TinhThanh[(ma - 1) % 34],
                    SoDienThoai = $"09{ma:00000000}",
                    SoCCCD = $"00000000{ma:0000}",
                    DiaChiThuongTru = $"Số {ma % 100 + 1}, đường Số {ma % 30 + 1}, {QuyTacDuLieu.TinhThanh[(ma - 1) % 34]}"
                };
            }).OrderBy(hocVien => hocVien.HoTen, StringComparer.Create(culture, true)).ToList();

            danhSach.AddRange(cacTenTrongLop);
            so += 40;
        }
        return danhSach;
    }
}

