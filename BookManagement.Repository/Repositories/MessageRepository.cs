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
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> GetByIdAsync(Guid messageId)
        {
            return await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<IEnumerable<Message>> GetByChatIdAsync(Guid chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetUnreadByChatAsync(Guid chatId)
        {
            return await _context.Messages
                .Where(m => m.ChatId == chatId && m.IsRead == false)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(Guid messageId)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message != null)
            {
                message.IsRead = true;
                await UpdateAsync(message);
            }
        }

        public async Task MarkChatMessagesAsReadAsync(Guid chatId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId && m.IsRead == false)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            _context.Messages.UpdateRange(messages);
            await _context.SaveChangesAsync();
        }
    }
}
