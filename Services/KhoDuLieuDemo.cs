using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Kho dữ liệu nghiệp vụ dùng chung trong tiến trình.
/// Khi chạy ứng dụng: nạp từ SQLite lúc khởi động (lần đầu thì sinh dữ liệu mẫu rồi lưu),
/// sau mỗi thao tác ghi thì đồng bộ phần thay đổi xuống CSDL.
/// Khi khởi tạo không tham số (bộ kiểm thử): chỉ sinh dữ liệu mẫu trong bộ nhớ, không chạm CSDL.
/// </summary>
public sealed class KhoDuLieuDemo
{
    // Thông báo các tab làm mới bản sao sau khi giao dịch đã hoàn tất.
    public event Action? DuLieuDaThayDoi;
    public object KhoaDongBo { get; } = new();
    public List<Khoa> DanhSachKhoa { get; private set; }
    public List<ToChuyenMon> DanhSachTo { get; private set; }
    public List<CanBoGiangVien> DanhSachCanBo { get; private set; }
    public List<LopDaoTao> DanhSachLop { get; private set; }
    public List<HocVien> DanhSachHocVien { get; private set; }

    private readonly IDbContextFactory<CoSoDuLieuNghiepVu>? nhaMayCsdl;
    private readonly ILogger<KhoDuLieuDemo>? nhatKy;
    private readonly AnhChupDaLuu anhChup = new();

    /// <summary>Kho thuần bộ nhớ cho bộ kiểm thử.</summary>
    public KhoDuLieuDemo()
    {
        (DanhSachKhoa, DanhSachTo, DanhSachCanBo, DanhSachLop, DanhSachHocVien) = SinhDuLieuMau();
    }

    /// <summary>Kho lưu bền trong SQLite, dùng khi chạy ứng dụng.</summary>
    public KhoDuLieuDemo(IDbContextFactory<CoSoDuLieuNghiepVu> nhaMayCsdl, ILogger<KhoDuLieuDemo> nhatKy)
    {
        this.nhaMayCsdl = nhaMayCsdl;
        this.nhatKy = nhatKy;
        using var db = nhaMayCsdl.CreateDbContext();
        db.DamBaoCauTruc();
        var dongHo = System.Diagnostics.Stopwatch.StartNew();
        if (!db.Khoa.Any())
        {
            (DanhSachKhoa, DanhSachTo, DanhSachCanBo, DanhSachLop, DanhSachHocVien) = SinhDuLieuMau();
            DongBoXuongCsdl(null, null, boQuaNhatKy: true);
            nhatKy.LogInformation("Đã sinh và lưu dữ liệu mẫu vào CSDL trong {Ms} ms.", dongHo.ElapsedMilliseconds);
        }
        else
        {
            (DanhSachKhoa, DanhSachTo, DanhSachCanBo, DanhSachLop, DanhSachHocVien) = NapTuCsdl(db);
            ChupLaiTrangThaiDaLuu();
        }
        // Bổ nhiệm ban đầu cho lãnh đạo hiện hành (ngày bổ nhiệm giả lập, ổn định theo mã cán bộ).
        if (!db.BoNhiem.Any())
        {
            db.BoQuaNhatKy = true;
            db.BoNhiem.AddRange(CacViTri(DanhSachKhoa, DanhSachTo).Select(v => new BoNhiem
            {
                MaCanBo = v.MaCanBo, ChucVu = v.ChucVu, LoaiDonVi = v.LoaiDonVi, MaDonVi = v.MaDonVi,
                TuNgay = new DateTime(2019 + v.MaCanBo.Sum(c => c) % 6, 9, 1)
            }));
            db.SaveChanges();
            db.BoQuaNhatKy = false;
        }
        if (!db.TinTuc.Any())
        {
            db.BoQuaNhatKy = true;
            db.TinTuc.AddRange(DichVuBanTin.BaiMau(DateTime.Today));
            db.SaveChanges();
            db.BoQuaNhatKy = false;
        }
        DichVuBanTin.NangCapBaiMau(db);
        // Dữ liệu đào tạo (chuyên đề, phòng, lịch, điểm danh, điểm) sinh riêng để CSDL cũ cũng được bổ sung.
        if (!db.ChuyenDe.Any())
        {
            dongHo.Restart();
            db.BoQuaNhatKy = true;
            KhoiTaoDaoTaoMau.Tao(db, DanhSachKhoa, DanhSachCanBo, DanhSachLop, DanhSachHocVien, DateTime.Today);
            nhatKy.LogInformation("Đã sinh dữ liệu đào tạo mẫu trong {Ms} ms.", dongHo.ElapsedMilliseconds);
        }
        // Phân công giảng dạy ban đầu suy ra từ lịch đã có (lớp mới về sau phân công trên trang Chương trình).
        db.BoQuaNhatKy = false;
        DichVuChuongTrinh.KhoiTaoPhanCong(db);
        DichVuChuongTrinh.KhoiTaoChamCong(db, DanhSachCanBo, DateTime.Today);
        DichVuHocVu.KhoiTaoSoDauBai(db, DateTime.Today);
        KhoiTaoDaoTaoMau.BoSungLichLopNamCuoi(db, DanhSachLop, DanhSachHocVien, DateTime.Today);
        if (!db.KetQuaLuuTru.Any())
        {
            dongHo.Restart();
            KhoiTaoDaoTaoMau.TaoKetQuaLuuTru(db, DanhSachLop, DanhSachHocVien, DateTime.Today);
            nhatKy.LogInformation("Đã sinh kết quả học tập các học kỳ trước trong {Ms} ms.", dongHo.ElapsedMilliseconds);
        }
    }

