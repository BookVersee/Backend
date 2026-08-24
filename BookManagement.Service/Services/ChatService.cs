using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Services;

public class ChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatThreadDto>> GetShopChatsAsync(int shopId)
    {
        var chats = await _db.Chats
            .Include(c => c.User)
            .Include(c => c.Messages)
            .Where(c => c.ShopId == shopId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        var result = new List<ChatThreadDto>();
        foreach (var chat in chats)
        {
            var lastMsg = chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            int unreadCount = chat.Messages.Count(m => !m.IsRead && m.SenderId == chat.UserId);

            result.Add(new ChatThreadDto
            {
                ChatId = chat.ChatId,
                UserId = chat.UserId,
                UserName = chat.User?.FullName ?? chat.User?.Username ?? "Customer",
                ShopId = chat.ShopId,
                LastMessage = lastMsg?.Content,
                UnreadCount = unreadCount,
                UpdatedAt = chat.UpdatedAt
            });
        }

        return result;
    }

    public async Task<List<MessageDto>> GetChatMessagesAsync(int shopId, int chatId, int pageIndex, int pageSize, int shopUserId)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId && c.ShopId == shopId);
        if (chat == null)
        {
            throw new KeyNotFoundException("Chat thread not found or unauthorized access.");
        }

        var unreadMessages = await _db.Messages
            .Where(m => m.ChatId == chatId && !m.IsRead && m.SenderId != shopUserId)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _db.SaveChangesAsync();
        }

        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                Content = m.Content,
                ImageUrl = m.ImageUrl,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return messages;
    }
}
