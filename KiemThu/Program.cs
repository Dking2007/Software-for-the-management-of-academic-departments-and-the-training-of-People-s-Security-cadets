using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Radzen;
using Nhom2_BTL.Components.Layout;
using Nhom2_BTL.Models;
using Nhom2_BTL.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Bộ kiểm tra chạy trên kho riêng, không gọi server và không sửa dữ liệu của phiên đang mở.
var dv = new ServiceCollection();
dv.AddScoped<TuyChonGiaoDien>();
dv.AddLogging(); dv.AddRadzenComponents(); dv.AddSingleton<NavigationManager, DieuHuongKiemThu>(); dv.AddSingleton<IJSRuntime, JavaScriptKhongTuongTac>();
dv.AddAuthorizationCore(o => o.AddPolicy("Admin", p => p.RequireRole("Admin"))); dv.AddCascadingAuthenticationState();
// Kho nghiệp vụ thuần bộ nhớ (công thức gốc); dữ liệu đào tạo dùng CSDL tạm rỗng để trang dựng được.
dv.AddSingleton(new KhoDuLieuDemo()); dv.AddSingleton<KhoVeDangNhapDemo>();
var tepDaoTao = Path.Combine(Path.GetTempPath(), $"kiem-thu-dao-tao-{Guid.NewGuid()}.db");
dv.AddDbContextFactory<CoSoDuLieuNghiepVu>(o => o.UseSqlite($"Data Source={tepDaoTao};Pooling=False"));
dv.AddScoped<DichVuDaoTao>(); dv.AddScoped<DichVuHocVu>(); dv.AddScoped<DichVuNhatKy>(); dv.AddScoped<DichVuTimKiem>();
dv.AddScoped<DichVuExcel>(); dv.AddScoped<DichVuNhapExcel>(); dv.AddScoped<DichVuBaoCaoExcel>(); dv.AddScoped<DichVuTruongTuyChinh>(); dv.AddScoped<DichVuTongQuan>(); dv.AddScoped<DichVuSoDoToChuc>(); dv.AddScoped<DichVuChuongTrinh>(); dv.AddScoped<DichVuBanTin>(); dv.AddSingleton(new KhoThongBaoDemo()); dv.AddScoped<DichVuThongBao>();
dv.AddScoped<NhaCungCapTrangThaiXacThucDemo>();
dv.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>());
dv.AddScoped<DichVuPhanQuyenDemo>(); dv.AddScoped<DichVuQuanLyDuLieuDemo>();
// Đăng nhập đi qua Identity: dùng CSDL tài khoản tạm trong thư mục Temp, không đụng App_Data.
var tepTaiKhoan = Path.Combine(Path.GetTempPath(), $"kiem-thu-nghiep-vu-{Guid.NewGuid()}.db");
dv.AddDbContext<CoSoTaiKhoan>(o => o.UseSqlite("Data Source=" + tepTaiKhoan + ";Pooling=False"));
dv.AddIdentityCore<TaiKhoan>(o => KiemTraMatKhau.CauHinh(o.Password)).AddPasswordValidator<KiemTraMatKhau>().AddEntityFrameworkStores<CoSoTaiKhoan>();
dv.AddSingleton<DichVuTaiKhoan>();
// Tài khoản mẫu chỉ đăng nhập được trong Development.
dv.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new MoiTruongKiemThu());
using var root = dv.BuildServiceProvider(); using var phien = root.CreateScope();
await phien.ServiceProvider.GetRequiredService<CoSoTaiKhoan>().Database.EnsureCreatedAsync();
root.GetRequiredService<IDbContextFactory<CoSoDuLieuNghiepVu>>().CreateDbContext().DamBaoCauTruc();
await KhoiTaoTaiKhoanMau.TaoAsync(phien.ServiceProvider.GetRequiredService<UserManager<TaiKhoan>>());
AppDomain.CurrentDomain.ProcessExit += (_, _) => { try { File.Delete(tepTaiKhoan); File.Delete(tepDaoTao); } catch { } };
var auth = phien.ServiceProvider.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>();
var data = phien.ServiceProvider.GetRequiredService<DichVuQuanLyDuLieuDemo>();
int dem = 0;
void KiemTra(bool dung, string ten) { if (!dung) throw new Exception("FAIL: " + ten); dem++; Console.WriteLine("PASS: " + ten); }
void ThanhCong(KetQuaThaoTac k, string ten) => KiemTra(k.ThanhCong, ten + " | " + k.ThongBao);
string AnhChup() => JsonSerializer.Serialize(new { khoa=data.LayDanhSachKhoa(), to=data.LayDanhSachTo(), cb=data.LayDanhSachCanBo(), lop=data.LayDanhSachLop(), hv=data.LayDanhSachHocVien() });
async Task TuChoi(Func<Task<KetQuaThaoTac>> thaoTac, string ten) { var truoc=AnhChup(); KiemTra(!(await thaoTac()).ThanhCong && truoc==AnhChup(), ten + " — dữ liệu không đổi"); }
HocVien TaoHV(string ma="TEST-HV") => new() { MaHocVien=ma, HoTen="Nguyễn Văn Minh", MaLop="B10D57", ChucVu="Học viên", NgaySinh=new(2002,4,12), GioiTinh="Nam", QueQuan="Hà Nội", SoDienThoai="0987654321", SoCCCD="000099999999", DiaChiThuongTru="Số 1, đường Số 1, Hà Nội", Email="kiemthu@example.invalid" };
CanBoGiangVien TaoCB() => new() { MaCanBo="TEST-CB",HoTen="Đỗ Thu Hà",GioiTinh="Nữ",MaKhoa="KH-11",MaTo="KH-11-T02",ChuyenMon="Luật",SoDienThoai="0987654322",SoCCCD="000088888888",QueQuan="Hà Nội",Email="canbo@example.invalid" };

