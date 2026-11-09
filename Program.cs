using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Nhom2_BTL.Components;
using Nhom2_BTL.Models;
using Nhom2_BTL.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

// Dùng định dạng ngày, số và so sánh văn bản tiếng Việt thống nhất.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

// Tạo bộ cấu hình và vùng chứa dịch vụ cho ứng dụng Blazor.
var boMayUngDung = WebApplication.CreateBuilder(args);

// Bật giao diện Blazor tương tác trên máy chủ, phân quyền, trạng thái đăng nhập và Radzen.
boMayUngDung.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var thuMucTaiKhoan = Path.Combine(boMayUngDung.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(thuMucTaiKhoan);
boMayUngDung.Services.AddDbContext<CoSoTaiKhoan>(o => o.UseSqlite($"Data Source={Path.Combine(thuMucTaiKhoan, "tai-khoan.db")}"));
boMayUngDung.Services.AddIdentityCore<TaiKhoan>(o => {
    KiemTraMatKhau.CauHinh(o.Password);
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddPasswordValidator<KiemTraMatKhau>().AddEntityFrameworkStores<CoSoTaiKhoan>();
boMayUngDung.Services.AddSingleton<DichVuTaiKhoan>();
boMayUngDung.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddFixedWindowLimiter("tai-khoan", x => { x.PermitLimit = 30; x.Window = TimeSpan.FromMinutes(1); x.QueueLimit = 0; });
});
boMayUngDung.Services.AddSingleton<KhoPhienDemo>();
boMayUngDung.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "QuanLyDaoTao.Phien";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = boMayUngDung.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = false;
    o.LoginPath = "/dang-nhap";
    o.Events.OnValidatePrincipal = context =>
    {
        if (!context.HttpContext.RequestServices.GetRequiredService<KhoPhienDemo>().ConHieuLuc(context.Principal!)) context.RejectPrincipal();
        return Task.CompletedTask;
    };
});
boMayUngDung.Services.AddAuthorization(cauHinh =>
    cauHinh.AddPolicy(nameof(VaiTroDemo.Admin), chinhSach => chinhSach.RequireRole(nameof(VaiTroDemo.Admin))));
boMayUngDung.Services.AddCascadingAuthenticationState();
boMayUngDung.Services.AddRadzenComponents();
boMayUngDung.Services.AddScoped<TuyChonGiaoDien>();
boMayUngDung.Services.AddSingleton<KhoVeDangNhapDemo>();
boMayUngDung.Services.AddScoped<NhaCungCapTrangThaiXacThucDemo>();
boMayUngDung.Services.AddScoped<AuthenticationStateProvider>(dichVu => dichVu.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>());
// Dữ liệu nghiệp vụ lưu bền trong SQLite riêng (App_Data/du-lieu.db), tách khỏi CSDL tài khoản.
// Dùng factory vì Blazor Server không nên giữ một DbContext suốt cả phiên.
boMayUngDung.Services.AddDbContextFactory<CoSoDuLieuNghiepVu>(o => o.UseSqlite($"Data Source={Path.Combine(thuMucTaiKhoan, "du-lieu.db")}"));
// Kho singleton giữ bản đệm trong bộ nhớ để đọc nhanh; mọi thao tác ghi được lưu xuống CSDL.
boMayUngDung.Services.AddSingleton<KhoDuLieuDemo>();
// Thông báo lưu trong cùng CSDL nghiệp vụ (bảng ThongBao, ThongBaoDaDoc).
boMayUngDung.Services.AddSingleton<KhoThongBaoDemo>();
boMayUngDung.Services.AddScoped<DichVuThongBao>();
boMayUngDung.Services.AddScoped<DichVuPhanQuyenDemo>();
boMayUngDung.Services.AddScoped<DichVuQuanLyDuLieuDemo>();
boMayUngDung.Services.AddScoped<DichVuDaoTao>();
boMayUngDung.Services.AddScoped<DichVuHocVu>();
boMayUngDung.Services.AddScoped<DichVuNhatKy>();
boMayUngDung.Services.AddScoped<DichVuTimKiem>();
boMayUngDung.Services.AddScoped<DichVuSoDoToChuc>();
boMayUngDung.Services.AddScoped<DichVuTongQuan>();
boMayUngDung.Services.AddScoped<DichVuExcel>();
boMayUngDung.Services.AddScoped<DichVuNhapExcel>();
boMayUngDung.Services.AddScoped<DichVuBaoCaoExcel>();
boMayUngDung.Services.AddScoped<DichVuTruongTuyChinh>();
boMayUngDung.Services.AddScoped<DichVuBanTin>();
boMayUngDung.Services.AddScoped<DichVuChuongTrinh>();
QuyTacDuLieu.HocVienXemBanCungLop = boMayUngDung.Configuration.GetValue("HocVienXemBanCungLop", true);

