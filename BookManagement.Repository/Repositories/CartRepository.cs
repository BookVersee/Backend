using System;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByUserIdAsync(Guid userId)
        {
            return await _context.Carts
                .Include(c => c.CartBookDetails)
                    .ThenInclude(cbd => cbd.Book)
                        .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart> GetOrCreateCartAsync(Guid userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId
                };
                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }

        public async Task AddCartDetailAsync(CartBookDetail detail)
        {
            await _context.CartBookDetails.AddAsync(detail);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartDetailAsync(CartBookDetail detail)
        {
            _context.CartBookDetails.Update(detail);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCartDetailAsync(Guid cartDetailId)
        {
            var detail = await _context.CartBookDetails.FindAsync(cartDetailId);
            if (detail != null)
            {
                _context.CartBookDetails.Remove(detail);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(Guid cartId)
        {
            var details = await _context.CartBookDetails
                .Where(cbd => cbd.CartId == cartId)
                .ToListAsync();
            _context.CartBookDetails.RemoveRange(details);
            await _context.SaveChangesAsync();
        }
    }
}
