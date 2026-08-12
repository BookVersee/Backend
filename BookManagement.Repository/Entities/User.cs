using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class User : BaseEntity<Guid>, IAuditableEntity
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; } = UserRole.CUSTOMER;
        public UserStatus Status { get; set; } = UserStatus.ACTIVE;
        public string? Address { get; set; }
        public string? QrImageUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Shop? Shop { get; set; }
        public Cart? Cart { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();
        public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
    }
}