// Hoàn tất cấu hình dịch vụ trước khi thiết lập các tuyến xử lý HTTP.
var ungDung = boMayUngDung.Build();
using (var scope = ungDung.Services.CreateScope())
{
    // Tạo/nạp CSDL nghiệp vụ ngay khi khởi động, không để người dùng đầu tiên phải chờ sinh dữ liệu mẫu.
    var khoNghiepVu = scope.ServiceProvider.GetRequiredService<KhoDuLieuDemo>();
    scope.ServiceProvider.GetRequiredService<KhoThongBaoDemo>();
    var dbTaiKhoan = scope.ServiceProvider.GetRequiredService<CoSoTaiKhoan>();
    await KhoiTaoCoSoTaiKhoan.TaoAsync(dbTaiKhoan);
    // Chuyển mã vai trò cũ trong database; các phiên mang claim cũ sẽ bị từ chối.
    await dbTaiKhoan.Users.Where(x => x.VaiTro == "Viewer")
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.VaiTro, "Guest"));
    var users = scope.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>();
    if (ungDung.Environment.IsDevelopment())
    {
        // Tài khoản mẫu "hocvien" (duy nhất) gắn với học viên đầu tiên lớp năm thứ tư B1D55: có lịch học, điểm danh hiện hành
        // và kết quả 6 học kỳ trước, đủ để xem học tập theo năm học, học kỳ.
        string maHocVienMau;
        lock (khoNghiepVu.KhoaDongBo) maHocVienMau = khoNghiepVu.DanhSachHocVien.FirstOrDefault(h => h.MaLop == KhoiTaoDaoTaoMau.LopNamCuoiMau)?.MaHocVien
            ?? khoNghiepVu.DanhSachHocVien.FirstOrDefault(h => h.MaLop == "B1D58")?.MaHocVien ?? "HV00001";
        await KhoiTaoTaiKhoanMau.TaoAsync(users, KhoiTaoDaoTaoMau.MaGiangVienMau, maHocVienMau);
    }
    // Chỉ cấu hình trên máy chủ; không có trang web tự nhận quyền admin.
    var ten = ungDung.Configuration["KhoiTaoAdmin:TenDangNhap"];
    var mk = ungDung.Configuration["KhoiTaoAdmin:MatKhau"];
    if (!await users.Users.AnyAsync(x => x.VaiTro == "Admin" && x.Id != "demo-co-dinh-admin") && !string.IsNullOrWhiteSpace(ten) && !string.IsNullOrWhiteSpace(mk))
    {
        if (await users.FindByNameAsync(ten) is not null) throw new InvalidOperationException("Tên admin khởi tạo đã tồn tại. Chọn tên riêng khác.");
        var result = await users.CreateAsync(new TaiKhoan { UserName = ten, VaiTro = "Admin", LockoutEnabled = true }, mk);
        if (!result.Succeeded) throw new InvalidOperationException("Không tạo được admin. Kiểm tra tên và chính sách mật khẩu.");
    }
}

// Chỉ bật trang lỗi chung và HSTS khi ứng dụng không chạy ở môi trường phát triển.
if (!ungDung.Environment.IsDevelopment())
{
    ungDung.UseExceptionHandler("/Error", createScopeForErrors: true);
    // HSTS yêu cầu trình duyệt ưu tiên HTTPS trong các lần truy cập tiếp theo.
    ungDung.UseHsts();
}

// Thiết lập HTTPS, phục vụ tệp tĩnh, chống giả mạo yêu cầu và ánh xạ ứng dụng Blazor.
ungDung.UseHttpsRedirection();

ungDung.UseStaticFiles();
ungDung.UseAuthentication();
ungDung.UseAuthorization();
ungDung.UseAntiforgery();
ungDung.UseRateLimiter();

// Từ điển nhãn giao diện cho lớp dịch toàn trang (wwwroot/dich-trang.js). Chỉ là nhãn tĩnh, không chứa dữ liệu.
ungDung.MapGet("/tu-dien/{ngon}", (string ngon) => Results.Json(TuDienGiaoDien.Lay(ngon)));

// Cấp token theo cookie hiện tại, kể cả khi trang được mở bằng điều hướng tương tác.
ungDung.MapGet("/phien/ma-bao-ve", (HttpContext http, IAntiforgery chongGiaMao) =>
{
    http.Response.Headers.CacheControl = "no-store";
    var token = chongGiaMao.GetAndStoreTokens(http);
    return Results.Json(new { tenTruong = token.FormFieldName, giaTri = token.RequestToken });
});

// Cookie phải được ghi bằng HTTP POST, trước khi phản hồi bắt đầu.
ungDung.MapPost("/phien/dang-nhap", async (HttpContext http, IAntiforgery chongGiaMao, NhaCungCapTrangThaiXacThucDemo auth, KhoPhienDemo kho) =>
{
    try { await chongGiaMao.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.LocalRedirect("/dang-nhap?loi=phien"); }
    var form = await http.Request.ReadFormAsync();
    if (!await auth.DangNhapAsync(form["tenDangNhap"].ToString(), form["matKhau"].ToString())) return Results.LocalRedirect("/dang-nhap?loi=1");
    var user = (await auth.GetAuthenticationStateAsync()).User;
    kho.ThuHoi(http.User);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);
    return Results.LocalRedirect("/");
}).RequireRateLimiting("tai-khoan");
ungDung.MapPost("/phien/dang-xuat", async (HttpContext http, IAntiforgery chongGiaMao, KhoPhienDemo kho) =>
{
    try { await chongGiaMao.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.LocalRedirect("/dang-nhap?loi=phien"); }
    kho.ThuHoi(http.User);
    await http.SignOutAsync();
    return Results.LocalRedirect("/dang-nhap");
});

ungDung.MapPost("/phien/dang-ky", async (HttpContext http, IAntiforgery baoVe, DichVuTaiKhoan users) => {
    try { await baoVe.ValidateRequestAsync(http); }
    catch (AntiforgeryValidationException) { return Results.LocalRedirect("/dang-ky?loi=1"); }
    var f = await http.Request.ReadFormAsync();
    if (f["matKhau"] != f["xacNhan"] || !await users.DangKy(f["tenDangNhap"].ToString(), f["matKhau"].ToString())) return Results.LocalRedirect("/dang-ky?loi=1");
    var tenDangNhap = Uri.EscapeDataString(f["tenDangNhap"].ToString().Trim());
    return Results.LocalRedirect($"/dang-nhap?moi=1&tenDangNhap={tenDangNhap}");
}).RequireRateLimiting("tai-khoan");
ungDung.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

ungDung.Run();
