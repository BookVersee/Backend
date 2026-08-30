using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BookManagement.Service.User
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, tính toán, xử lý bảo mật và truy vấn trực tiếp DbContext.
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;

        public UserService(
            AppDbContext context,
            IEmailService emailService,
            IMemoryCache cache)
        {
            _context = context;
            _emailService = emailService;
            _cache = cache;
        }

        /// Chức năng: Lấy thông tin hồ sơ tài khoản cá nhân
        public async Task<UserResponse> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            return MapToResponse(user);
        }

        /// Chức năng: Cập nhật thông tin lý lịch cá nhân
        public async Task<UserResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Phone)) user.Phone = request.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(request.Address)) user.Address = request.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
                if (emailExists)
                    throw new InvalidOperationException("Email is already in use.");
                user.Email = request.Email.Trim().ToLower();
            }

            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
            return MapToResponse(user);
        }

        /// Chức năng: Gửi mã OTP đổi mật khẩu qua Email (Bước 1)
        public async Task SendPasswordOtpAsync(SendOtpRequest request)
        {
            var emailOrUsername = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == emailOrUsername || u.Email.ToLower() == emailOrUsername);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản với Email này.");

            var otp = Random.Shared.Next(100000, 999999).ToString();
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

        /// Chức năng: Xác thực mã OTP đổi mật khẩu (Bước 2)
        public async Task VerifyPasswordOtpAsync(VerifyPasswordOtpRequest request)
        {
            var emailOrUsername = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == emailOrUsername || u.Email.ToLower() == emailOrUsername);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản với Email này.");

            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            if (!_cache.TryGetValue(cacheKey, out string? savedOtp) || savedOtp != request.Otp)
            {
                throw new InvalidOperationException("Mã OTP không chính xác hoặc đã hết hạn.");
            }

            _cache.Set($"verified_reset_{user.Email.ToLower()}", true, TimeSpan.FromMinutes(10));
            _cache.Remove(cacheKey);
        }

        /// Chức năng: Đặt mật khẩu mới sau khi xác thực OTP thành công (Bước 3)
        public async Task ResetNewPasswordAsync(ResetNewPasswordRequest request)
        {
            var emailOrUsername = request.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == emailOrUsername || u.Email.ToLower() == emailOrUsername);
            if (user == null) throw new KeyNotFoundException("Không tìm thấy tài khoản.");

            var verifiedCacheKey = $"verified_reset_{user.Email.ToLower()}";

            if (!_cache.TryGetValue(verifiedCacheKey, out bool isVerified) || !isVerified)
            {
                throw new InvalidOperationException("Vui lòng xác thực mã OTP trước khi nhập mật khẩu mới.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            _cache.Remove(verifiedCacheKey);
            await _context.SaveChangesAsync();
        }

        /// Chức năng: Đổi mật khẩu tài khoản bằng mật khẩu cũ
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordWithOldPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("Tài khoản không tồn tại.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Mật khẩu cũ không chính xác.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        /// Chức năng: Lấy lịch sử giao dịch tài chính cá nhân
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

        /// Chức năng: Đăng ký nâng cấp tài khoản mở Cửa hàng bán sách
        public async Task<BookManagement.Service.Shop.ShopResponse> RegisterShopAsync(Guid userId, RegisterShopRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (user.Role != UserRole.CUSTOMER)
            {
                throw new InvalidOperationException("Only Customer accounts are allowed to register to become a shop.");
            }

            var existingShop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == userId);
            if (existingShop != null)
            {
                throw new InvalidOperationException("Account already has a shop or a pending shop registration application.");
            }

            var shop = new BookManagement.Repository.Entities.Shop
            {
                Id = userId,
                ShopName = request.ShopName.Trim(),
                Condition = ShopCondition.OPEN,
                Rating = 0
            };

            user.Role = UserRole.SHOP;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.Shops.AddAsync(shop);
            await _context.SaveChangesAsync();

            var adminUsers = await _context.Users.Where(u => u.Role == UserRole.ADMIN || u.Role == UserRole.SUPER_ADMIN).ToListAsync();
            foreach (var admin in adminUsers)
            {
                await _context.Notifications.AddAsync(new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = admin.Id,
                    Type = NotificationType.SYSTEM,
                    Content = $"Khách hàng {user.FullName ?? user.Username} đã đăng ký mở Cửa hàng '{shop.ShopName}' thành công (Trạng thái: Hoạt động).",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _context.Notifications.AddAsync(new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"Chúc mừng! Cửa hàng '{shop.ShopName}' của bạn đã được đăng ký thành công và gian hàng đã đi vào hoạt động. Bạn có thể bắt đầu đăng bán sách ngay!",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync();

            return new BookManagement.Service.Shop.ShopResponse
            {
                Id = shop.Id,
                UserId = shop.Id,
                ShopName = shop.ShopName,
                Condition = shop.Condition,
                Rating = shop.Rating
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