    /// <summary>Báo các tab làm mới khi dữ liệu đào tạo/học vụ (lưu thẳng vào CSDL) thay đổi.</summary>
    public void BaoThayDoiNgoai() => DuLieuDaThayDoi?.Invoke();

    /// <summary>Mã các lớp mà cán bộ có buổi dạy trong lịch (rỗng ở kho thuần bộ nhớ).</summary>
    public IReadOnlySet<string> LopGiangVienDay(string maCanBo)
    {
        if (nhaMayCsdl is null) return new HashSet<string>();
        using var db = nhaMayCsdl.CreateDbContext();
        return db.BuoiHoc.Where(b => b.MaCanBo == maCanBo).Select(b => b.MaLop).Distinct().ToHashSet();
    }

    /// <summary>Mã các cán bộ có buổi dạy lớp này trong lịch (rỗng ở kho thuần bộ nhớ).</summary>
    public IReadOnlySet<string> GiangVienDayLop(string maLop)
    {
        if (nhaMayCsdl is null) return new HashSet<string>();
        using var db = nhaMayCsdl.CreateDbContext();
        return db.BuoiHoc.Where(b => b.MaLop == maLop).Select(b => b.MaCanBo).Distinct().ToHashSet();
    }

    /// <summary>
    /// Lý do không được xoá bản ghi vì còn dữ liệu đào tạo/học vụ tham chiếu; null nếu xoá được.
    /// Kho thuần bộ nhớ (kiểm thử) không có dữ liệu đào tạo nên luôn trả null.
    /// </summary>
    public string? LyDoKhongXoa(string loai, string ma)
    {
        if (nhaMayCsdl is null) return null;
        using var db = nhaMayCsdl.CreateDbContext();
        return loai switch
        {
            "HocVien" => DemVaNoi((db.DiemDanh.Count(x => x.MaHocVien == ma), "dòng điểm danh"), (db.KetQua.Count(x => x.MaHocVien == ma), "kết quả học tập"), "Học viên"),
            "Lop" => DemVaNoi((db.BuoiHoc.Count(x => x.MaLop == ma), "buổi học trong lịch"), (0, ""), "Lớp"),
            "CanBo" => DemVaNoi((db.BuoiHoc.Count(x => x.MaCanBo == ma), "buổi giảng dạy trong lịch"), (0, ""), "Cán bộ"),
            "Khoa" => DemVaNoi((db.ChuyenDe.Count(x => x.MaKhoa == ma), "chuyên đề phụ trách"), (0, ""), "Khoa"),
            _ => null
        };

        static string? DemVaNoi((int So, string Ten) a, (int So, string Ten) b, string doiTuong)
        {
            var phan = new[] { a, b }.Where(x => x.So > 0).Select(x => $"{x.So:N0} {x.Ten}").ToList();
            return phan.Count == 0 ? null : $"{doiTuong} còn {string.Join(" và ", phan)}. Không thể xoá để giữ lịch sử học vụ.";
        }
    }

