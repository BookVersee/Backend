using System;
using System.Threading.Tasks;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Shop;

public class ShopService : IShopService
{
    private readonly AppDbContext _db;

    public ShopService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShopRegisterResponse> RegisterShopAsync(int userId, ShopRegisterRequest dto)
    {
        var existingShop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (existingShop != null)
        {
            throw new InvalidOperationException("User already registered a shop.");
        }

        var shop = new BookStore.BE2.Domain.Entities.Shop
        {
            UserId = userId,
            ShopName = dto.ShopName,
            Condition = ShopCondition.OPEN,
            Rating = 5.0f,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shops.Add(shop);

        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Address = dto.Address ?? user.Address;
            user.QrImageUrl = dto.QrImageUrl ?? user.QrImageUrl;
            user.Role = UserRole.SHOP;
        }

        await _db.SaveChangesAsync();

        return new ShopRegisterResponse
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            CreatedAt = shop.CreatedAt
        };
    }

    public async Task<ShopProfileResponse> GetShopProfileAsync(int userId)
    {
        var shop = await _db.Shops
            .Include(s => s.Books)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null)
        {
            throw new KeyNotFoundException("Shop not found for user.");
        }

        return new ShopProfileResponse
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            Rating = shop.Rating,
            TotalBooks = shop.Books.Count(b => b.Status != BookStatus.HIDDEN),
            CreatedAt = shop.CreatedAt
        };
    }

    public async Task UpdateShopProfileAsync(int userId, UpdateShopProfileRequest dto)
    {
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            throw new KeyNotFoundException("Shop not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.ShopName))
            shop.ShopName = dto.ShopName;

        if (!string.IsNullOrEmpty(dto.Condition) &&
            Enum.TryParse<ShopCondition>(dto.Condition, true, out var condition))
            shop.Condition = condition;

        await _db.SaveChangesAsync();
    }
}