// Dữ liệu ban đầu: hai nhánh, định dạng, chức vụ và tên đa dạng.
var goc = data.LayThongKe();
KiemTra(goc.SoKhoa==12 && goc.SoTo==24 && goc.SoLop==106 && goc.SoHocVien==4240,"Cơ cấu ban đầu theo tài liệu");
KiemTra(data.LayDanhSachLop().All(l=>l.SiSo==40 && QuyTacDuLieu.LopHopLe(l.MaLop,l.HeDaoTao,l.KhoaHoc,l.ChuyenNganh)),"Chuyên ngành mọi lớp và sĩ số ban đầu");
KiemTra(data.LayDanhSachHocVien().GroupBy(h=>h.MaLop).All(g=>QuyTacDuLieu.ChiTieuChucVu.All(q=>g.Count(h=>h.ChucVu==q.Key)==q.Value)),"Cơ cấu bảy chức vụ trên mọi lớp");
KiemTra(data.LayDanhSachHocVien().All(h=>h.TaoLuc is null) && data.LayDanhSachCanBo().All(c=>c.TaoLuc is null),"Hồ sơ khởi tạo không mang nhãn mới");
KiemTra(data.LayDanhSachHocVien().Select(h=>h.HoTen).Distinct().Count()>4000,"Tên tiếng Việt đa dạng, trên 4000 tên duy nhất");
// Kiểm tra định dạng trên dữ liệu gốc của kho; bản sao cho khách đã bị che trường cá nhân nên không hợp lệ theo thiết kế.
var khoGoc=root.GetRequiredService<KhoDuLieuDemo>();
KiemTra(khoGoc.DanhSachHocVien.All(h=>Validator.TryValidateObject(h,new(h),[],true)) && khoGoc.DanhSachCanBo.All(c=>Validator.TryValidateObject(c,new(c),[],true)),"Định dạng toàn bộ dữ liệu khởi tạo");
KiemTra(khoGoc.DanhSachHocVien.Select(h=>h.SoCCCD).Distinct().Count()==4240,"CCCD khởi tạo duy nhất");
KiemTra(data.LayDanhSachCanBo().All(c=>data.LayDanhSachTo().Any(t=>t.MaTo==c.MaTo && t.MaKhoa==c.MaKhoa)),"Cán bộ có tổ và khoa nhất quán");
await TuChoi(()=>data.LuuHocVienAsync(TaoHV()),"Khách chưa đăng nhập không được ghi");
KiemTra(!await auth.DangNhapAsync("admin","sai"),"Sai mật khẩu bị từ chối");
KiemTra(await auth.DangNhapAsync("admin","demo123"),"Admin đăng nhập");
int suKien=0; data.DuLieuDaThayDoi += ()=>suKien++;

// Nhóm A: thêm, sửa, chuyển cùng khóa, khác khóa/hệ, xóa và chức vụ.
var hv=TaoHV(); ThanhCong(await data.LuuHocVienAsync(hv),"Thêm học viên thứ 41 vào B10D57");
var tk=data.LayThongKe();
KiemTra(tk.HocVienTheoLop["B10D57"]==41 && tk.SoHocVien==goc.SoHocVien+1 && tk.HocVienTheoKhoaHoc["D57"]==goc.HocVienTheoKhoaHoc["D57"]+1 && tk.HocVienTheoHe["Chính quy"]==goc.HocVienTheoHe["Chính quy"]+1,"Sĩ số lớp, khóa, hệ và toàn hệ thống tăng đúng");
KiemTra(goc.HocVienTheoLop.Where(x=>x.Key!="B10D57").All(x=>tk.HocVienTheoLop[x.Key]==x.Value) && tk.SoCanBo==goc.SoCanBo,"Các lớp khác và cán bộ không thay đổi");
KiemTra(data.LayDanhSachHocVien().Single(h=>h.MaHocVien==hv.MaHocVien).TaoLuc is not null,"Bản ghi người dùng thêm có mốc nhãn Mới");
hv.HoTen="Nguyễn Đức Huy"; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Sửa thông tin cá nhân"); KiemTra(data.LayThongKe().SoHocVien==tk.SoHocVien,"Sửa tên không đổi sĩ số");
hv.MaLop="B11D57"; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Chuyển lớp cùng khóa");
tk=data.LayThongKe(); KiemTra(tk.HocVienTheoLop["B10D57"]==40 && tk.HocVienTheoLop["B11D57"]==41 && tk.HocVienTheoKhoaHoc["D57"]==601 && tk.SoHocVien==4241,"Chuyển cùng khóa giữ tổng");
hv.MaLop="B1D58"; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Chuyển khác khóa");
tk=data.LayThongKe(); KiemTra(tk.HocVienTheoKhoaHoc["D57"]==600 && tk.HocVienTheoKhoaHoc["D58"]==601 && tk.SoHocVien==4241,"Tổng hai khóa cập nhật đúng");
hv.MaLop="B1K6"; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Chuyển khác hệ");
tk=data.LayThongKe(); KiemTra(tk.HocVienTheoHe["Chính quy"]==3600 && tk.HocVienTheoHe["Văn bằng 2"]==641 && tk.SoHocVien==4241,"Tổng hai hệ cập nhật đúng");
hv.ChucVu="B trưởng"; await TuChoi(()=>data.LuuHocVienAsync(hv,hv.MaHocVien),"Không vượt chỉ tiêu B trưởng khi chuyển/sửa"); hv.ChucVu="Học viên";
ThanhCong(await data.XoaHocVienAsync(hv.MaHocVien),"Xóa đúng học viên vừa thêm");
KiemTra(data.LayThongKe().SoHocVien==goc.SoHocVien && data.LayThongKe().SoCanBo==goc.SoCanBo,"Xóa khôi phục các tổng ban đầu");
var lanhDao=data.LayDanhSachHocVien().First(h=>h.ChucVu=="B trưởng");
ThanhCong(await data.XoaHocVienAsync(lanhDao.MaHocVien),"Xóa học viên giữ chức vụ");
KiemTra(!data.LayDanhSachHocVien().Any(h=>h.MaLop==lanhDao.MaLop && h.ChucVu=="B trưởng"),"Không tự phân công người thay thế");

