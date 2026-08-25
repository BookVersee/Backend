using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Book
{
    public interface IBookService
    {
        Task<PagedResponse<BookResponse>> FindBooksAsync(FilterRequest filter);
        Task<BookResponse> GetBookDetailAsync(Guid bookId);
        Task<ShopResponse> GetShopProfileAsync(Guid shopId);
        Task<IEnumerable<BookResponse>> GetBooksByShopAsync(Guid shopId);
    }
}
