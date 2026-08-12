using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Category : BaseEntity<Guid>
    {
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public bool Status { get; set; } = true;

        // Navigation Properties
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
