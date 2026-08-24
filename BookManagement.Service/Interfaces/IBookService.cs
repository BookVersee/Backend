using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos.Book;
using BookManagement.Service.Models;

namespace BookManagement.Service.Interfaces
{
    public interface IBookService
    {
        Task<ApiResponse<BookDto>> GetBookByIdAsync(Guid bookId);
        Task<ApiResponse<IEnumerable<BookDto>>> GetBooksByShopAsync(Guid shopId, BookFilterDto filter);
        Task<ApiResponse<BookDto>> CreateBookAsync(Guid shopId, CreateBookDto dto);
        Task<ApiResponse<BookDto>> UpdateBookAsync(Guid shopId, Guid bookId, UpdateBookDto dto);
        Task<ApiResponse<bool>> UpdateStockAsync(Guid shopId, Guid bookId, int stockQuantity);
        Task<ApiResponse<bool>> UpdateStatusAsync(Guid shopId, Guid bookId, BookStatus status);
        Task<ApiResponse<bool>> DeleteBookAsync(Guid shopId, Guid bookId);
    }
}
