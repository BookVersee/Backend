namespace BookManagement.Service.Shop;

public interface IShopService
{
    Task<ShopProfileResponse> GetShopProfileAsync(Guid shopId);
    Task<ShopProfileResponse?> GetShopByUserIdAsync(Guid userId);
    Task<IEnumerable<ShopBookResponse>> GetBooksByShopAsync(Guid shopId);
}
