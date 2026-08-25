using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Chat;

public interface IChatService
{
    Task<List<ChatThreadResponse>> GetShopChatsAsync(int shopId);
    Task<List<MessageResponse>> GetChatMessagesAsync(int shopId, int chatId, int pageIndex, int pageSize, int shopUserId);
}
