using System.Threading.Tasks;

namespace BookManagement.Service.Shop;

public interface IShopService
{
    Task<ShopRegisterResponse> RegisterShopAsync(int userId, ShopRegisterRequest dto);
    Task<ShopProfileResponse> GetShopProfileAsync(int userId);
    Task UpdateShopProfileAsync(int userId, UpdateShopProfileRequest dto);
}