    /// <summary>
    /// Gọi sau mỗi thao tác ghi thành công: lưu phần thay đổi xuống CSDL rồi báo các tab làm mới.
    /// Nếu lưu thất bại, nạp lại bộ nhớ từ CSDL để hai bên không lệch nhau và báo lỗi cho người gọi.
    /// </summary>
    public void ThongBaoThayDoi(string? nguoiThucHien = null, string? vaiTro = null)
    {
        if (nhaMayCsdl is not null)
        {
            lock (KhoaDongBo)
            {
                try { DongBoXuongCsdl(nguoiThucHien, vaiTro); }
                catch (Exception loi)
                {
                    nhatKy?.LogError(loi, "Lưu dữ liệu nghiệp vụ thất bại; khôi phục bộ nhớ từ CSDL.");
                    using var db = nhaMayCsdl.CreateDbContext();
                    (DanhSachKhoa, DanhSachTo, DanhSachCanBo, DanhSachLop, DanhSachHocVien) = NapTuCsdl(db);
                    ChupLaiTrangThaiDaLuu();
                    DuLieuDaThayDoi?.Invoke();
                    throw new InvalidOperationException("Không lưu được thay đổi vào cơ sở dữ liệu. Dữ liệu đã được khôi phục về lần lưu gần nhất.", loi);
                }
            }
        }
        DuLieuDaThayDoi?.Invoke();
    }

