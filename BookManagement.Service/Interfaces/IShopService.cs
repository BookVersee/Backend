using System;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos.Shop;
using BookManagement.Service.Models;

namespace BookManagement.Service.Interfaces
{
    public interface IShopService
    {
        Task<ApiResponse<ShopDto>> GetShopByIdAsync(Guid shopId);
        Task<ApiResponse<ShopDto>> GetShopByUserIdAsync(Guid userId);
        Task<ApiResponse<ShopDto>> CreateShopAsync(CreateShopDto dto);
        Task<ApiResponse<ShopDto>> UpdateShopAsync(Guid shopId, UpdateShopDto dto);
        Task<ApiResponse<bool>> UpdateShopConditionAsync(Guid shopId, ShopCondition condition);
    }
}
