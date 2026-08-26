using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.JwtService;
using UserEntity = BookManagement.Repository.Entities.User;
namespace BookManagement.Service.Auth
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly ITokenService _tokenService;
        private readonly BookManagement.Repository.Data.AppDbContext _context;

        public UserSessionService(
            IUserRepository userRepository,
            IUserSessionRepository sessionRepository,
            ITokenService tokenService,
            BookManagement.Repository.Data.AppDbContext context)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _tokenService = tokenService;
            _context = context;
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