// Nhóm B: chuyển cán bộ không tác động tới học viên.
var cb=TaoCB(); var soHV=data.LayThongKe().SoHocVien;
ThanhCong(await data.LuuCanBoAsync(cb),"Thêm cán bộ Tổ 2 Khoa Luật");
tk=data.LayThongKe(); KiemTra(tk.CanBoTheoTo["KH-11-T02"]==goc.CanBoTheoTo["KH-11-T02"]+1 && tk.CanBoTheoKhoa["KH-11"]==goc.CanBoTheoKhoa["KH-11"]+1 && tk.SoCanBo==goc.SoCanBo+1 && tk.SoHocVien==soHV,"Tăng đúng tổ/khoa/cán bộ, học viên giữ nguyên");
cb.HoTen="Lê Ngọc Anh"; ThanhCong(await data.LuuCanBoAsync(cb,cb.MaCanBo),"Sửa cá nhân cán bộ"); KiemTra(data.LayThongKe().SoCanBo==goc.SoCanBo+1,"Sửa cán bộ không tăng bản ghi");
cb.MaTo="KH-11-T01"; ThanhCong(await data.LuuCanBoAsync(cb,cb.MaCanBo),"Chuyển tổ cùng khoa");
tk=data.LayThongKe(); KiemTra(tk.CanBoTheoTo["KH-11-T02"]==goc.CanBoTheoTo["KH-11-T02"] && tk.CanBoTheoKhoa["KH-11"]==goc.CanBoTheoKhoa["KH-11"]+1,"Tổ cũ giảm, khoa giữ tổng");
cb.MaTo="KH-12-T02"; cb.MaKhoa="KH-12"; ThanhCong(await data.LuuCanBoAsync(cb,cb.MaCanBo),"Chuyển cán bộ sang khoa khác");
tk=data.LayThongKe(); KiemTra(tk.CanBoTheoKhoa["KH-11"]==goc.CanBoTheoKhoa["KH-11"] && tk.CanBoTheoKhoa["KH-12"]==goc.CanBoTheoKhoa["KH-12"]+1 && tk.SoCanBo==goc.SoCanBo+1,"Hai khoa và tổng cán bộ cập nhật đúng");
ThanhCong(await data.XoaCanBoAsync(cb.MaCanBo),"Xóa cán bộ"); KiemTra(data.LayThongKe().SoCanBo==goc.SoCanBo && data.LayThongKe().SoHocVien==soHV,"Xóa cán bộ không ảnh hưởng học viên");
var truong=data.LayDanhSachCanBo().First(c=>c.ChucVu=="Trưởng khoa"); ThanhCong(await data.XoaCanBoAsync(truong.MaCanBo),"Xóa lãnh đạo khoa");
KiemTra(data.LayDanhSachKhoa().Single(k=>k.MaKhoa==truong.MaKhoa).MaTruongKhoa is null,"Không để tham chiếu lãnh đạo đã xóa");

