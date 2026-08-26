using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.User
{
    public interface IUserService
    {
        Task<UserResponse> GetProfileAsync(Guid userId);
        Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task VerifyEmailAsync(VerifyEmailRequest request);
        Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(Guid userId);
        Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
        Task MarkNotificationAsReadAsync(Guid userId, Guid notificationId);
        Task<BookManagement.Service.Admin.ShopResponse> RegisterShopAsync(Guid userId, RegisterShopRequest request);
    }
}
