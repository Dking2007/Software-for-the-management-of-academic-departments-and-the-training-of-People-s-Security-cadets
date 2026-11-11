using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Nhom2_BTL.Models;

namespace Nhom2_BTL.Services;

/// <summary>
/// Nhớ theo TÀI KHOẢN phần hướng dẫn nào người dùng đã xem, để:
/// - người mới (chưa có dấu "chung") được mời xem giới thiệu một lần;
/// - người đã xem/bỏ qua không bao giờ bị làm phiền lại, kể cả trên máy khác;
/// - hướng dẫn riêng từng trang không tự bật, chỉ gợi ý bằng chấm nhỏ trên nút lá cờ cho tới khi xem.
/// Chỉ ghi cho chính người đang đăng nhập (lấy từ phiên), không nhận tên tài khoản từ trình duyệt.
/// </summary>
public sealed partial class DichVuHuongDan(IDbContextFactory<CoSoDuLieuNghiepVu> csdl, AuthenticationStateProvider xacThuc)
{
    public const string Chung = "chung";

    // Khoá hợp lệ: "chung" hoặc "trang:" + đường dẫn chữ thường (a-z, 0-9, -, /). Chặn chuỗi tuỳ ý từ trình duyệt.
    [GeneratedRegex("^(chung|trang:[a-z0-9\\-/]{1,80})$")]
    private static partial Regex KhoaHopLe();
    public static bool LaKhoaHopLe(string? khoa) => khoa is not null && KhoaHopLe().IsMatch(khoa);

    private async Task<string?> TenAsync()
    {
        var nd = NguoiDungHienTai.Tu((await xacThuc.GetAuthenticationStateAsync()).User);
        return nd.DaDangNhap && nd.Ten != "" ? nd.Ten : null;
    }

    public async Task<HashSet<string>> LayDaXemAsync()
    {
        var ten = await TenAsync();
        if (ten is null) return [];
        await using var db = await csdl.CreateDbContextAsync();
        return (await db.HuongDanDaXem.Where(x => x.TenDangNhap == ten).Select(x => x.Khoa).ToListAsync()).ToHashSet();
    }

    public async Task DanhDauAsync(string khoa)
    {
        if (!LaKhoaHopLe(khoa)) return;
        var ten = await TenAsync();
        if (ten is null) return;
        await using var db = await csdl.CreateDbContextAsync();
        if (await db.HuongDanDaXem.AnyAsync(x => x.TenDangNhap == ten && x.Khoa == khoa)) return;
        db.HuongDanDaXem.Add(new HuongDanDaXem { TenDangNhap = ten, Khoa = khoa, Luc = DateTimeOffset.UtcNow });
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { /* hai tab cùng ghi: đã có là được */ }
    }

    /// <summary>Cho phép người dùng tự đặt lại (Trợ giúp → "Xem lại như người mới").</summary>
    public async Task DatLaiAsync()
    {
        var ten = await TenAsync();
        if (ten is null) return;
        await using var db = await csdl.CreateDbContextAsync();
        db.HuongDanDaXem.RemoveRange(db.HuongDanDaXem.Where(x => x.TenDangNhap == ten));
        await db.SaveChangesAsync();
    }
}
