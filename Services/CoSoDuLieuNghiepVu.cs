using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// CSDL SQLite lưu dữ liệu nghiệp vụ (khoa, tổ, cán bộ, lớp, học viên, đào tạo, học vụ, nhật ký)
/// để dữ liệu còn nguyên sau khi tắt ứng dụng. Tách khỏi CSDL tài khoản để có thể làm lại dữ liệu mẫu
/// mà không mất tài khoản.
///
/// Nhật ký thao tác được ghi tự động tại một điểm: mỗi lần SaveChanges, mọi bản ghi thêm/sửa/xoá
/// sinh một dòng NhatKy kèm giá trị trước/sau, trong cùng giao dịch với thay đổi.
/// </summary>
public sealed class CoSoDuLieuNghiepVu(DbContextOptions<CoSoDuLieuNghiepVu> tuyChon) : DbContext(tuyChon)
{
    // Cột ẩn giữ thứ tự chèn, để danh sách hiển thị đúng thứ tự như dữ liệu sinh ban đầu.
    public const string CotThuTu = "ThuTu";

    public DbSet<Khoa> Khoa => Set<Khoa>();
    public DbSet<ToChuyenMon> ToChuyenMon => Set<ToChuyenMon>();
    public DbSet<CanBoGiangVien> CanBo => Set<CanBoGiangVien>();
    public DbSet<LopDaoTao> Lop => Set<LopDaoTao>();
    public DbSet<HocVien> HocVien => Set<HocVien>();
    public DbSet<ChuyenDe> ChuyenDe => Set<ChuyenDe>();
    public DbSet<ChuyenMonGiangDay> ChuyenMonGiangDay => Set<ChuyenMonGiangDay>();
    public DbSet<Phong> Phong => Set<Phong>();
    public DbSet<BuoiHoc> BuoiHoc => Set<BuoiHoc>();
    public DbSet<DiemDanh> DiemDanh => Set<DiemDanh>();
    public DbSet<KetQua> KetQua => Set<KetQua>();
    public DbSet<NhatKy> NhatKy => Set<NhatKy>();
    public DbSet<Models.ThongBao> ThongBao => Set<Models.ThongBao>();
    public DbSet<ThongBaoDaDoc> ThongBaoDaDoc => Set<ThongBaoDaDoc>();
    public DbSet<BoNhiem> BoNhiem => Set<BoNhiem>();
    public DbSet<TinTuc> TinTuc => Set<TinTuc>();
    public DbSet<PhanCongGiangDay> PhanCong => Set<PhanCongGiangDay>();
    public DbSet<XetTuCachThi> XetTuCach => Set<XetTuCachThi>();
    public DbSet<LichThi> LichThi => Set<LichThi>();
    public DbSet<ChamCong> ChamCong => Set<ChamCong>();
    public DbSet<KetQuaLuuTru> KetQuaLuuTru => Set<KetQuaLuuTru>();
    public DbSet<SoDauBai> SoDauBai => Set<SoDauBai>();
    public DbSet<BaiThi> BaiThi => Set<BaiThi>();
    public DbSet<TruongTuyChinh> TruongTuyChinh => Set<TruongTuyChinh>();
    public DbSet<GiaTriTuyChinh> GiaTriTuyChinh => Set<GiaTriTuyChinh>();
    public DbSet<LichSuXetTuCach> LichSuXetTuCach => Set<LichSuXetTuCach>();
    public DbSet<DieuChinhTuCach> DieuChinhTuCach => Set<DieuChinhTuCach>();
    public DbSet<CaTrucBan> CaTrucBan => Set<CaTrucBan>();

    // ---- Thông tin người thực hiện, do dịch vụ gán trước khi lưu ----
    public string NguoiThucHien { get; set; } = "Hệ thống";
    public string VaiTroThucHien { get; set; } = "";
    /// <summary>Bỏ ghi nhật ký (chỉ dùng khi sinh dữ liệu mẫu lần đầu).</summary>
    public bool BoQuaNhatKy { get; set; }
    /// <summary>Giá trị cũ dạng JSON cho bản ghi được gắn lại (Update/Remove) mà context không có giá trị gốc.</summary>
    public Dictionary<object, string> GiaTriCuNgoai { get; } = new(ReferenceEqualityComparer.Instance);

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Khoa>(e =>
        {
            e.ToTable("Khoa");
            e.HasKey(x => x.MaKhoa);
            e.Ignore(x => x.SiSo);
            e.Property<int>(CotThuTu);
        });

