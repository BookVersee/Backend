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
    public class BookRepository : IBookRepository
    {
        private readonly AppDbContext _context;

        public BookRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Book?> GetByIdAsync(Guid id)
        {
            return await _context.Books
                .Include(b => b.Shop)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public Task<IQueryable<Book>> GetQueryableAsync()
        {
            return Task.FromResult(_context.Books
                .Include(b => b.Shop)
                .Include(b => b.Category)
                .AsNoTracking());
        }

        public async Task<IEnumerable<Book>> GetBooksByShopIdAsync(Guid shopId)
        {
            return await _context.Books
                .Include(b => b.Category)
                .Where(b => b.ShopId == shopId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Shop?> GetShopByIdAsync(Guid shopId)
        {
            return await _context.Shops
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == shopId);
        }

        public async Task UpdateAsync(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }
    }
}