// Nhóm C/D: ràng buộc, lỗi định dạng và chống xóa dây chuyền.
await TuChoi(()=>data.LuuToAsync(new(){MaTo="BAD",TenTo="Tổ",MaKhoa="KH-X"}),"Tổ thuộc khoa không tồn tại");
cb=TaoCB(); cb.MaTo="KH-X"; await TuChoi(()=>data.LuuCanBoAsync(cb),"Cán bộ thuộc tổ không tồn tại");
cb.MaTo="KH-12-T01"; await TuChoi(()=>data.LuuCanBoAsync(cb),"Khoa/tổ không khớp");
hv=TaoHV();hv.MaLop="BAD";await TuChoi(()=>data.LuuHocVienAsync(hv),"Học viên thuộc lớp không tồn tại");
hv=TaoHV();hv.SoCCCD="1";await TuChoi(()=>data.LuuHocVienAsync(hv),"CCCD sai độ dài");
hv=TaoHV();hv.SoDienThoai="0000000000";await TuChoi(()=>data.LuuHocVienAsync(hv),"Điện thoại sai đầu số");
hv=TaoHV();hv.Email="sai";await TuChoi(()=>data.LuuHocVienAsync(hv),"Email sai định dạng");
hv=TaoHV();hv.NgaySinh=DateTime.Today.AddDays(1);await TuChoi(()=>data.LuuHocVienAsync(hv),"Ngày sinh tương lai");
await TuChoi(()=>data.LuuLopAsync(data.LayDanhSachLop()[0]),"Lớp trùng mã");
await TuChoi(()=>data.XoaKhoaAsync("KH-11"),"Không xóa khoa còn tổ");
await TuChoi(()=>data.XoaToAsync("KH-11-T02"),"Không xóa tổ còn cán bộ");
await TuChoi(()=>data.XoaLopAsync("B10D57"),"Không xóa lớp còn học viên");
var lop=data.LayDanhSachLop().First(l=>l.MaLop=="B10D57");lop.ChuyenNganh="Điều tra hình sự";await TuChoi(()=>data.LuuLopAsync(lop,lop.MaLop),"B10D57 không được gán sai chuyên ngành");
ThanhCong(await data.LuuLopAsync(new(){MaLop="B5K6",HeDaoTao="Văn bằng 2",KhoaHoc="K6",ChuyenNganh="Điều tra"}),"VB2 cho phép lớp thứ năm");
KiemTra(data.LayDanhSachLop().Single(l=>l.MaLop=="B5K6").SiSo==0,"Lớp mới có sĩ số thực bằng 0");
ThanhCong(await data.XoaLopAsync("B5K6"),"Xóa lớp không có học viên");
var khoa=data.LayDanhSachKhoa()[0];var maCu=khoa.MaKhoa;khoa.MaKhoa="DOI-MA";await TuChoi(()=>data.LuuKhoaAsync(khoa,maCu),"Không đổi mã khoa gây mồ côi");
// Bổ sung vòng đời đơn vị, phân công lãnh đạo, lỗi lặp và tính độc lập của bản sao.
var khoaMoi=new Khoa { MaKhoa="TEST-KHOA",TenKhoa="Khoa kiểm thử" };
ThanhCong(await data.LuuKhoaAsync(khoaMoi),"Thêm khoa rỗng");
khoaMoi.TenKhoa="Khoa kiểm thử cập nhật"; ThanhCong(await data.LuuKhoaAsync(khoaMoi,khoaMoi.MaKhoa),"Sửa tên khoa giữ mã");
var toMoi=new ToChuyenMon { MaTo="TEST-TO",TenTo="Tổ kiểm thử",MaKhoa=khoaMoi.MaKhoa };
ThanhCong(await data.LuuToAsync(toMoi),"Thêm tổ đúng khoa");
toMoi.TenTo="Tổ kiểm thử cập nhật"; ThanhCong(await data.LuuToAsync(toMoi,toMoi.MaTo),"Sửa tổ giữ mã");
cb=TaoCB();cb.MaKhoa=khoaMoi.MaKhoa;cb.MaTo=toMoi.MaTo;cb.ChucVu="Tổ trưởng";
ThanhCong(await data.LuuCanBoAsync(cb),"Phân công tổ trưởng mới");
KiemTra(data.LayDanhSachTo().Single(t=>t.MaTo==toMoi.MaTo).MaToTruong==cb.MaCanBo,"Liên kết tổ trưởng đúng mã");
await TuChoi(()=>data.LuuCanBoAsync(cb),"Từ chối trùng mã cán bộ");
await TuChoi(()=>data.XoaToAsync(toMoi.MaTo),"Từ chối xóa tổ đang có cán bộ");
var toDich=new ToChuyenMon {MaTo="TEST-TO-2",TenTo="Tổ đích",MaKhoa="KH-12"};
ThanhCong(await data.LuuToAsync(toDich),"Thêm tổ đích");
cb.MaTo=toDich.MaTo;cb.MaKhoa=toDich.MaKhoa;
ThanhCong(await data.LuuCanBoAsync(cb,cb.MaCanBo),"Chuyển tổ trưởng sang khoa khác");
KiemTra(data.LayDanhSachTo().Single(t=>t.MaTo==toMoi.MaTo).MaToTruong is null && data.LayDanhSachTo().Single(t=>t.MaTo==toDich.MaTo).MaToTruong==cb.MaCanBo,"Xóa phân công cũ và gán đúng phân công mới");
ThanhCong(await data.XoaCanBoAsync(cb.MaCanBo),"Xóa tổ trưởng");
KiemTra(data.LayDanhSachTo().Single(t=>t.MaTo==toDich.MaTo).MaToTruong is null,"Xóa tổ trưởng không còn tham chiếu");
ThanhCong(await data.XoaToAsync(toMoi.MaTo),"Xóa tổ rỗng");
ThanhCong(await data.XoaToAsync(toDich.MaTo),"Xóa tổ đích rỗng");
ThanhCong(await data.XoaKhoaAsync(khoaMoi.MaKhoa),"Xóa khoa rỗng");
// Văn bằng 2: số lớp chẵn là ANM & PCTPCNC, số lẻ là Điều tra (quy tắc trong tài liệu thiết kế).
var lopMoi=new LopDaoTao {MaLop="B6K6",HeDaoTao="Văn bằng 2",KhoaHoc="K6",ChuyenNganh="Điều tra"};
await TuChoi(()=>data.LuuLopAsync(lopMoi),"VB2 lớp số chẵn không được gán chuyên ngành Điều tra");
lopMoi.ChuyenNganh="ANM & PCTPCNC";ThanhCong(await data.LuuLopAsync(lopMoi),"Tạo lớp để kiểm tra phân công");
hv=TaoHV();hv.MaLop=lopMoi.MaLop;hv.ChucVu="B trưởng";
ThanhCong(await data.LuuHocVienAsync(hv),"Thêm B trưởng vào vị trí trống");
var daTao=data.LayDanhSachHocVien().Single(x=>x.MaHocVien==hv.MaHocVien);
hv.GioiTinh="Nữ"; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Tên không tự thay đổi giới tính người dùng nhập");
KiemTra(data.LayDanhSachHocVien().Single(x=>x.MaHocVien==hv.MaHocVien).GioiTinh=="Nữ" && data.LayDanhSachHocVien().Single(x=>x.MaHocVien==hv.MaHocVien).TaoLuc==daTao.TaoLuc,"Sửa giữ giới tính đã chọn và thời điểm tạo");
await TuChoi(()=>data.LuuHocVienAsync(hv),"Từ chối trùng mã học viên");
var trungCCCD=TaoHV("HV-TRUNG");await TuChoi(()=>data.LuuHocVienAsync(trungCCCD),"Từ chối trùng CCCD");
hv.MaLop=lanhDao.MaLop; ThanhCong(await data.LuuHocVienAsync(hv,hv.MaHocVien),"Chuyển B trưởng sang vị trí đã trống");
KiemTra(!data.LayDanhSachHocVien().Any(x=>x.MaLop==lopMoi.MaLop && x.ChucVu=="B trưởng"),"Chuyển lớp giải phóng chức vụ cũ");
ThanhCong(await data.XoaHocVienAsync(hv.MaHocVien),"Dọn hồ sơ trong kho kiểm thử");
ThanhCong(await data.XoaLopAsync(lopMoi.MaLop),"Xóa lớp trống sau chuyển học viên");
var banSao=data.LayDanhSachHocVien()[0];var tenGoc=banSao.HoTen;banSao.HoTen="Không lưu";
KiemTra(data.LayDanhSachHocVien().Single(x=>x.MaHocVien==banSao.MaHocVien).HoTen==tenGoc,"Chỉnh bản sao không làm thay đổi dữ liệu nguồn");
hv=TaoHV();hv.MaHocVien=null!;await TuChoi(()=>data.LuuHocVienAsync(hv),"Mã rỗng trả lỗi thay vì ném ngoại lệ");
var veCu=auth.TaoVeMoTab();await auth.DangXuatAsync();
KiemTra(!await auth.DangNhapBangVeAsync(veCu),"Vé cũ bị thu hồi sau đăng xuất");
ThanhCong(await Task.FromResult(new KetQuaThaoTac(await auth.DangNhapAsync("admin","demo123"),"Đăng nhập lại")),"Khôi phục Admin để tiếp tục kiểm thử");

