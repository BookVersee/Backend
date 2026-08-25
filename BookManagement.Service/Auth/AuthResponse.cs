using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Auth
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = null!;
    }

    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UserSessionResponse
    {
        public Guid Id { get; set; }
        public string RefreshToken { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
