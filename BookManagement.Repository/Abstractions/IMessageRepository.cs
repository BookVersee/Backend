using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid messageId);
    Task<IEnumerable<Message>> GetByChatIdAsync(Guid chatId);
    Task<IEnumerable<Message>> GetUnreadByChatAsync(Guid chatId);
    Task AddAsync(Message message);
    Task UpdateAsync(Message message);
    Task MarkAsReadAsync(Guid messageId);
    Task MarkChatMessagesAsReadAsync(Guid chatId);
}
