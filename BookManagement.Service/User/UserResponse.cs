using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.User
{
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

    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public TransactionType? TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionCode { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public Guid? ReferenceId { get; set; }
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
