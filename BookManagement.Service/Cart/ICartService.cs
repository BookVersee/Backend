using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Cart
{
    public interface ICartService
    {
        Task<CartResponse> GetCartAsync(Guid userId);
        Task<CartResponse> AddToCartAsync(Guid userId, AddItemRequest request);
        Task<CartResponse> UpdateCartItemAsync(Guid userId, Guid cartDetailId, UpdateItemRequest request);
        Task<CartResponse> RemoveFromCartAsync(Guid userId, Guid cartDetailId);
        Task ClearCartAsync(Guid userId);
    }
}
