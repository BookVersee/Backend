using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Services;

public class ChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatThreadDto>> GetUserChatThreadsAsync(Guid userId)
    {
        var chats = await _db.Chats
            .Include(c => c.Shop)
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return chats.Select(c =>
        {
            var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            int unread = c.Messages.Count(m => !m.IsRead && m.SenderId != userId);

            return new ChatThreadDto
            {
                ChatId = c.Id,
                UserId = c.UserId,
                UserName = c.Shop != null ? c.Shop.ShopName : "Shop",
                ShopId = c.ShopId,
                LastMessage = lastMsg?.Content,
                UnreadCount = unread,
                UpdatedAt = c.UpdatedAt ?? c.CreatedAt
            };
        }).OrderByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task<List<ChatThreadDto>> GetShopChatThreadsAsync(Guid shopId)
    {
        var chats = await _db.Chats
            .Include(c => c.User)
            .Include(c => c.Messages)
            .Where(c => c.ShopId == shopId)
            .ToListAsync();

        return chats.Select(c =>
        {
            var lastMsg = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
            int unread = c.Messages.Count(m => !m.IsRead && m.SenderId != c.User?.Id);

            return new ChatThreadDto
            {
                ChatId = c.Id,
                UserId = c.UserId,
                UserName = c.User != null ? (c.User.FullName ?? c.User.Username) : "User",
                ShopId = c.ShopId,
                LastMessage = lastMsg?.Content,
                UnreadCount = unread,
                UpdatedAt = c.UpdatedAt ?? c.CreatedAt
            };
        }).OrderByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid requesterId)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat == null)
        {
            throw new KeyNotFoundException("Chat thread not found.");
        }

        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                MessageId = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                Content = m.Content,
                ImageUrl = m.ImageUrl,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        // Mark unread messages as read
        var unreadMsgs = await _db.Messages
            .Where(m => m.ChatId == chatId && !m.IsRead && m.SenderId != requesterId)
            .ToListAsync();

        if (unreadMsgs.Any())
        {
            foreach (var msg in unreadMsgs)
            {
                msg.IsRead = true;
            }
            await _db.SaveChangesAsync();
        }

        return messages;
    }

    public async Task<MessageDto> SendMessageAsync(Guid userId, Guid shopId, string content, string? imageUrl, Guid senderId)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.UserId == userId && c.ShopId == shopId);
        if (chat == null)
        {
            chat = new Chat
            {
                UserId = userId,
                ShopId = shopId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
        }

        var message = new Message
        {
            ChatId = chat.Id,
            SenderId = senderId,
            Content = content,
            ImageUrl = imageUrl,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Messages.Add(message);
        chat.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return new MessageDto
        {
            MessageId = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            Content = message.Content,
            ImageUrl = message.ImageUrl,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }
}
