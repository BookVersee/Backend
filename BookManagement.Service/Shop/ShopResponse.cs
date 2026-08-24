namespace BookManagement.Service.Shop;

public class ShopProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ShopName { get; set; } = null!;
    public string? Condition { get; set; }
    public decimal Rating { get; set; }
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class ShopBookResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Author { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = null!;
}
