using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Shop
{
    public class ShopDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public ShopCondition Condition { get; set; }
        public float Rating { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