        mb.Entity<ToChuyenMon>(e =>
        {
            e.ToTable("ToChuyenMon");
            e.HasKey(x => x.MaTo);
            e.Ignore(x => x.SiSo);
            e.Property<int>(CotThuTu);
            // Không xoá dây chuyền: dịch vụ chặn xoá khoa còn tổ, CSDL chặn thêm một lớp nữa.
            e.HasOne<Khoa>().WithMany().HasForeignKey(x => x.MaKhoa).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<CanBoGiangVien>(e =>
        {
            e.ToTable("CanBo");
            e.HasKey(x => x.MaCanBo);
            e.Ignore(x => x.TenKhoa);
            e.Ignore(x => x.TenTo);
            e.Ignore(x => x.MaTraCuuNoiBo);
            e.Property<int>(CotThuTu);
            e.HasIndex(x => x.SoCCCD).IsUnique();
            e.HasOne<Khoa>().WithMany().HasForeignKey(x => x.MaKhoa).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ToChuyenMon>().WithMany().HasForeignKey(x => x.MaTo).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<LopDaoTao>(e =>
        {
            e.ToTable("Lop");
            e.HasKey(x => x.MaLop);
            e.Ignore(x => x.SiSo);
            e.Property<int>(CotThuTu);
        });

        mb.Entity<HocVien>(e =>
        {
            e.ToTable("HocVien");
            e.HasKey(x => x.MaHocVien);
            e.Property<int>(CotThuTu);
            e.HasIndex(x => x.SoCCCD).IsUnique();
            e.HasIndex(x => x.MaLop);
            e.HasOne<LopDaoTao>().WithMany().HasForeignKey(x => x.MaLop).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ChuyenDe>(e =>
        {
            e.ToTable("ChuyenDe");
            e.HasKey(x => x.MaChuyenDe);
            e.HasOne<Khoa>().WithMany().HasForeignKey(x => x.MaKhoa).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ChuyenMonGiangDay>(e =>
        {
            e.ToTable("ChuyenMonGiangDay");
            e.HasKey(x => new { x.MaCanBo, x.MaChuyenDe });
            // Xoá cán bộ hoặc chuyên đề thì bỏ luôn quan hệ "đủ điều kiện dạy".
            e.HasOne<CanBoGiangVien>().WithMany().HasForeignKey(x => x.MaCanBo).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ChuyenDe>().WithMany().HasForeignKey(x => x.MaChuyenDe).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Phong>(e =>
        {
            e.ToTable("Phong");
            e.HasKey(x => x.MaPhong);
        });

        mb.Entity<BuoiHoc>(e =>
        {
            e.ToTable("BuoiHoc");
            e.HasKey(x => x.Id);
            e.Ignore(x => x.TietKetThuc);
            e.HasIndex(x => x.Ngay);
            e.HasIndex(x => new { x.MaLop, x.Ngay });
            e.HasOne<LopDaoTao>().WithMany().HasForeignKey(x => x.MaLop).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ChuyenDe>().WithMany().HasForeignKey(x => x.MaChuyenDe).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CanBoGiangVien>().WithMany().HasForeignKey(x => x.MaCanBo).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Phong>().WithMany().HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<DiemDanh>(e =>
        {
            e.ToTable("DiemDanh");
            e.HasKey(x => x.Id);
            // Mỗi học viên một dòng điểm danh cho mỗi buổi.
            e.HasIndex(x => new { x.BuoiHocId, x.MaHocVien }).IsUnique();
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.HasOne<BuoiHoc>().WithMany().HasForeignKey(x => x.BuoiHocId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<HocVien>().WithMany().HasForeignKey(x => x.MaHocVien).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<KetQua>(e =>
        {
            e.ToTable("KetQua");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MaHocVien, x.MaChuyenDe }).IsUnique();
            e.Property(x => x.CapNhatLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.HasOne<HocVien>().WithMany().HasForeignKey(x => x.MaHocVien).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ChuyenDe>().WithMany().HasForeignKey(x => x.MaChuyenDe).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Models.ThongBao>(e =>
        {
            e.ToTable("ThongBao");
            e.HasKey(x => x.Ma);
            e.Property(x => x.Tags).HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<List<string>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
            e.Property(x => x.DangLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.Property(x => x.SuaLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.Property(x => x.HieuLucDen).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<ThongBaoDaDoc>(e =>
        {
            e.ToTable("ThongBaoDaDoc");
            e.HasKey(x => new { x.MaThongBao, x.TenDangNhap });
            e.Property(x => x.Luc).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.HasOne<Models.ThongBao>().WithMany().HasForeignKey(x => x.MaThongBao).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<BoNhiem>(e =>
        {
            e.ToTable("BoNhiem");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MaCanBo, x.TuNgay });
            e.HasIndex(x => new { x.MaDonVi, x.ChucVu });
        });

        mb.Entity<TinTuc>(e =>
        {
            e.ToTable("TinTuc");
            e.HasKey(x => x.Id);
            e.Property(x => x.DangLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<PhanCongGiangDay>(e =>
        {
            e.ToTable("PhanCongGiangDay");
            e.HasKey(x => new { x.MaLop, x.MaChuyenDe });
            e.HasOne<LopDaoTao>().WithMany().HasForeignKey(x => x.MaLop).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Models.ChuyenDe>().WithMany().HasForeignKey(x => x.MaChuyenDe).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CanBoGiangVien>().WithMany().HasForeignKey(x => x.MaCanBo).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<XetTuCachThi>(e =>
        {
            e.ToTable("XetTuCachThi");
            e.HasKey(x => new { x.MaLop, x.MaChuyenDe });
            e.Property(x => x.CapNhatLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<LichThi>(e =>
        {
            e.ToTable("LichThi");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MaLop, x.MaChuyenDe }).IsUnique();
            e.HasOne<Models.Phong>().WithMany().HasForeignKey(x => x.MaPhong).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ChamCong>(e =>
        {
            e.ToTable("ChamCong");
            e.HasKey(x => new { x.MaCanBo, x.Ngay });
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<CaTrucBan>(e =>
        {
            e.ToTable("CaTrucBan");
            e.HasKey(x => x.Id);
            // Mỗi khoa mỗi ngày đúng một ca trực.
            e.HasIndex(x => new { x.MaKhoa, x.Ngay }).IsUnique();
            e.HasIndex(x => new { x.MaCanBo, x.Ngay });
            e.Property(x => x.PhanCongLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.Property(x => x.ChotLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<KetQuaLuuTru>(e =>
        {
            e.ToTable("KetQuaLuuTru");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MaHocVien);
        });

        mb.Entity<TruongTuyChinh>(e =>
        {
            e.ToTable("TruongTuyChinh");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.LoaiDoiTuong);
            e.Property(x => x.TaoLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
        });
        mb.Entity<GiaTriTuyChinh>(e =>
        {
            e.ToTable("GiaTriTuyChinh");
            e.HasKey(x => new { x.TruongId, x.MaDoiTuong });
            e.HasOne<TruongTuyChinh>().WithMany().HasForeignKey(x => x.TruongId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<BaiThi>(e =>
        {
            e.ToTable("BaiThi");
            e.HasKey(x => new { x.MaLop, x.MaChuyenDe });
            e.Property(x => x.DaGuiLuc).HasConversion(new DateTimeOffsetToBinaryConverter());
        });
        mb.Entity<LichSuXetTuCach>(e =>
        {
            e.ToTable("LichSuXetTuCach");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.MaLop, x.MaChuyenDe });
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
        });
        mb.Entity<DieuChinhTuCach>(e =>
        {
            e.ToTable("DieuChinhTuCach");
            e.HasKey(x => new { x.MaLop, x.MaChuyenDe, x.MaHocVien });
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
        });

        mb.Entity<SoDauBai>(e =>
        {
            e.ToTable("SoDauBai");
            e.HasKey(x => x.BuoiHocId);
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.HasOne<Models.BuoiHoc>().WithOne().HasForeignKey<SoDauBai>(x => x.BuoiHocId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<NhatKy>(e =>
        {
            e.ToTable("NhatKy");
            e.HasKey(x => x.Id);
            e.Property(x => x.ThoiDiem).HasConversion(new DateTimeOffsetToBinaryConverter());
            e.HasIndex(x => x.ThoiDiem);
        });
    }

    // ---------- Nhật ký tự động ----------

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (BoQuaNhatKy) return base.SaveChanges(acceptAllChangesOnSuccess);
        var nhap = ThuThapNhatKy();
        if (nhap.Count == 0) return base.SaveChanges(acceptAllChangesOnSuccess);
        using var giaoDich = Database.CurrentTransaction is null ? Database.BeginTransaction() : null;
        var ketQua = base.SaveChanges(true);
        HoanTatNhatKy(nhap);
        base.SaveChanges(true);
        giaoDich?.Commit();
        return ketQua;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken huy = default)
    {
        if (BoQuaNhatKy) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, huy);
        var nhap = ThuThapNhatKy();
        if (nhap.Count == 0) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, huy);
        await using var giaoDich = Database.CurrentTransaction is null ? await Database.BeginTransactionAsync(huy) : null;
        var ketQua = await base.SaveChangesAsync(true, huy);
        HoanTatNhatKy(nhap);
        await base.SaveChangesAsync(true, huy);
        if (giaoDich is not null) await giaoDich.CommitAsync(huy);
        return ketQua;
    }

    private sealed record NhapNhatKy(EntityEntry Muc, NhatKy Dong, bool LaThem);

    private List<NhapNhatKy> ThuThapNhatKy()
    {
        var ketQua = new List<NhapNhatKy>();
        foreach (var muc in ChangeTracker.Entries().ToList())
        {
            if (muc.Entity is Models.NhatKy or Models.ThongBaoDaDoc || muc.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            JsonObject? cu = null, moi = null;
            if (muc.State != EntityState.Added)
                cu = GiaTriCuNgoai.TryGetValue(muc.Entity, out var jsonCu) ? LocTruong(muc, JsonNode.Parse(jsonCu)!.AsObject()) : ThanhJson(muc, muc.OriginalValues);
            if (muc.State != EntityState.Deleted) moi = ThanhJson(muc, muc.CurrentValues);
            if (muc.State == EntityState.Modified && cu is not null && moi is not null)
            {
                // Chỉ giữ các trường thực sự thay đổi để nhật ký dễ đọc.
                foreach (var ten in cu.Select(x => x.Key).ToList())
                    if (JsonNode.DeepEquals(cu[ten], moi[ten])) { cu.Remove(ten); moi.Remove(ten); }
                if (cu.Count == 0 && moi.Count == 0) continue;
            }
            ketQua.Add(new(muc, new NhatKy
            {
                ThoiDiem = DateTimeOffset.Now,
                NguoiThucHien = NguoiThucHien,
                VaiTro = VaiTroThucHien,
                HanhDong = muc.State switch { EntityState.Added => "Thêm", EntityState.Deleted => "Xoá", _ => "Sửa" },
                DoiTuong = TenDoiTuong(muc.Entity),
                MaDoiTuong = KhoaChinh(muc),
                GiaTriCu = cu?.ToJsonString(TuyChonJson),
                GiaTriMoi = moi?.ToJsonString(TuyChonJson),
            }, muc.State == EntityState.Added));
        }
        return ketQua;
    }

    private void HoanTatNhatKy(List<NhapNhatKy> nhap)
    {
        // Khoá tự tăng (ví dụ Id buổi học) chỉ có sau khi lưu.
        foreach (var x in nhap.Where(x => x.LaThem)) x.Dong.MaDoiTuong = KhoaChinh(x.Muc);
        NhatKy.AddRange(nhap.Select(x => x.Dong));
        GiaTriCuNgoai.Clear();
    }

    private static readonly JsonSerializerOptions TuyChonJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static JsonObject ThanhJson(EntityEntry muc, PropertyValues giaTri)
    {
        var obj = new JsonObject();
        foreach (var p in muc.Metadata.GetProperties())
            if (!p.IsShadowProperty() && p.Name != "LuotXem") obj[p.Name] = p.Name == "Anh" && giaTri[p] is string { Length: > 0 } ? "(ảnh đính kèm)" : JsonSerializer.SerializeToNode(giaTri[p]);
        return obj;
    }

    private static JsonObject LocTruong(EntityEntry muc, JsonObject nguon)
    {
        var obj = new JsonObject();
        foreach (var p in muc.Metadata.GetProperties())
            if (!p.IsShadowProperty() && nguon.TryGetPropertyValue(p.Name, out var v)) obj[p.Name] = p.Name == "Anh" && v is not null ? "(ảnh đính kèm)" : v?.DeepClone();
        return obj;
    }

    // Bản ghi có khoá tự tăng (điểm danh, kết quả) hiển thị khoá nghiệp vụ để người đọc nhật ký hiểu ngay.
    private static string KhoaChinh(EntityEntry muc) => muc.Entity switch
    {
        Models.DiemDanh d => $"Buổi {d.BuoiHocId} · {d.MaHocVien}",
        Models.KetQua k => $"{k.MaHocVien} · {k.MaChuyenDe}",
        _ => string.Join(" · ", muc.Metadata.FindPrimaryKey()!.Properties.Select(p => muc.Property(p.Name).CurrentValue?.ToString()))
    };

    public static string TenDoiTuong(object x) => x switch
    {
        Models.Khoa => "Khoa", Models.ToChuyenMon => "Tổ chuyên môn", CanBoGiangVien => "Cán bộ", LopDaoTao => "Lớp", Models.HocVien => "Học viên",
        Models.ChuyenDe => "Chuyên đề", Models.ChuyenMonGiangDay => "Chuyên môn giảng dạy", Models.Phong => "Phòng", Models.BuoiHoc => "Buổi học",
        Models.DiemDanh => "Điểm danh", Models.KetQua => "Kết quả", Models.ThongBao => "Thông báo", Models.BoNhiem => "Bổ nhiệm", Models.TinTuc => "Bản tin", Models.PhanCongGiangDay => "Phân công giảng dạy",
        Models.XetTuCachThi => "Xét tư cách thi", Models.LichThi => "Lịch thi", Models.ChamCong => "Chấm công", Models.SoDauBai => "Sổ đầu bài", Models.BaiThi => "Thông tin bài thi", Models.TruongTuyChinh => "Trường dữ liệu mở rộng", Models.GiaTriTuyChinh => "Giá trị trường mở rộng", Models.LichSuXetTuCach => "Lịch sử xét tư cách", Models.DieuChinhTuCach => "Điều chỉnh tư cách thi", Models.CaTrucBan => "Ca trực ban", _ => x.GetType().Name
    };

    // ---------- Nâng cấu trúc CSDL không mất dữ liệu ----------

    /// <summary>
    /// Tạo CSDL nếu chưa có; nếu đã có thì chỉ tạo thêm các bảng (và chỉ mục của chúng) còn thiếu.
    /// Đủ cho việc bổ sung bảng mới; đổi cột của bảng cũ thì cần migration riêng.
    /// </summary>
    public void DamBaoCauTruc()
    {
        Database.EnsureCreated();
        Database.OpenConnection();
        try
        {
            var bangDaCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var lenh = Database.GetDbConnection().CreateCommand())
            {
                lenh.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using var doc = lenh.ExecuteReader();
                while (doc.Read()) bangDaCo.Add(doc.GetString(0));
            }
            var bangMoi = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cau in Database.GenerateCreateScript().Split([";\r\n", ";\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                var sql = cau.Trim();
                var bang = Regex.Match(sql, "^CREATE TABLE \"(\\w+)\"");
                if (bang.Success && !bangDaCo.Contains(bang.Groups[1].Value))
                {
                    Database.ExecuteSqlRaw(sql);
                    bangMoi.Add(bang.Groups[1].Value);
                    continue;
                }
                var chiMuc = Regex.Match(sql, "^CREATE (UNIQUE )?INDEX \"\\w+\" ON \"(\\w+)\"");
                if (chiMuc.Success && bangMoi.Contains(chiMuc.Groups[2].Value)) Database.ExecuteSqlRaw(sql);
            }
            // Hàng rào 7: nhật ký chỉ thêm, không ai sửa hoặc xoá được (kể cả Admin, kể cả gọi SQL trực tiếp qua ứng dụng).
            Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS NhatKy_KhongSua BEFORE UPDATE ON NhatKy BEGIN SELECT RAISE(ABORT, 'Nhật ký chỉ được thêm, không được sửa.'); END;");
            Database.ExecuteSqlRaw("CREATE TRIGGER IF NOT EXISTS NhatKy_KhongXoa BEFORE DELETE ON NhatKy BEGIN SELECT RAISE(ABORT, 'Nhật ký chỉ được thêm, không được xoá.'); END;");
        }
        finally { Database.CloseConnection(); }
    }
}
