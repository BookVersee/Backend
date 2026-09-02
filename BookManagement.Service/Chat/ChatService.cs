using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using ChatEntity = BookManagement.Repository.Entities.Chat;
using ShopEntity = BookManagement.Repository.Entities.Shop;
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
                throw new KeyNotFoundException("Không tìm thấy cuộc trò chuyện này.");
            }

            var isCustomer = chat.UserId == requesterId;
            var isShopOwner = chat.Shop != null && chat.Shop.Id == requesterId;

            if (!isCustomer && !isShopOwner)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập đoạn chat này.");
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
            if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                throw new ArgumentException("Nội dung tin nhắn hoặc hình ảnh không được để trống.");
            }

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
                throw new ArgumentException("Thiếu thông tin Cửa hàng (ShopId) hoặc Phòng Chat (ChatId).");
            }

            // Kiểm tra và tự động bảo đảm bản ghi Shop tồn tại trong CSDL để tránh lỗi khóa ngoại (Foreign Key FK_Chats_Shops)
            var resolvedShopIds = await _db.Database
                .SqlQueryRaw<Guid>("SELECT Id FROM Shops WHERE UserId = {0} OR Id = {0}", targetShopId)
                .ToListAsync();

            if (resolvedShopIds.Any())
            {
                targetShopId = resolvedShopIds.First();
            }
            else
            {
                var shopUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == targetShopId);
                if (shopUser == null)
                {
                    throw new KeyNotFoundException("Cửa hàng không tồn tại trên hệ thống.");
                }

                var shopName = shopUser.FullName ?? shopUser.Username ?? "Cửa hàng";
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"IF NOT EXISTS (SELECT 1 FROM Shops WHERE Id = {targetShopId}) INSERT INTO Shops (Id, ShopName, Condition, Rating, ViolationCount) VALUES ({targetShopId}, {shopName}, 'OPEN', 5, 0);");
                targetShopId = shopUser.Id;
            }

            // Kiểm tra Người dùng gửi chat có tồn tại không
            var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == targetUserId);
            if (!userExists)
            {
                throw new KeyNotFoundException("Tài khoản người dùng không tồn tại.");
            }

            var chat = await _db.Chats.FirstOrDefaultAsync(c => c.UserId == targetUserId && c.ShopId == targetShopId);
            if (chat == null)
            {
                chat = new ChatEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = targetUserId,
                    ShopId = targetShopId,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.Chats.Add(chat);
                await _db.SaveChangesAsync();
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chat.Id,
                SenderId = senderId,
                Content = dto.Content?.Trim(),
                ImageUrl = dto.ImageUrl?.Trim(),
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
                Content = message.Content ?? string.Empty,
                ImageUrl = message.ImageUrl,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            if (_realtimeNotifier != null)
            {
                try
                {
                    await _realtimeNotifier.BroadcastMessageAsync(chat.Id, messageDto);
                }
                catch
                {
                    // Bỏ qua lỗi SignalR realtime nếu client ngắt kết nối
                }
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
