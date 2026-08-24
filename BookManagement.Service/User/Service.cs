using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Service.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly INotificationRepository _notificationRepository;

        public UserService(
            IUserRepository userRepository,
            ITransactionRepository transactionRepository,
            INotificationRepository notificationRepository)
        {
            _userRepository = userRepository;
            _transactionRepository = transactionRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<Response> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            return MapToResponse(user);
        }

        public async Task<Response> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Phone)) user.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Address)) user.Address = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                if (await _userRepository.ExistsByEmailAsync(request.Email))
                    throw new InvalidOperationException("Email is already in use.");
                user.Email = request.Email.Trim().ToLower();
            }

            await _userRepository.UpdateAsync(user);
            return MapToResponse(user);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("User email not found.");
            // TODO: Integrate email sending service for reset link
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("User not found.");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("User not found.");
            // TODO: Validate email verification code
        }

        public async Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(Guid userId)
        {
            var transactions = await _transactionRepository.GetTransactionsByUserIdAsync(userId);
            return transactions.Select(th => new TransactionResponse
            {
                Id = th.Id,
                UserId = th.UserId,
                ReferenceType = th.ReferenceType,
                ReferenceId = th.ReferenceId,
                TransactionType = th.TransactionType,
                Amount = th.Amount,
                TransactionCode = th.TransactionCode,
                Description = th.Description,
                CreatedAt = th.CreatedAt
            });
        }

        public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId);
            return notifications.Select(n => new NotificationResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                Content = n.Content,
                ImageUrl = n.ImageUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task MarkNotificationAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null && notification.UserId == userId)
                await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        private static Response MapToResponse(BookManagement.Repository.Entities.User user) => new Response
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Address = user.Address,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };
    }
}
