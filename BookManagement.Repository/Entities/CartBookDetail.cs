using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class CartBookDetail : BaseEntity<Guid>
    {
        public Guid CartId { get; set; }
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation Properties
        public Cart Cart { get; set; } = null!;
        public Book Book { get; set; } = null!;
    }
}
