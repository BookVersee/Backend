using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Book;
using BookManagement.Service.Common;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;

namespace BookManagement.Service.Shop
{
    public interface IShopService
    {
        Task<ShopRegisterResponseDto> RegisterShopAsync(Guid userId, ShopRegisterDto dto);
        Task<ShopProfileDto> GetShopProfileAsync(Guid userIdOrShopId);
        Task<BookResponseDto> CreateBookAsync(Guid userIdOrShopId, CreateBookRequestDto dto);
        Task<BookResponseDto> GetBookByIdAsync(Guid userIdOrShopId, Guid bookId);
        Task<PagedResultDto<BookResponseDto>> GetShopBooksAsync(Guid userIdOrShopId, BookQueryDto query);
        Task<BookResponseDto> UpdateBookAsync(Guid userIdOrShopId, Guid bookId, UpdateBookRequestDto dto);
        Task DeleteBookAsync(Guid userIdOrShopId, Guid bookId);
        Task<ShopOrderDetailDto> GetShopOrderDetailAsync(Guid userIdOrShopId, Guid orderId);
        Task UpdateOrderStatusAsync(Guid userIdOrShopId, Guid orderId, UpdateOrderStatusDto dto);
        Task<RevenueResponseDto> GetShopRevenueAsync(Guid userIdOrShopId, RevenueQueryRequest query);
        Task<PagedResultDto<FeedbackDto>> GetShopFeedbacksAsync(Guid userIdOrShopId, ShopFeedbackQueryRequest query);
        Task<ResponseCreatedDto> CreateFeedbackResponseAsync(Guid userIdOrShopId, Guid feedbackId, FeedbackResponseRequestDto dto);
        Task ProcessReturnRequestAsync(Guid userIdOrShopId, Guid returnRequestId, ProcessReturnRequestDto dto);
        Task<ShopProfileDto> UpdateShopConditionAsync(Guid userIdOrShopId, UpdateShopConditionDto dto);
    }
}
