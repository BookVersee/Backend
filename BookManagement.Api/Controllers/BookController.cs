using System;
using System.Threading.Tasks;
using BookManagement.Service.Book;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [HttpGet]
        public async Task<IActionResult> FindBooks([FromQuery] FilterRequest filter)
        {
            var result = await _bookService.FindBooksAsync(filter);
            return Ok(ApiResponse<PagedResponse<BookResponse>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookDetail(Guid id)
        {
            var book = await _bookService.GetBookDetailAsync(id);
            return Ok(ApiResponse<BookResponse>.SuccessResponse(book));
        }

        [HttpGet("shops/{shopId}")]
        public async Task<IActionResult> GetShopProfile(Guid shopId)
        {
            var shop = await _bookService.GetShopProfileAsync(shopId);
            return Ok(ApiResponse<ShopResponse>.SuccessResponse(shop));
        }

        [HttpGet("shops/{shopId}/books")]
        public async Task<IActionResult> GetBooksByShop(Guid shopId)
        {
            var books = await _bookService.GetBooksByShopAsync(shopId);
            return Ok(ApiResponse<object>.SuccessResponse(books));
        }
    }
}
