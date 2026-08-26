using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Service.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookManagement.Service.User
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public UserService(
            AppDbContext context,
            IUserRepository userRepository,
            INotificationRepository notificationRepository,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _context = context;
            _userRepository = userRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<UserResponse> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            return MapToResponse(user);
        }

        public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
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

            var otp = Random.Shared.Next(100000, 999999).ToString();
            
            // Store OTP in RAM memory cache for 15 minutes (No DB columns required)
            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(15));

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #4F46E5; text-align: center;'>Hệ Thống BookManagement</h2>
                    <p>Xin chào <strong>{user.FullName ?? user.Username}</strong>,</p>
                    <p>Bạn đã gửi yêu cầu đặt lại mật khẩu tài khoản. Mã OTP xác thực của bạn là:</p>
                    <div style='background-color: #F3F4F6; text-align: center; padding: 15px; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111827; border-radius: 6px; margin: 20px 0;'>
                        {otp}
                    </div>
                    <p style='color: #6B7280; font-size: 14px;'>Mã OTP có hiệu lực trong <strong>15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "Mã OTP Đặt Lại Mật Khẩu - BookManagement", htmlBody);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out string? savedOtp))
            {
                if (savedOtp != request.ResetToken)
                    throw new InvalidOperationException("Mã OTP khôi phục mật khẩu không chính xác.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _cache.Remove(cacheKey);
            await _userRepository.UpdateAsync(user);
        }

        public async Task VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var cacheKey = $"verify_otp_{user.Email.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out string? savedOtp))
            {
                if (savedOtp != request.VerificationCode)
                    throw new InvalidOperationException("Mã OTP xác thực email không chính xác.");
                _cache.Remove(cacheKey);
            }

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #10B981;'>Xác Thực Email Thành Công!</h2>
                    <p>Xin chào <strong>{user.FullName ?? user.Username}</strong>,</p>
                    <p>Địa chỉ email <strong>{user.Email}</strong> của bạn đã được xác minh thành công trên hệ thống BookManagement.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "Xác Minh Email Thành Công - BookManagement", htmlBody);
        }

        public async Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(Guid userId)
        {
            var transactions = await _context.TransactionHistories
                .AsNoTracking()
                .Where(th => th.UserId == userId)
                .OrderByDescending(th => th.CreatedAt)
                .ToListAsync();

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
                Content = n.Content ?? string.Empty,
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

        public async Task<BookManagement.Service.Admin.ShopResponse> RegisterShopAsync(Guid userId, RegisterShopRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (user.Role != BookManagement.Repository.Entities.Enums.UserRole.CUSTOMER)
            {
                throw new InvalidOperationException("Only Customer accounts are allowed to register to become a shop.");
            }

            var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existingShop != null)
            {
                throw new InvalidOperationException("Account already has a shop or a pending shop registration application.");
            }

            var shop = new BookManagement.Repository.Entities.Shop
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShopName = request.ShopName.Trim(),
                Condition = BookManagement.Repository.Entities.Enums.ShopCondition.PENDING,
                Rating = 0
            };

            await _context.Shops.AddAsync(shop);
            await _context.SaveChangesAsync();

            // Auto-notify Admin accounts about pending shop application
            var adminUsers = await _context.Users.Where(u => u.Role == BookManagement.Repository.Entities.Enums.UserRole.ADMIN).ToListAsync();
            foreach (var admin in adminUsers)
            {
                await _notificationRepository.CreateNotificationAsync(new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    Type = BookManagement.Repository.Entities.Enums.NotificationType.SYSTEM,
                    Content = $"Khách hàng {user.FullName ?? user.Username} vừa nộp đơn mở Cửa hàng '{shop.ShopName}'. Vui lòng phê duyệt!",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            return new BookManagement.Service.Admin.ShopResponse
            {
                Id = shop.Id,
                UserId = shop.UserId,
                ShopName = shop.ShopName,
                Status = shop.Condition.ToString(),
                Rating = (decimal)shop.Rating
            };
        }

        private static UserResponse MapToResponse(BookManagement.Repository.Entities.User user) => new UserResponse
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
