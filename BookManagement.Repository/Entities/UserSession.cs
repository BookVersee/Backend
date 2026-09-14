using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class UserSession : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public string RefreshToken { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
