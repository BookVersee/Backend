using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Book;
using BookManagement.Service.Common;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [ApiController]
    [Route("api/shop")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        /// Chức năng: Tìm kiếm và lọc danh sách sản phẩm sách
        [HttpGet("FindBooks")]
        public async Task<IActionResult> FindBooks([FromQuery] BookQueryDto filter)
        {
            var result = await _bookService.FindBooksAsync(filter);
            return Ok(ApiResponse<PagedResponse<BookResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Xem thông tin chi tiết sản phẩm sách
        [HttpGet("GetBookDetail")]
        public async Task<IActionResult> GetBookDetail(Guid id)
        {
            var book = await _bookService.GetBookDetailAsync(id);
            return Ok(ApiResponse<BookResponse>.SuccessResponse(book));
        }

        /// Chức năng: Xem thông tin hồ sơ cửa hàng
        [HttpGet("GetShopProfile")]
        public async Task<IActionResult> GetShopProfile(Guid shopId)
        {
            var shop = await _bookService.GetShopProfileAsync(shopId);
            return Ok(ApiResponse<ShopProfileDto>.SuccessResponse(shop));
        }

        /// Chức năng: Lấy danh sách sản phẩm sách của Cửa hàng
        [HttpGet("GetBooksByShop")]
        public async Task<IActionResult> GetBooksByShop(Guid shopId)
        {
            var books = await _bookService.GetBooksByShopAsync(shopId);
            return Ok(ApiResponse<IEnumerable<BookResponse>>.SuccessResponse(books));
        }
    }
}
