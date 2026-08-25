using System.Threading.Tasks;

namespace BookManagement.Service.Book;

public interface IBookService
{
    Task<BookResponse> CreateBookAsync(int shopId, CreateBookRequest dto);
    Task<BookResponse> GetBookByIdAsync(int shopId, int bookId);
    Task<PagedBookResponse> GetShopBooksAsync(int shopId, BookQueryRequest query);
    Task<BookResponse> UpdateBookAsync(int shopId, int bookId, UpdateBookRequest dto);
    Task DeleteBookAsync(int shopId, int bookId);
}