KiemTra(suKien>10,"Phát thông báo thay đổi cho các tab");

// Nhóm E: gọi thẳng mọi API ghi bằng Guest để xác nhận quyền không chỉ nằm ở nút.
await auth.DangXuatAsync();KiemTra(await auth.DangNhapAsync("nguoixem","demo123"),"Tài khoản xem (Guest) đăng nhập");
await TuChoi(()=>data.LuuHocVienAsync(TaoHV()),"Guest không thêm/sửa học viên");
await TuChoi(()=>data.XoaHocVienAsync(data.LayDanhSachHocVien()[0].MaHocVien),"Guest không xóa học viên");
await TuChoi(()=>data.LuuCanBoAsync(TaoCB()),"Guest không thêm/sửa cán bộ");
await TuChoi(()=>data.XoaCanBoAsync(data.LayDanhSachCanBo()[0].MaCanBo),"Guest không xóa cán bộ");
await TuChoi(()=>data.LuuKhoaAsync(new(){MaKhoa="TEST",TenKhoa="Khoa"}),"Guest không thêm/sửa khoa");
await TuChoi(()=>data.XoaKhoaAsync("KH-11"),"Guest không xóa khoa");
await TuChoi(()=>data.LuuToAsync(new(){MaTo="TEST",TenTo="Tổ",MaKhoa="KH-11"}),"Guest không thêm/sửa tổ");
await TuChoi(()=>data.XoaToAsync("KH-11-T02"),"Guest không xóa tổ");
await TuChoi(()=>data.LuuLopAsync(new(){MaLop="B5K6",HeDaoTao="Văn bằng 2",KhoaHoc="K6",ChuyenNganh="Điều tra"}),"Guest không thêm/sửa lớp");
await TuChoi(()=>data.XoaLopAsync("B10D57"),"Guest không xóa lớp");
using var phien2=root.CreateScope();var auth2=phien2.ServiceProvider.GetRequiredService<NhaCungCapTrangThaiXacThucDemo>();
KiemTra(!(await auth2.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated,"Phiên mới không thừa hưởng quyền của phiên khác");
KiemTra(await auth2.DangNhapBangVeAsync(auth.TaoVeMoTab()) && (await auth2.GetAuthenticationStateAsync()).User.IsInRole("Guest"),"Vé tab giữ đúng vai trò Guest");
KiemTra(!await auth2.DangNhapBangVeAsync("Admin"),"Không nâng quyền bằng vé tự đặt");
// Kiểm tra HTML do component thực sự dựng; không khẳng định đã kiểm thử tương tác hay responsive.
await using var renderer=new HtmlRenderer(phien.ServiceProvider, phien.ServiceProvider.GetRequiredService<ILoggerFactory>());
async Task<string> HienThi<T>(Dictionary<string,object?>? thamSo=null) where T:IComponent => await renderer.Dispatcher.InvokeAsync(async()=>
{
    var html=await renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(thamSo ?? new()));
    return System.Net.WebUtility.HtmlDecode(html.ToHtmlString());
});
var trangXem=await HienThi<Nhom2_BTL.Components.Pages.HocVien>();
KiemTra(trangXem.Contains("Danh sách học viên") && !trangXem.Contains("Thêm học viên") && !trangXem.Contains("Xóa học viên"),"HTML Guest có bảng học viên, không có điều khiển ghi");
await auth.DangNhapAsync("admin","demo123");
var trangQuanTri=await HienThi<Nhom2_BTL.Components.Pages.HocVien>();
KiemTra(trangQuanTri.Contains("Thêm học viên") && trangQuanTri.Contains("Xóa học viên"),"HTML Admin có điều khiển quản lý học viên");
// STT lấy từ View hiện tại của Radzen, không phải vị trí của dữ liệu gốc.
var cacStt=System.Text.RegularExpressions.Regex.Matches(trangQuanTri, @"<tr[^>]*class=""rz-data-row[^>]*>\s*<td[^>]*>\s*<span[^>]*>([0-9]+)</span>");
KiemTra(cacStt.Count==10 && cacStt.Select(m=>int.Parse(m.Groups[1].Value)).SequenceEqual(Enumerable.Range(1,10)),"STT trang đầu là 1–10 (mặc định 10 dòng/trang) khi Radzen dựng bảng");
var trangCanBo=await HienThi<Nhom2_BTL.Components.Pages.CanBoGiangVien>();
KiemTra(trangCanBo.Contains("Tất cả các khoa") && trangCanBo.Contains("Tất cả các tổ"),"HTML cán bộ có cả bộ lọc khoa và tổ");
var trangChu=await HienThi<Nhom2_BTL.Components.Pages.Home>();
KiemTra(trangChu.Contains("Xin chào, admin") && !trangChu.Contains("target=\"_blank\""),"HTML trang chủ chào đúng tài khoản và mở liên kết cùng tab");
KiemTra(System.Text.RegularExpressions.Regex.Matches(trangChu, "class=\"the-so-lieu\"").Count == 5, "Trang chủ có đủ năm thẻ số liệu (khoa, tổ, cán bộ, lớp, học viên)");
await auth.DangXuatAsync(); KiemTra(await auth.DangNhapAsync("nguoixem", "demo123"),"Guest đăng nhập lại");
var trangChuGuest = await HienThi<Nhom2_BTL.Components.Pages.Home>();
KiemTra(trangChuGuest.Contains("Xin chào, nguoixem"), "Trang chủ chào đúng tài khoản Guest");
await auth.DangXuatAsync();
var trangChuKhach = await HienThi<Nhom2_BTL.Components.Pages.Home>();
KiemTra(trangChuKhach.Contains("Tổng quan") && !trangChuKhach.Contains("Xin chào"), "Chưa đăng nhập: trang chủ không chào tên ai");
await auth.DangNhapAsync("admin", "demo123");
var formHV=await HienThi<BieuMauHocVien>(new() { [nameof(BieuMauHocVien.DuLieuBanDau)]=TaoHV(),[nameof(BieuMauHocVien.DanhSachLop)]=data.LayDanhSachLop() });
KiemTra(formHV.Contains("D57") && formHV.Contains("ANM & PCTPCNC") && !formHV.Contains("MaKhoa"),"HTML form học viên hiện đúng khóa/chuyên ngành và không chọn khoa");
var formCB=await HienThi<BieuMauCanBo>(new() { [nameof(BieuMauCanBo.DuLieuBanDau)]=TaoCB(),[nameof(BieuMauCanBo.DanhSachKhoa)]=data.LayDanhSachKhoa(),[nameof(BieuMauCanBo.DanhSachTo)]=data.LayDanhSachTo() });
KiemTra(formCB.Contains("MaKhoa") && formCB.Contains("MaTo") && !formCB.Contains("MaLop"),"HTML form cán bộ chọn khoa/tổ, không chọn lớp");
var nhanMoi=await HienThi<NhanBanGhi>(new(){[nameof(NhanBanGhi.TaoLuc)]=DateTimeOffset.UtcNow});
var nhanCu=await HienThi<NhanBanGhi>(new(){[nameof(NhanBanGhi.TaoLuc)]=DateTimeOffset.UtcNow.AddMinutes(-16)});
KiemTra(nhanMoi.Contains("Mới") && !nhanCu.Contains("Mới"),"HTML nhãn Mới có thời hạn, không đổi hồ sơ");
// Gọi bộ lọc thực tế của trang lớp để kiểm tra các trường được hứa trên ô tìm kiếm.
var trangLopLoc = new Nhom2_BTL.Components.Pages.Lop();
var kieuTrangLop = trangLopLoc.GetType();
var rieng = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
kieuTrangLop.GetField("danhSachLop", rieng)!.SetValue(trangLopLoc, data.LayDanhSachLop().ToList());
IEnumerable<LopDaoTao> LocLop(string tuKhoa)
{
    kieuTrangLop.GetField("tuKhoa", rieng)!.SetValue(trangLopLoc, tuKhoa);
    return (IEnumerable<LopDaoTao>)kieuTrangLop.GetProperty("DanhSachHienThi", rieng)!.GetValue(trangLopLoc)!;
}
KiemTra(LocLop("  chính QUY  ").Count() == 90, "Tìm hệ đào tạo không phân biệt hoa thường và bỏ khoảng trắng ngoài");
KiemTra(LocLop("Văn bằng 2").Count() == 16, "Tìm hệ Văn bằng 2 trả đúng 16 lớp");
KiemTra(LocLop("B10D57").Single().MaLop == "B10D57", "Tìm đúng mã lớp");
KiemTra(!LocLop("không-có-lớp-này").Any(), "Từ khóa tự do không khớp trả danh sách trống");
KiemTra(LocLop("   ").Count() == 106, "Xóa từ khóa khôi phục toàn bộ lớp");
Console.WriteLine($"HOÀN TẤT: {dem} kiểm tra đạt.");

