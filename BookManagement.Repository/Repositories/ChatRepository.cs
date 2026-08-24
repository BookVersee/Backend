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
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Chat?> GetByIdAsync(Guid chatId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == chatId);
        }

        public async Task<Chat?> GetByUserAndShopAsync(Guid userId, Guid shopId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ShopId == shopId);
        }

        public async Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Chat>> GetChatsByShopAsync(Guid shopId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .Where(c => c.ShopId == shopId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Chat chat)
        {
            await _context.Chats.AddAsync(chat);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Chat chat)
        {
            _context.Chats.Update(chat);
            await _context.SaveChangesAsync();
        }
    }
}
