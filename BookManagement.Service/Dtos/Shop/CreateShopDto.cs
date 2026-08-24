using System;
using System.ComponentModel.DataAnnotations;

namespace BookManagement.Service.Dtos.Shop
{
    public class CreateShopDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(100, ErrorMessage = "Shop name cannot exceed 100 characters")]
        public string ShopName { get; set; } = null!;
    }
}
