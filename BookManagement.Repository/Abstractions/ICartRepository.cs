using System;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface ICartRepository
    {
        Task<Cart?> GetCartByUserIdAsync(Guid userId);
        Task<Cart> GetOrCreateCartAsync(Guid userId);
        Task AddCartDetailAsync(CartBookDetail detail);
        Task UpdateCartDetailAsync(CartBookDetail detail);
        Task RemoveCartDetailAsync(Guid cartDetailId);
        Task ClearCartAsync(Guid cartId);
    }
}
