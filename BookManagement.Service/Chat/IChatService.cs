using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Chat
{
    public interface IChatService
    {
        Task<List<ChatThreadDto>> GetUserChatThreadsAsync(Guid userId);
        Task<List<ChatThreadDto>> GetShopChatThreadsAsync(Guid shopId);
        Task<List<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid requesterId);
        Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto);
    }
}
