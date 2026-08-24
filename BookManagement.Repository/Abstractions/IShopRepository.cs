using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IShopRepository
{
    Task<Shop?> GetShopByIdAsync(Guid shopId);
    Task<Shop?> GetShopByUserIdAsync(Guid userId);
    Task<IEnumerable<Shop>> GetAllShopsAsync();
    Task<IEnumerable<Shop>> GetPendingShopsAsync();
    Task AddAsync(Shop shop);
    Task UpdateAsync(Shop shop);
    Task<bool> ExistsByIdAsync(Guid shopId);
}
