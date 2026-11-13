// Vai trò tệp: Kiểm thử văn bản, nơi nhận, quyền truy cập và trạng thái đã đọc.
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nhom2_BTL.Models;
using Nhom2_BTL.Services;

void Check(bool ok, string ten) { if (!ok) throw new Exception(ten); Console.WriteLine("PASS " + ten); }
async Task BiTuChoi(Func<Task> thaoTac, string ten)
{
    try { await thaoTac(); } catch (UnauthorizedAccessException) { Console.WriteLine("PASS " + ten); return; }
    throw new Exception(ten);
}
var tep = Path.Combine(Path.GetTempPath(), $"thong-bao-{Guid.NewGuid()}.db");
var dichVu = new ServiceCollection();
dichVu.AddLogging();
dichVu.AddDbContext<CoSoTaiKhoan>(o => o.UseSqlite("Data Source=" + tep));
dichVu.AddIdentityCore<TaiKhoan>(o => KiemTraMatKhau.CauHinh(o.Password))
    .AddPasswordValidator<KiemTraMatKhau>().AddEntityFrameworkStores<CoSoTaiKhoan>();
dichVu.AddSingleton<DichVuTaiKhoan>();
try
{
    using var provider = dichVu.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoSoTaiKhoan>();
    await db.Database.EnsureCreatedAsync();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
    var admin = new TaiKhoan { UserName = "admin-test-tin", VaiTro = "Admin" };
    var guest = new TaiKhoan { UserName = "guest-test-tin", VaiTro = "Guest" };
    var manager = new TaiKhoan { UserName = "manager-test-tin", VaiTro = "Manager", PhamVi = "Khoa" };
    foreach (var u in new[] { admin, guest, manager }) Check((await users.CreateAsync(u, "a1")).Succeeded, "create test actor " + u.VaiTro);
    ClaimsPrincipal Principal(TaiKhoan u) => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, u.Id), new Claim(ClaimTypes.Name, u.UserName!),
        new Claim(ClaimTypes.Role, u.VaiTro), new Claim("DauBaoMat", u.SecurityStamp!), new Claim("PhamVi", u.PhamVi)
    }, "Test"));
    var auth = new TestAuth();
    var kho = new KhoThongBaoDemo();
    var svc = new DichVuThongBao(kho, new KhoDuLieuDemo(), auth, provider.GetRequiredService<DichVuTaiKhoan>());
    var mau = svc.LayDanhSach();
    var lop = svc.LayLop();
    var d57 = lop.First(x => x.KhoaHoc == "D57");
    var k5 = lop.First(x => x.KhoaHoc == "K5");
    Check(svc.LayTags().Any(x => x.Ma == DichVuThongBao.TagKhoaHoc(d57)), "cohort is available as a stored tag");
    Check(DichVuThongBao.KhopPhamVi(["he:" + d57.HeDaoTao], "lop:" + d57.MaLop, lop), "specific class includes system-wide notice");
    Check(DichVuThongBao.KhopPhamVi([DichVuThongBao.TagKhoaHoc(d57)], "lop:" + d57.MaLop, lop), "specific class includes cohort-wide notice");
    Check(!DichVuThongBao.KhopPhamVi([DichVuThongBao.TagKhoaHoc(k5)], "lop:" + d57.MaLop, lop), "exclude a different training system");
    Check(DichVuThongBao.KhopPhamVi(["lop:" + d57.MaLop], DichVuThongBao.TagKhoaHoc(d57), lop), "cohort filter includes its classes");
    Check(DichVuThongBao.KhopPhamVi(["lop:*"], "lop:" + k5.MaLop, lop), "all-classes notice matches any class");
    Check(!DichVuThongBao.KhopPhamVi(["khoa:KH-01"], "lop:*", lop), "class filter excludes unrelated faculty notices");
    Check(mau.Count == 3 && mau[0].DangLuc == new DateTimeOffset(2026, 11, 3, 8, 0, 0, TimeSpan.FromHours(7)), "three fixed demo posts and latest time UTC+7");
    var lucDang = mau[0].DangLuc;
    Check(!mau[0].LaMoi(lucDang.AddTicks(-1)) && mau[0].LaMoi(lucDang) && mau[0].LaMoi(lucDang.AddHours(24).AddTicks(-1)) && !mau[0].LaMoi(lucDang.AddHours(24)), "new badge 24-hour boundaries, excluding future posts");
    mau[0].Tags.Add("fake"); mau[0].TieuDe = "fake";
    Check(svc.LayDanhSach()[0].TieuDe != "fake" && svc.LayDanhSach()[0].Tags.Count == 0, "read snapshots cannot alter store");
    var nhap = new ThongBao { TieuDe = "Thông báo thử", NoiDung = "Nội dung\nhai dòng", TacGia = "Giả mạo", DangLuc = DateTimeOffset.MinValue };
    foreach (var u in new[] { guest, manager }) {
        auth.User = Principal(u);
        await BiTuChoi(async () => { await svc.LuuAsync(nhap); }, "deny create " + u.VaiTro);
        await BiTuChoi(() => svc.XoaAsync(mau[0].Ma), "deny delete " + u.VaiTro);
        await BiTuChoi(async () => { await svc.LuuAsync(new ThongBao { Ma = mau[0].Ma, TieuDe = "Tấn công", NoiDung = "Sửa trái phép" }); }, "deny edit " + u.VaiTro);
    }
    auth.User = new ClaimsPrincipal(new ClaimsIdentity());
    await BiTuChoi(async () => { await svc.LuuAsync(nhap); }, "deny anonymous");
    auth.User = Principal(admin);
    var tagKhoa = svc.LayTags().First(x => x.Ma.StartsWith("khoa:"));
    var tagHe = svc.LayTags().First(x => x.Ma.StartsWith("he:"));
    var tagLop = svc.LayTags().First(x => x.Ma.StartsWith("lop:"));
    nhap.Tags = [tagKhoa.Ma, tagHe.Ma, tagLop.Ma];
    var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aS1cAAAAASUVORK5CYII=");
    var soSuKien = 0; kho.DaThayDoi += () => soSuKien++;
    var ma = await svc.LuuAsync(nhap, png);
    var bai = svc.LayDanhSach().First(x => x.Ma == ma);
    Check(bai.TacGia == admin.UserName && bai.DangLuc > lucDang && bai.Tags.Count == 3 && bai.Anh!.StartsWith("data:image/png;base64,"), "admin creates post with server author/time, all tag types and uploaded image");
    var banSua = bai.TaoBanSao(); banSua.TieuDe = "Đã sửa"; banSua.TacGia = "Giả mạo"; banSua.DangLuc = DateTimeOffset.MinValue; banSua.Tags = [];
    await svc.LuuAsync(banSua, xoaAnh: true);
    var daSua = svc.LayDanhSach().First(x => x.Ma == ma);
    Check(daSua.TieuDe == "Đã sửa" && daSua.TacGia == bai.TacGia && daSua.DangLuc == bai.DangLuc && daSua.SuaLuc is not null && daSua.Anh is null && daSua.Tags.Count == 0, "edit keeps author/publication time, removes tags and image");
    foreach (var duLieu in new[] { new byte[0], new byte[DichVuThongBao.GioiHanAnh + 1], System.Text.Encoding.UTF8.GetBytes("<svg></svg>") }) {
        try { DichVuThongBao.TaoAnh(duLieu); throw new Exception("Invalid image accepted"); }
        catch (ArgumentException) { Console.WriteLine("PASS invalid or oversized image rejected"); }
    }
    nhap.Tags = ["khoa:khong-ton-tai"];
    try { await svc.LuuAsync(nhap); throw new Exception("Invalid tag accepted"); }
    catch (ArgumentException) { Console.WriteLine("PASS invalid tag rejected"); }
    nhap.Tags = []; nhap.TieuDe = "   ";
    try { await svc.LuuAsync(nhap); throw new Exception("Empty title accepted"); }
    catch (ArgumentException) { Console.WriteLine("PASS blank title rejected"); }
    await svc.XoaAsync(ma);
    Check(svc.LayDanhSach().Count == 3 && soSuKien == 3, "delete and change notifications");
    admin.BiKhoa = true; await users.UpdateAsync(admin);
    nhap.TieuDe = "Thử sau khi khóa";
    await BiTuChoi(async () => { await svc.LuuAsync(nhap); }, "reject blocked admin even with old admin claim");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(tep); }

sealed class TestAuth : AuthenticationStateProvider
{
    public ClaimsPrincipal User { get; set; } = new(new ClaimsIdentity());
    // Cung cấp trạng thái xác thực hiện tại cho Blazor.
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(User));
}
