using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Models;

namespace BookManagement.Service.Auth
{
    public interface IUserSessionService
    {
        Task<TokenResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, string? deviceInfo = null);
        Task<TokenResponse> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null);
        //  Task<UserSessionDto> CreateSessionAsync(Guid userId, string ipAddress, string deviceInfo);
        Task<UserSessionResponse> CreateSessionAsync(
    Guid userId,
    string ipAddress,
    string deviceInfo);
        Task RevokeSessionAsync(string refreshToken);
        Task RevokeAllUserSessionsAsync(Guid userId);
        Task<TokenResponse> ValidateAndRefreshTokenAsync(string refreshToken);
        //Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId);
        Task<IEnumerable<UserSessionResponse>> GetUserSessionsAsync(Guid userId);
    }
}
