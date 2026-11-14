// Vai trò tệp: Nâng schema SQLite cũ mà không xóa dữ liệu tài khoản.
using Microsoft.EntityFrameworkCore;
namespace Nhom2_BTL.Services;

public static class KhoiTaoCoSoTaiKhoan
{
    public static async Task TaoAsync(CoSoTaiKhoan db)
    {
        await db.Database.EnsureCreatedAsync();
        // Nâng schema SQLite cũ mà không xóa dữ liệu tài khoản.
        await db.Database.OpenConnectionAsync();
        try {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA table_info('AspNetUsers')";
            var coPhamVi = false;
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) coPhamVi |= reader.GetString(1) == "PhamVi";
            if (!coPhamVi) await db.Database.ExecuteSqlRawAsync("ALTER TABLE AspNetUsers ADD COLUMN PhamVi TEXT NOT NULL DEFAULT ''");
        } finally { await db.Database.CloseConnectionAsync(); }
    }
}
