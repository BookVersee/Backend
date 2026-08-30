using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.User
{
    public interface IUserService
    {
        Task<UserResponse> GetProfileAsync(Guid userId);
        Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        
        // TH1: Đổi / Khôi phục mật khẩu qua mã OTP gửi về Gmail (3 Bước)
        Task SendPasswordOtpAsync(SendOtpRequest request);
        Task VerifyPasswordOtpAsync(VerifyPasswordOtpRequest request);
        Task ResetNewPasswordAsync(ResetNewPasswordRequest request);

        // TH2: Đổi mật khẩu bằng Mật khẩu cũ khi đã đăng nhập
        Task ChangePasswordAsync(Guid userId, ChangePasswordWithOldPasswordRequest request);

        Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(Guid userId);
        Task<BookManagement.Service.Shop.ShopResponse> RegisterShopAsync(Guid userId, RegisterShopRequest request);
        Task LogoutAsync(Guid userId, string? refreshToken = null);
    }
}
