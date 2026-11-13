// Vai trò tệp: Chính sách đơn giản cho ứng dụng minh họa: phải có cả chữ và số.
using Microsoft.AspNetCore.Identity;

namespace Nhom2_BTL.Services;

// Chính sách đơn giản cho ứng dụng minh họa: phải có cả chữ và số.
public sealed class KiemTraMatKhau : IPasswordValidator<TaiKhoan>
{
    public static void CauHinh(PasswordOptions cauHinh)
    {
        cauHinh.RequiredLength = 0;
        cauHinh.RequiredUniqueChars = 0;
        cauHinh.RequireUppercase = false;
        cauHinh.RequireLowercase = false;
        cauHinh.RequireDigit = false;
        cauHinh.RequireNonAlphanumeric = false;
    }

    public Task<IdentityResult> ValidateAsync(UserManager<TaiKhoan> manager, TaiKhoan user, string? password)
    {
        var hopLe = password is not null
            && password.Any(char.IsLetter)
            && password.Any(char.IsDigit);

        return Task.FromResult(hopLe ? IdentityResult.Success : IdentityResult.Failed(new IdentityError
        {
            Code = "MatKhauCanChuVaSo",
            Description = "Mật khẩu phải có cả chữ và số."
        }));
    }
}
