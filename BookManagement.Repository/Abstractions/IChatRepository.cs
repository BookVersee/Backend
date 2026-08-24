using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid chatId);
    Task<Chat?> GetByUserAndShopAsync(Guid userId, Guid shopId);
    Task<IEnumerable<Chat>> GetChatsByUserAsync(Guid userId);
    Task<IEnumerable<Chat>> GetChatsByShopAsync(Guid shopId);
    Task AddAsync(Chat chat);
    Task UpdateAsync(Chat chat);
}
