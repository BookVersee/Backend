using BookManagement.Repository.Abstractions;

namespace BookManagement.Service.Shop;

public class ShopService : IShopService
{
    private readonly IShopRepository _shopRepository;
    private readonly IBookRepository _bookRepository;

    public ShopService(IShopRepository shopRepository, IBookRepository bookRepository)
    {
        _shopRepository = shopRepository;
        _bookRepository = bookRepository;
    }

    public async Task<ShopProfileResponse> GetShopProfileAsync(Guid shopId)
    {
        var shop = await _shopRepository.GetShopByIdAsync(shopId);
        if (shop == null)
            throw new Exception("Shop not found");

        return new ShopProfileResponse
        {
            Id = shop.Id,
            UserId = shop.UserId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            Rating = (decimal)shop.Rating,
            OwnerName = shop.User?.FullName,
            Phone = shop.User?.Phone,
            Address = shop.User?.Address
        };
    }

    public async Task<ShopProfileResponse?> GetShopByUserIdAsync(Guid userId)
    {
        var shop = await _shopRepository.GetShopByUserIdAsync(userId);
        if (shop == null)
            return null;

        return new ShopProfileResponse
        {
            Id = shop.Id,
            UserId = shop.UserId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            Rating = (decimal)shop.Rating,
            OwnerName = shop.User?.FullName,
            Phone = shop.User?.Phone,
            Address = shop.User?.Address
        };
    }

    public async Task<IEnumerable<ShopBookResponse>> GetBooksByShopAsync(Guid shopId)
    {
        var books = await _bookRepository.GetBooksByShopIdAsync(shopId);
        return books.Select(b => new ShopBookResponse
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Price = b.Price,
            ImageUrl = b.ImageUrl,
            Status = b.Status.ToString()
        }).ToList();
    }
}
