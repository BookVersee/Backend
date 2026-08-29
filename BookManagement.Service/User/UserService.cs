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

        // TH1 - Bước 1: Gửi mã OTP khôi phục / đổi mật khẩu qua Gmail
        public async Task SendPasswordOtpAsync(SendOtpRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản với Email này.");

            var otp = Random.Shared.Next(100000, 999999).ToString();
            
            // Lưu OTP vào RAM Memory Cache trong 15 phút
            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(15));

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #4F46E5; text-align: center;'>Hệ Thống BookManagement</h2>
                    <p>Xin chào <strong>{user.FullName ?? user.Username}</strong>,</p>
                    <p>Bạn đã yêu cầu gửi mã OTP để đổi / khôi phục mật khẩu. Mã OTP của bạn là:</p>
                    <div style='background-color: #F3F4F6; text-align: center; padding: 15px; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111827; border-radius: 6px; margin: 20px 0;'>
                        {otp}
                    </div>
                    <p style='color: #6B7280; font-size: 14px;'>Mã OTP có hiệu lực trong <strong>15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "Mã OTP Xác Thực Đổi Mật Khẩu - BookManagement", htmlBody);
        }

        // TH1 - Bước 2: Xác thực mã OTP (Nếu đúng mới cho phép sang bước nhập mật khẩu mới)
        public async Task VerifyPasswordOtpAsync(VerifyPasswordOtpRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản với Email này.");

            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != request.Otp)
            {
                throw new InvalidOperationException("Mã OTP không chính xác hoặc đã hết hạn.");
            }

            // Đánh dấu xác thực OTP thành công trong RAM Cache (10 phút) để dùng ở Bước 3
            _cache.Set($"verified_reset_{user.Email.ToLower()}", true, TimeSpan.FromMinutes(10));
            _cache.Remove(cacheKey); // Xóa OTP cũ sau khi đã xác thực thành công
        }

        // TH1 - Bước 3: Đặt mật khẩu mới (Chỉ cần Email và Mật khẩu mới, không cần nhập lại OTP)
        public async Task ResetNewPasswordAsync(ResetNewPasswordRequest request)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản.");

            var verifiedCacheKey = $"verified_reset_{user.Email.ToLower()}";

            if (!_cache.TryGetValue(verifiedCacheKey, out bool isVerified) || !isVerified)
            {
                throw new InvalidOperationException("Vui lòng xác thực mã OTP trước khi nhập mật khẩu mới.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _cache.Remove(verifiedCacheKey); // Xóa trạng thái xác thực sau khi đổi mật khẩu thành công
            await _userRepository.UpdateAsync(user);
        }

        // TH2: Thay đổi mật khẩu khi nhớ Mật khẩu cũ (Yêu cầu đăng nhập)
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordWithOldPasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("Tài khoản không tồn tại.");

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
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
                Condition = BookManagement.Repository.Entities.Enums.ShopCondition.OPEN,
                Rating = 0
            };

            user.Role = BookManagement.Repository.Entities.Enums.UserRole.SHOP;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.Shops.AddAsync(shop);
            await _context.SaveChangesAsync();

            // Auto-notify Admin & Super Admin accounts about new shop activation
            var adminUsers = await _context.Users.Where(u => u.Role == BookManagement.Repository.Entities.Enums.UserRole.ADMIN || u.Role == BookManagement.Repository.Entities.Enums.UserRole.SUPER_ADMIN).ToListAsync();
            foreach (var admin in adminUsers)
            {
                await _notificationRepository.CreateNotificationAsync(new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    Type = BookManagement.Repository.Entities.Enums.NotificationType.SYSTEM,
                    Content = $"Khách hàng {user.FullName ?? user.Username} đã đăng ký mở Cửa hàng '{shop.ShopName}' thành công (Trạng thái: Hoạt động).",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            // Auto-notify New Shop Owner
            await _notificationRepository.CreateNotificationAsync(new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = BookManagement.Repository.Entities.Enums.NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"Chúc mừng! Cửa hàng '{shop.ShopName}' của bạn đã được đăng ký thành công và gian hàng đã đi vào hoạt động. Bạn có thể bắt đầu đăng bán sách ngay!",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            });

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
