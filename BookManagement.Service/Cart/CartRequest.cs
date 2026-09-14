using System;

namespace BookManagement.Service.Cart
{
    public class AddItemRequest
    {
        public Guid BookId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateItemRequest
    {
        public int Quantity { get; set; }
    }
}
