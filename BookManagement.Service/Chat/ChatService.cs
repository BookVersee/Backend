using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using ChatEntity = BookManagement.Repository.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Chat
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, xử lý tin nhắn và lưu DbContext.
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;
        private readonly IChatRealtimeNotifier? _realtimeNotifier;

        public ChatService(AppDbContext db, IChatRealtimeNotifier? realtimeNotifier = null)
        {
            _db = db;
            _realtimeNotifier = realtimeNotifier;
        }

        /// Chức năng: Lấy danh sách các cuộc trò chuyện của khách hàng
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

        /// Chức năng: Lấy danh sách các cuộc trò chuyện của Cửa hàng
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
                int unread = c.Messages.Count(m => !m.IsRead && m.SenderId == c.UserId);

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

        /// Chức năng: Xem danh sách tin nhắn và tự động đánh dấu đã đọc
        public async Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid requesterId)
        {
            var chat = await _db.Chats
                .Include(c => c.Shop)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                throw new KeyNotFoundException("Chat thread not found.");
            }

            var isCustomer = chat.UserId == requesterId;
            var isShopOwner = chat.Shop != null && chat.Shop.Id == requesterId;

            if (!isCustomer && !isShopOwner)
            {
                throw new UnauthorizedAccessException("You are not authorized to view messages in this chat thread.");
            }

            var messages = await _db.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    MessageId = m.Id,
                    ChatId = m.ChatId,
                    SenderId = m.SenderId,
                    Content = m.Content ?? string.Empty,
                    ImageUrl = m.ImageUrl,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

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

        /// Chức năng: Gửi tin nhắn mới và bắn thông báo SignalR realtime
        public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto)
        {
            Guid targetUserId = senderId;
            Guid targetShopId = dto.ShopId ?? Guid.Empty;

            if (dto.ChatId.HasValue && dto.ChatId.Value != Guid.Empty)
            {
                var existingChat = await _db.Chats.FirstOrDefaultAsync(c => c.Id == dto.ChatId.Value);
                if (existingChat != null)
                {
                    targetUserId = existingChat.UserId;
                    targetShopId = existingChat.ShopId;
                }
            }
            else if (dto.ShopId.HasValue && dto.ShopId.Value != Guid.Empty)
            {
                var senderShop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == senderId);
                if (senderShop != null && senderShop.Id == dto.ShopId.Value && dto.UserId.HasValue)
                {
                    targetUserId = dto.UserId.Value;
                    targetShopId = senderShop.Id;
                }
                else
                {
                    targetUserId = senderId;
                    targetShopId = dto.ShopId.Value;
                }
            }

            if (targetShopId == Guid.Empty)
            {
                throw new ArgumentException("ShopId or ChatId is required.");
            }

            var chat = await _db.Chats.FirstOrDefaultAsync(c => c.UserId == targetUserId && c.ShopId == targetShopId);
            if (chat == null)
            {
                chat = new ChatEntity
                {
                    UserId = targetUserId,
                    ShopId = targetShopId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
            }

            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = senderId,
                Content = dto.Content,
                ImageUrl = dto.ImageUrl,
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Messages.Add(message);
            chat.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            var messageDto = new MessageDto
            {
                MessageId = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                Content = message.Content,
                ImageUrl = message.ImageUrl,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            if (_realtimeNotifier != null)
            {
                await _realtimeNotifier.BroadcastMessageAsync(chat.Id, messageDto);
            }

            return messageDto;
        }

        /// Chức năng: Gửi tin nhắn mới với tham số mở rộng
        public async Task<MessageDto> SendMessageAsync(Guid userId, Guid shopId, string content, string? imageUrl, Guid senderId)
        {
            return await SendMessageAsync(senderId, new SendMessageDto
            {
                UserId = userId,
                ShopId = shopId,
                Content = content,
                ImageUrl = imageUrl
            });
        }
    }
}
