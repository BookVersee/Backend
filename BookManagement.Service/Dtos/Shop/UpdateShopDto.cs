using System.ComponentModel.DataAnnotations;
using BookStore.BE2.Domain.Enums;

namespace BookManagement.Service.Dtos.Shop
{
    public class UpdateShopDto
    {
        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(100, ErrorMessage = "Shop name cannot exceed 100 characters")]
        public string ShopName { get; set; } = null!;

        public ShopCondition Condition { get; set; } = ShopCondition.OPEN;
    }
}
