using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Chat;

public interface IChatRealtimeNotifier
{
    Task BroadcastMessageAsync(Guid chatId, MessageDto message);
}