    private static (List<Khoa>, List<ToChuyenMon>, List<CanBoGiangVien>, List<LopDaoTao>, List<HocVien>) NapTuCsdl(CoSoDuLieuNghiepVu db)
    {
        static IQueryable<T> TheoThuTu<T>(IQueryable<T> q) where T : class
            => q.AsNoTracking().OrderBy(x => EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu));
        return (TheoThuTu(db.Khoa).ToList(), TheoThuTu(db.ToChuyenMon).ToList(), TheoThuTu(db.CanBo).ToList(),
            TheoThuTu(db.Lop).ToList(), TheoThuTu(db.HocVien).ToList());
    }

    private static (List<Khoa>, List<ToChuyenMon>, List<CanBoGiangVien>, List<LopDaoTao>, List<HocVien>) SinhDuLieuMau()
    {
        var khoa = TaoKhoa();
        var to = TaoTo(khoa);
        var canBo = TaoCanBo(khoa, to);
        var lop = TaoLop();
        var hocVien = TaoHocVien(lop);
        GanNguoiPhuTrach(khoa, to, canBo);
        return (khoa, to, canBo, lop, hocVien);
    }

    // ---------- Đồng bộ bộ nhớ → CSDL ----------
    // Giữ ảnh chụp JSON của từng bản ghi ở lần lưu gần nhất. Mỗi lần ghi, so danh sách hiện tại
    // với ảnh chụp: bản ghi mới thì thêm, khác thì cập nhật, biến mất thì xoá. Tất cả trong một giao dịch.

    private sealed class AnhChupDaLuu
    {
        public Dictionary<string, string> Khoa = [], To = [], CanBo = [], Lop = [], HocVien = [];
        public int ThuTuLonNhat;
    }

    private void ChupLaiTrangThaiDaLuu()
    {
        anhChup.Khoa = DanhSachKhoa.ToDictionary(x => x.MaKhoa, Json);
        anhChup.To = DanhSachTo.ToDictionary(x => x.MaTo, Json);
        anhChup.CanBo = DanhSachCanBo.ToDictionary(x => x.MaCanBo, Json);
        anhChup.Lop = DanhSachLop.ToDictionary(x => x.MaLop, Json);
        anhChup.HocVien = DanhSachHocVien.ToDictionary(x => x.MaHocVien, Json);
        using var db = nhaMayCsdl!.CreateDbContext();
        anhChup.ThuTuLonNhat = new[]
        {
            db.Khoa.Max(x => (int?)EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu)) ?? 0,
            db.ToChuyenMon.Max(x => (int?)EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu)) ?? 0,
            db.CanBo.Max(x => (int?)EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu)) ?? 0,
            db.Lop.Max(x => (int?)EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu)) ?? 0,
            db.HocVien.Max(x => (int?)EF.Property<int>(x, CoSoDuLieuNghiepVu.CotThuTu)) ?? 0,
        }.Max();
    }

    private void DongBoXuongCsdl(string? nguoiThucHien, string? vaiTro, bool boQuaNhatKy = false)
    {
        using var db = nhaMayCsdl!.CreateDbContext();
        db.BoQuaNhatKy = boQuaNhatKy;
        db.NguoiThucHien = nguoiThucHien ?? "Hệ thống";
        db.VaiTroThucHien = vaiTro ?? "";
        var thuTu = anhChup.ThuTuLonNhat;
        if (!boQuaNhatKy) GhiBoNhiem(db);
        var moi = new AnhChupDaLuu
        {
            Khoa = DongBoBang(db, DanhSachKhoa, anhChup.Khoa, x => x.MaKhoa, ref thuTu),
            To = DongBoBang(db, DanhSachTo, anhChup.To, x => x.MaTo, ref thuTu),
            CanBo = DongBoBang(db, DanhSachCanBo, anhChup.CanBo, x => x.MaCanBo, ref thuTu),
            Lop = DongBoBang(db, DanhSachLop, anhChup.Lop, x => x.MaLop, ref thuTu),
            HocVien = DongBoBang(db, DanhSachHocVien, anhChup.HocVien, x => x.MaHocVien, ref thuTu),
        };
        if (db.ChangeTracker.HasChanges()) db.SaveChanges();
        // Chỉ nhận ảnh chụp mới khi đã lưu thành công.
        anhChup.Khoa = moi.Khoa; anhChup.To = moi.To; anhChup.CanBo = moi.CanBo; anhChup.Lop = moi.Lop; anhChup.HocVien = moi.HocVien;
        anhChup.ThuTuLonNhat = thuTu;
    }

    // ---------- Lịch sử bổ nhiệm ----------

    private sealed record ViTriLanhDao(string LoaiDonVi, string MaDonVi, string ChucVu, string MaCanBo);

    private static IEnumerable<ViTriLanhDao> CacViTri(IEnumerable<Khoa> khoa, IEnumerable<ToChuyenMon> to)
    {
        foreach (var k in khoa)
        {
            if (k.MaTruongKhoa is { } t) yield return new("Khoa", k.MaKhoa, "Trưởng khoa", t);
            if (k.MaPhoKhoa is { } p1) yield return new("Khoa", k.MaKhoa, "Phó khoa", p1);
            if (k.MaPhoKhoaThuHai is { } p2) yield return new("Khoa", k.MaKhoa, "Phó khoa", p2);
        }
        foreach (var x in to)
        {
            if (x.MaToTruong is { } t) yield return new("Tổ", x.MaTo, "Tổ trưởng", t);
            if (x.MaToPho is { } p) yield return new("Tổ", x.MaTo, "Tổ phó", p);
        }
    }

    /// <summary>So vị trí lãnh đạo trước/sau lần ghi: vị trí mất người thì đóng bổ nhiệm, vị trí có người mới thì mở bổ nhiệm mới.</summary>
    private void GhiBoNhiem(CoSoDuLieuNghiepVu db)
    {
        var cu = CacViTri(anhChup.Khoa.Values.Select(j => System.Text.Json.JsonSerializer.Deserialize<Khoa>(j)!),
                          anhChup.To.Values.Select(j => System.Text.Json.JsonSerializer.Deserialize<ToChuyenMon>(j)!)).ToHashSet();
        var moi = CacViTri(DanhSachKhoa, DanhSachTo).ToHashSet();
        var homNay = DateTime.Today;
        foreach (var het in cu.Except(moi))
            foreach (var bn in db.BoNhiem.Where(b => b.MaCanBo == het.MaCanBo && b.MaDonVi == het.MaDonVi && b.ChucVu == het.ChucVu && b.DenNgay == null).ToList())
                bn.DenNgay = homNay;
        foreach (var bd in moi.Except(cu))
            db.BoNhiem.Add(new BoNhiem { MaCanBo = bd.MaCanBo, ChucVu = bd.ChucVu, LoaiDonVi = bd.LoaiDonVi, MaDonVi = bd.MaDonVi, TuNgay = homNay });
    }

    /// <summary>Lịch sử bổ nhiệm của một cán bộ (mới nhất trước); rỗng ở kho thuần bộ nhớ.</summary>
    public List<BoNhiem> LayBoNhiem(string maCanBo)
    {
        if (nhaMayCsdl is null) return [];
        using var db = nhaMayCsdl.CreateDbContext();
        return db.BoNhiem.AsNoTracking().Where(b => b.MaCanBo == maCanBo).OrderByDescending(b => b.TuNgay).ToList();
    }

    private static Dictionary<string, string> DongBoBang<T>(CoSoDuLieuNghiepVu db, List<T> hienTai,
        Dictionary<string, string> daLuu, Func<T, string> layMa, ref int thuTu) where T : class
    {
        var ketQua = new Dictionary<string, string>(hienTai.Count);
        foreach (var banGhi in hienTai)
        {
            var ma = layMa(banGhi);
            var json = Json(banGhi);
            ketQua[ma] = json;
            if (!daLuu.TryGetValue(ma, out var jsonCu))
            {
                var muc = db.Add(banGhi);
                muc.Property(CoSoDuLieuNghiepVu.CotThuTu).CurrentValue = ++thuTu;
            }
            else if (jsonCu != json)
            {
                db.GiaTriCuNgoai[banGhi] = jsonCu;
                var muc = db.Update(banGhi);
                // Không ghi đè thứ tự gốc bằng giá trị mặc định của cột ẩn.
                muc.Property(CoSoDuLieuNghiepVu.CotThuTu).IsModified = false;
            }
        }
        foreach (var (ma, jsonCu) in daLuu)
        {
            if (ketQua.ContainsKey(ma)) continue;
            var banCu = System.Text.Json.JsonSerializer.Deserialize<T>(jsonCu)!;
            db.GiaTriCuNgoai[banCu] = jsonCu;
            db.Remove(banCu);
        }
        return ketQua;
    }

    private static string Json<T>(T x) => System.Text.Json.JsonSerializer.Serialize(x);

    private static void GanNguoiPhuTrach(List<Khoa> danhSachKhoa, List<ToChuyenMon> danhSachTo, List<CanBoGiangVien> danhSachCanBo)
    {
        foreach (var khoa in danhSachKhoa)
        {
            khoa.MaTruongKhoa = danhSachCanBo.First(canBo => canBo.MaKhoa == khoa.MaKhoa && canBo.ChucVu == "Trưởng khoa").MaCanBo;
            var cacPhoKhoa = danhSachCanBo.Where(canBo => canBo.MaKhoa == khoa.MaKhoa && canBo.ChucVu == "Phó khoa").ToList();
            khoa.MaPhoKhoa = cacPhoKhoa[0].MaCanBo;
            khoa.MaPhoKhoaThuHai = cacPhoKhoa[1].MaCanBo;
        }
        foreach (var to in danhSachTo)
        {
            to.MaToTruong = danhSachCanBo.First(canBo => canBo.MaTo == to.MaTo && canBo.ChucVu == "Tổ trưởng").MaCanBo;
            to.MaToPho = danhSachCanBo.First(canBo => canBo.MaTo == to.MaTo && canBo.ChucVu == "Tổ phó").MaCanBo;
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
        // Ngẫu nhiên có hạt giống: tên giả đa dạng nhưng giữ ổn định sau mỗi lần chạy.
        var ngauNhien = new Random(2026);
        string[] capBac = ["Đại úy", "Thiếu tá", "Trung tá", "Thượng tá"];
        var tenDaDung = new HashSet<string>();
        foreach (var lop in danhSach)
        {
            string hoTen;
            do { hoTen = QuyTacDuLieu.TaoHoTen(ngauNhien.Next(1, 5000), ngauNhien.Next(3) == 0 ? "Nữ" : "Nam"); }
            while (!tenDaDung.Add(hoTen));
            lop.GiaoVienChuNhiem = $"{capBac[ngauNhien.Next(capBac.Length)]} {hoTen}";
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