// Hai circuit dùng cùng phiên cookie: thu hồi phải vô hiệu hóa cả hai.
var khoPhien = new KhoPhienDemo();
using var tabMot = new NhaCungCapTrangThaiXacThucDemo(new KhoVeDangNhapDemo(), khoPhien, root.GetRequiredService<DichVuTaiKhoan>());
await tabMot.DangNhapAsync("admin", "demo123");
var danhTinhAdmin = (await tabMot.GetAuthenticationStateAsync()).User;
using var tabHai = new NhaCungCapTrangThaiXacThucDemo(new KhoVeDangNhapDemo(), khoPhien);
tabHai.SetAuthenticationState(Task.FromResult(new AuthenticationState(danhTinhAdmin)));
KiemTra((await tabHai.GetAuthenticationStateAsync()).User.IsInRole("Admin"), "Tab mới khôi phục Admin từ danh tính cookie");
var daNhanThuHoi = false;
tabHai.AuthenticationStateChanged += _ => daNhanThuHoi = true;
khoPhien.ThuHoi(danhTinhAdmin);
KiemTra(daNhanThuHoi && !(await tabHai.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated, "Tab cũ được thông báo thu hồi và mất quyền");
using var tabXem = new NhaCungCapTrangThaiXacThucDemo(new KhoVeDangNhapDemo(), khoPhien, root.GetRequiredService<DichVuTaiKhoan>());
KiemTra(await tabXem.DangNhapAsync("nguoixem", "demo123"),"Tab xem đăng nhập");
KiemTra((await tabXem.GetAuthenticationStateAsync()).User.IsInRole("Guest") && !(await tabMot.GetAuthenticationStateAsync()).User.IsInRole("Admin"), "Phiên Guest mới không khôi phục quyền Admin cũ");

// Tìm kiếm tự do: không theo mẫu, không phân biệt thứ tự, dấu, dấu phân cách.
foreach (var tu in new[] { "duy-b10", "b10 duy", "duy,b10", "Duy B10", "b10/duy", "duyb10", "DUY   b10d58" })
    KiemTra(TimKiemMo.Khop(tu, "Nguyễn Văn Duy", "HV00123", "B10D58"), $"Tìm tự do khớp \"{tu}\"");
KiemTra(!TimKiemMo.Khop("duy b11", "Nguyễn Văn Duy", "HV00123", "B10D58"), "Mọi từ phải khớp: sai lớp thì không ra");
KiemTra(TimKiemMo.Khop("nguyn van duy", "Nguyễn Văn Duy") && TimKiemMo.Khop("nvd", "Nguyễn Văn Duy"), "Gõ sai một chữ và gõ chữ cái đầu vẫn tìm được");
KiemTra(TimKiemMo.Loc(new[] { "Nguyễn Văn Thanh", "Nguyễn Văn Anh", "Trần Anh Tuấn" }, "anh", x => [x]).SequenceEqual(new[] { "Nguyễn Văn Anh", "Trần Anh Tuấn" }), "\"anh\" không lẫn Thanh, người tên Anh xếp trước");
KiemTra(TimKiemMo.LocNguoi(new[] { "Nguyễn Văn Thanh", "Nguyễn Văn Anh", "Trần Anh Tuấn" }, "anh", x => [x]).SequenceEqual(new[] { "Nguyễn Văn Anh" }), "Danh sách người: một từ chỉ ra đúng người mang tên đó");
KiemTra(TimKiemMo.LocNguoi(new[] { "Trần Anh Tuấn" }, "anh", x => [x]).Count == 1, "Không ai mang đúng tên đó thì vẫn ra người có từ ấy trong họ tên");
KiemTra(TimKiemMo.LocNguoi(new[] { "Nguyễn Anh Thư", "Nguyễn Văn Anh" }, "anh thư", x => [x]).SequenceEqual(new[] { "Nguyễn Anh Thư" }), "Hai từ trở lên vẫn tìm tự do");
KiemTra(TimKiemMo.Loc(new[] { "B10D58", "B1D58", "B11D58" }, "b1", x => [x])[0] == "B1D58", "\"b1\" xếp B1D58 lên trước B10D58");
KiemTra(TimKiemMo.Loc(new[] { "B10D58", "B1D58", "Tổ 1", "Tổ 10" }, "1", x => [x]).SequenceEqual(new[] { "Tổ 1", "B1D58" }), "Số ngắn chỉ khớp đúng số 1, không khớp 10");
KiemTra(TimKiemMo.Loc(new[] { "A", "B" }, "  ", x => [x]).Count == 2, "Từ khoá rỗng giữ nguyên danh sách");
var (kqMotPhan, mucMotPhan) = TimKiemMo.LocChiTiet(new[] { "Võ Hồng Châu", "Nguyễn Văn Duy", "Tổ 1" }, "tổ 1 châu", x => [x]);
KiemTra(mucMotPhan == MucKhop.MotPhan && kqMotPhan.SequenceEqual(new[] { "Võ Hồng Châu" }), "Không ai khớp đủ thì trả kết quả một phần theo từ quan trọng, bỏ từ chung");
KiemTra(TimKiemMo.VungKhop("Nguyễn Văn Duy", "duy nguyen").SequenceEqual(new[] { (0, 6), (11, 3) }), "Tô sáng đúng vị trí của từng từ trên chữ có dấu");


// Kiểm tra bộ lọc trang thực tế, tránh quê quán Thanh Hóa làm lọt tên khác.
var trangTimHocVien = new Nhom2_BTL.Components.Pages.HocVien();
var kieuTimHocVien = trangTimHocVien.GetType();
kieuTimHocVien.GetField("danhSachHocVien", rieng)!.SetValue(trangTimHocVien, new List<HocVien> {
    new() { MaHocVien = "HV01", HoTen = "Nguyễn Văn Anh", QueQuan = "Hà Nội" },
    new() { MaHocVien = "HV02", HoTen = "Nguyễn Anh Thư", QueQuan = "Thanh Hóa" },
    new() { MaHocVien = "HV03", HoTen = "Nguyễn Văn Thanh", QueQuan = "Thanh Hóa" }
});
kieuTimHocVien.GetField("tuKhoa", rieng)!.SetValue(trangTimHocVien, "anh");
var ketQuaTen = (IEnumerable<HocVien>)kieuTimHocVien.GetProperty("DanhSachHienThi", rieng)!.GetValue(trangTimHocVien)!;
KiemTra(ketQuaTen.Single().MaHocVien == "HV01", "Bộ lọc học viên chỉ trả tên Anh dù người khác quê Thanh Hóa");

await KiemThuHoSoVaSiSo.ChayAsync(phien.ServiceProvider.GetRequiredService<DichVuPhanQuyenDemo>(), KiemTra);
var hoSoMau = data.LayDanhSachCanBo().First(c => c.ChucVu == "Phó khoa");
var htmlHoSo = await HienThi<HoSoCanBo>(new() { [nameof(HoSoCanBo.MaCanBo)] = hoSoMau.MaCanBo });
KiemTra(new[] { hoSoMau.HoTen, hoSoMau.ChucVu, hoSoMau.TenKhoa, hoSoMau.TenTo, hoSoMau.GioiTinh }.All(htmlHoSo.Contains), "Dialog lãnh đạo dùng đúng hồ sơ cán bộ và đơn vị");
// Dữ liệu cá nhân mặc định hiển thị dạng che; số đầy đủ chỉ hiện sau khi bấm "Hiện đủ" (có ghi nhật ký).
KiemTra(htmlHoSo.Contains(QuyTacHocVu.Che(hoSoMau.SoCCCD)) && !htmlHoSo.Contains(hoSoMau.SoCCCD) && !htmlHoSo.Contains(hoSoMau.SoDienThoai) && htmlHoSo.Contains("Hiện đủ"), "Hồ sơ che CCCD, SĐT theo mặc định và có nút Hiện đủ");
var htmlKhoa = await HienThi<Nhom2_BTL.Components.Pages.Khoa>();
var htmlTo = await HienThi<Nhom2_BTL.Components.Pages.ToChuyenMon>();
KiemTra(htmlKhoa.Contains("Sĩ số cán bộ", StringComparison.OrdinalIgnoreCase) && System.Text.RegularExpressions.Regex.Matches(htmlTo, "class=\"o-khoa\"").Count == 12, "Bảng khoa có sĩ số cán bộ; tổ chuyên môn nhóm theo khoa (12 khối, 24 tổ)");
KiemTra(htmlKhoa.Contains("aria-haspopup=\"dialog\"") && htmlTo.Contains("aria-haspopup=\"dialog\""), "Tên lãnh đạo khoa và tổ có điều khiển mở hồ sơ");
// Nhóm F: lưu bền SQLite — mở lại kho trên cùng tệp phải thấy đúng dữ liệu đã ghi.
var tepNghiepVu = Path.Combine(Path.GetTempPath(), $"kiem-thu-du-lieu-{Guid.NewGuid()}.db");
try
{
    var tuyChonCsdl = new DbContextOptionsBuilder<CoSoDuLieuNghiepVu>().UseSqlite($"Data Source={tepNghiepVu};Pooling=False").Options;
    var nhaMayCsdl = new NhaMayCsdlKiemThu(tuyChonCsdl);
    var nhatKyKho = root.GetRequiredService<ILoggerFactory>().CreateLogger<KhoDuLieuDemo>();
    var khoBen = new KhoDuLieuDemo(nhaMayCsdl, nhatKyKho);
    KiemTra(khoBen.DanhSachKhoa.Count == 12 && khoBen.DanhSachTo.Count == 24 && khoBen.DanhSachCanBo.Count == 108 && khoBen.DanhSachLop.Count == 106 && khoBen.DanhSachHocVien.Count == 4240, "CSDL trống: sinh đủ 12/24/108/106/4.240 bản ghi");
    var thuTuGoc = khoBen.DanhSachHocVien.Take(40).Select(h => h.MaHocVien).ToList();
    var maXoa = "";
    lock (khoBen.KhoaDongBo)
    {
        var hvMoi = TaoHV("HV-BEN"); hvMoi.SoCCCD = "000077777777"; hvMoi.SoDienThoai = "0977777777";
        khoBen.DanhSachHocVien.Add(hvMoi);
        khoBen.DanhSachKhoa.Single(k => k.MaKhoa == "KH-01").TenKhoa = "Khoa đã đổi tên";
        // Xoá học viên lớp chưa có lịch (D53); học viên đã có điểm danh thì khoá ngoại sẽ chặn.
        maXoa = khoBen.DanhSachHocVien.First(h => h.MaLop == "B1D53").MaHocVien;
        khoBen.DanhSachHocVien.RemoveAll(h => h.MaHocVien == maXoa);
    }
    khoBen.ThongBaoThayDoi();
    var khoMoLai = new KhoDuLieuDemo(nhaMayCsdl, nhatKyKho);
    KiemTra(khoMoLai.DanhSachHocVien.Any(h => h.MaHocVien == "HV-BEN") && !khoMoLai.DanhSachHocVien.Any(h => h.MaHocVien == maXoa) && khoMoLai.DanhSachHocVien.Count == 4240, "Mở lại: còn học viên mới thêm, mất học viên đã xoá");
    KiemTra(khoMoLai.DanhSachKhoa.Single(k => k.MaKhoa == "KH-01").TenKhoa == "Khoa đã đổi tên", "Mở lại: giữ thay đổi tên khoa");
    KiemTra(khoMoLai.DanhSachHocVien.Take(40).Select(h => h.MaHocVien).SequenceEqual(thuTuGoc), "Mở lại: giữ đúng thứ tự hiển thị ban đầu");
    KiemTra(khoMoLai.DanhSachHocVien.Last().MaHocVien == "HV-BEN", "Bản ghi mới nằm cuối danh sách");
    KiemTra(khoMoLai.DanhSachKhoa.Single(k => k.MaKhoa == "KH-01").MaTruongKhoa == khoBen.DanhSachKhoa.Single(k => k.MaKhoa == "KH-01").MaTruongKhoa, "Mở lại: giữ phân công trưởng khoa");
    // CSDL chặn trùng CCCD dù dịch vụ bị bỏ qua; bộ nhớ được khôi phục về lần lưu gần nhất.
    lock (khoMoLai.KhoaDongBo)
    {
        var hvTrung = TaoHV("HV-TRUNG-CSDL"); hvTrung.SoCCCD = "000077777777";
        khoMoLai.DanhSachHocVien.Add(hvTrung);
    }
    var daChan = false;
    try { khoMoLai.ThongBaoThayDoi(); } catch (InvalidOperationException) { daChan = true; }
    KiemTra(daChan && !khoMoLai.DanhSachHocVien.Any(h => h.MaHocVien == "HV-TRUNG-CSDL"), "CSDL chặn trùng CCCD và bộ nhớ quay về lần lưu gần nhất");
}
finally { try { File.Delete(tepNghiepVu); } catch { } }

// Nhóm G: tích hợp mốc 1 (vai trò, lịch, điểm danh, điểm, nhật ký, tài khoản, tìm kiếm) trên CSDL tạm.
await KiemThuTichHop.ChayAsync(KiemTra);

Console.WriteLine($"HOÀN TẤT TOÀN BỘ: {dem} kiểm tra đạt.");

sealed class MoiTruongKiemThu : Microsoft.Extensions.Hosting.IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "KiemThu";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}

sealed class NhaMayCsdlKiemThu(DbContextOptions<CoSoDuLieuNghiepVu> tuyChon) : IDbContextFactory<CoSoDuLieuNghiepVu>
{
    public CoSoDuLieuNghiepVu CreateDbContext() => new(tuyChon);
}
