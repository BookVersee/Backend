using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Shop;

namespace BookManagement.Service.Book
{
    public interface IBookService
    {
        Task<PagedResponse<BookResponse>> FindBooksAsync(BookQueryDto filter);
        Task<BookResponse> GetBookDetailAsync(Guid bookId);
        Task<ShopProfileDto> GetShopProfileAsync(Guid shopId);
        Task<IEnumerable<BookResponse>> GetBooksByShopAsync(Guid shopId);
    }
}
