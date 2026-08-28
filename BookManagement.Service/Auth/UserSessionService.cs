using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Email;
using BookManagement.Service.JwtService;
using BookManagement.Service.Models;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using UserEntity = BookManagement.Repository.Entities.User;

namespace BookManagement.Service.Auth
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly ITokenService _tokenService;
        private readonly BookManagement.Repository.Data.AppDbContext _context;
        private readonly GoogleAuthOptions _googleAuthOptions;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;

        public UserSessionService(
            IUserRepository userRepository,
            IUserSessionRepository sessionRepository,
            ITokenService tokenService,
            BookManagement.Repository.Data.AppDbContext context,
            IOptions<GoogleAuthOptions> googleAuthOptions,
            IEmailService emailService,
            IMemoryCache memoryCache)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _tokenService = tokenService;
            _context = context;
            _googleAuthOptions = googleAuthOptions.Value;
            _emailService = emailService;
            _memoryCache = memoryCache;
        }

        public async Task<TokenResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? deviceInfo = null)
        {
            if (await _userRepository.ExistsByUsernameAsync(request.Username))
                throw new InvalidOperationException("Username is already taken.");

            if (await _userRepository.ExistsByEmailAsync(request.Email))
                throw new InvalidOperationException("Email is already registered.");

            var assignedRole = request.Username.Trim().ToLower().StartsWith("admin") || request.Role == UserRole.ADMIN
                ? UserRole.ADMIN
                : request.Role;

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = request.Username.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Phone = request.Phone,
                Address = request.Address,
                Role = assignedRole,
                Status = UserStatus.ACTIVE
            };

            await _userRepository.CreateAsync(user);

            var accessToken = _tokenService.GenerateAccessToken(user);
            var sessionResponse = await CreateSessionAsync(user.Id, ipAddress ?? "Unknown", deviceInfo ?? "Unknown");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = sessionResponse.RefreshToken,
                ExpiresAt = sessionResponse.ExpiresAt,
                User = MapToUserResponse(user)
            };
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username/email or password.");

            if (user.Status == UserStatus.LOCKED)
                throw new UnauthorizedAccessException("Your account has been locked.");

            var accessToken = _tokenService.GenerateAccessToken(user);
            var sessionResponse = await CreateSessionAsync(user.Id, ipAddress ?? "Unknown", deviceInfo ?? "Unknown");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = sessionResponse.RefreshToken,
                ExpiresAt = sessionResponse.ExpiresAt,
                User = MapToUserResponse(user)
            };
        }

        public async Task<UserSessionResponse> CreateSessionAsync(Guid userId, string ipAddress, string deviceInfo)
        {
            var refreshToken = _tokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(7);

            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RefreshToken = refreshToken,
                IpAddress = ipAddress,
                DeviceInfo = deviceInfo,
                ExpiresAt = expiresAt,
                IsRevoked = false
            };

            await _sessionRepository.CreateAsync(session);

            return new UserSessionResponse
            {
                Id = session.Id,
                RefreshToken = session.RefreshToken,
                IpAddress = session.IpAddress,
                DeviceInfo = session.DeviceInfo,
                ExpiresAt = session.ExpiresAt,
                IsRevoked = session.IsRevoked,
                CreatedAt = session.CreatedAt
            };
        }

        public async Task RevokeSessionAsync(string refreshToken)
        {
            var session = await _sessionRepository.GetByRefreshTokenAsync(refreshToken);
            if (session != null && !session.IsRevoked)
            {
                session.IsRevoked = true;
                await _sessionRepository.UpdateAsync(session);
            }
        }

        public async Task RevokeAllUserSessionsAsync(Guid userId)
        {
            await _sessionRepository.RevokeAllUserSessionsAsync(userId);
        }

        public async Task<TokenResponse> ValidateAndRefreshTokenAsync(string refreshToken)
        {
            var session = await _sessionRepository.GetByRefreshTokenAsync(refreshToken);
            if (session == null || session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = session.User;
            if (user == null || user.Status == UserStatus.LOCKED)
                throw new UnauthorizedAccessException("User is inactive or locked.");

            // Token Rotation: revoke old session, issue new pair
            session.IsRevoked = true;
            await _sessionRepository.UpdateAsync(session);

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newSession = await CreateSessionAsync(user.Id, session.IpAddress ?? "Unknown", session.DeviceInfo ?? "Unknown");

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newSession.RefreshToken,
                ExpiresAt = newSession.ExpiresAt,
                User = MapToUserResponse(user)
            };
        }

        public async Task<IEnumerable<UserSessionResponse>> GetUserSessionsAsync(Guid userId)
        {
            var sessions = await _sessionRepository.GetUserActiveSessionsAsync(userId);
            var result = new List<UserSessionResponse>();
            foreach (var s in sessions)
            {
                result.Add(new UserSessionResponse
                {
                    Id = s.Id,
                    RefreshToken = s.RefreshToken,
                    IpAddress = s.IpAddress,
                    DeviceInfo = s.DeviceInfo,
                    ExpiresAt = s.ExpiresAt,
                    IsRevoked = s.IsRevoked,
                    CreatedAt = s.CreatedAt
                });
            }
            return result;
        }

        public async Task<TokenResponse> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress = null, string? deviceInfo = null)
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
            {
                throw new ArgumentException("Google ID Token is required.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings();
                if (!string.IsNullOrWhiteSpace(_googleAuthOptions.ClientId) && !_googleAuthOptions.ClientId.Contains("YOUR_GOOGLE_CLOUD"))
                {
                    settings.Audience = new[] { _googleAuthOptions.ClientId };
                }
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid Google ID Token: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new InvalidOperationException("Google account does not contain a valid email address.");
            }

            var email = payload.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                var baseUsername = email.Split('@')[0];
                var username = baseUsername;
                int count = 1;
                while (await _userRepository.ExistsByUsernameAsync(username))
                {
                    username = $"{baseUsername}_{count++}";
                }

                user = new UserEntity
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    Email = email,
                    FullName = payload.Name ?? baseUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    Role = UserRole.CUSTOMER,
                    Status = UserStatus.ACTIVE,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _userRepository.CreateAsync(user);
            }

            if (user.Status == UserStatus.LOCKED)
            {
                throw new UnauthorizedAccessException("Account is locked.");
            }

            var accessToken = _tokenService.GenerateAccessToken(user);
            var sessionResponse = await CreateSessionAsync(user.Id, ipAddress ?? "Unknown", deviceInfo ?? "Unknown");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = sessionResponse.RefreshToken,
                ExpiresAt = sessionResponse.ExpiresAt,
                User = MapToUserResponse(user)
            };
        }

        public async Task SendPasswordResetOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.");
            }

            var user = await _userRepository.GetByUsernameOrEmailAsync(email.Trim());
            if (user == null)
            {
                throw new KeyNotFoundException("Tài khoản với Email này không tồn tại trên hệ thống.");
            }

            var otpCode = Random.Shared.Next(100000, 999999).ToString();
            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            _memoryCache.Set(cacheKey, otpCode, TimeSpan.FromMinutes(5));

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
                    <h2 style='color: #2563EB;'>Yêu Cầu Đặt Lại Mật Khẩu</h2>
                    <p>Xin chào <strong>{user.FullName ?? user.Username}</strong>,</p>
                    <p>Mã OTP để đặt lại mật khẩu cho tài khoản BookManagement của bạn là:</p>
                    <div style='background-color: #F3F4F6; padding: 15px; text-align: center; border-radius: 6px; margin: 20px 0;'>
                        <span style='font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #1D4ED8;'>{otpCode}</span>
                    </div>
                    <p style='color: #6B7280; font-size: 13px;'>Mã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";

            await _emailService.SendEmailAsync(user.Email, "Mã OTP Đặt Lại Mật Khẩu - BookManagement", htmlBody);
        }

        public async Task ResetPasswordWithOtpAsync(ResetPasswordWithOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("Email, mã OTP và mật khẩu mới không được để trống.");
            }

            var user = await _userRepository.GetByUsernameOrEmailAsync(request.Email.Trim());
            if (user == null)
            {
                throw new KeyNotFoundException("Tài khoản với Email này không tồn tại.");
            }

            var cacheKey = $"reset_otp_{user.Email.ToLower()}";
            if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.OtpCode.Trim())
            {
                throw new InvalidOperationException("Mã OTP không chính xác hoặc đã hết hạn (hiệu lực 5 phút).");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            _memoryCache.Remove(cacheKey);
        }

        private static UserResponse MapToUserResponse(UserEntity user) => new UserResponse
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
