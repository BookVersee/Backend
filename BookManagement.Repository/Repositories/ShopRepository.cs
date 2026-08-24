using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class ShopRepository : IShopRepository
    {
        private readonly AppDbContext _context;

        public ShopRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Shop?> GetShopByIdAsync(Guid shopId)
        {
            return await _context.Shops
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == shopId);
        }

        public async Task<Shop?> GetShopByUserIdAsync(Guid userId)
        {
            return await _context.Shops
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<IEnumerable<Shop>> GetAllShopsAsync()
        {
            return await _context.Shops
                .Include(s => s.User)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Shop>> GetPendingShopsAsync()
        {
            return await _context.Shops
                .Include(s => s.User)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Shop shop)
        {
            await _context.Shops.AddAsync(shop);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Shop shop)
        {
            _context.Shops.Update(shop);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByIdAsync(Guid shopId)
        {
            return await _context.Shops.AnyAsync(s => s.Id == shopId);
        }
    }
}
