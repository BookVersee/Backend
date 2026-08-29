using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Shop : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public ShopCondition Condition { get; set; } = ShopCondition.PENDING;
        public float Rating { get; set; } = 0;
        public int ViolationCount { get; set; } = 0;
        public DateTimeOffset? LockedUntil { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<Book> Books { get; set; } = new List<Book>();
        public ICollection<Chat> Chats { get; set; } = new List<Chat>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
