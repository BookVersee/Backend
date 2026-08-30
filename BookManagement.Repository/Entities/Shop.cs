using System;
using System.Collections.Generic;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Shop : User
    {
        public string ShopName { get; set; } = null!;
        public ShopCondition Condition { get; set; } = ShopCondition.OPEN;
        public float Rating { get; set; } = 0;
        public int ViolationCount { get; set; } = 0;
        public DateTimeOffset? LockedUntil { get; set; }

        // Navigation Properties
        public ICollection<Book> Books { get; set; } = new List<Book>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
