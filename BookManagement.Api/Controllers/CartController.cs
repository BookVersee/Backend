using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Cart;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [Authorize(Roles = "CUSTOMER")]
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// Chức năng: Lấy danh sách sản phẩm trong giỏ hàng. Trả về: Dữ liệu giỏ hàng và tạm tính tổng tiền.
        [HttpGet("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart));
        }

        /// Chức năng: Thêm sản phẩm sách vào giỏ hàng. Trả về: Dữ liệu giỏ hàng mới nhất.
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddItemRequest request)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.AddToCartAsync(userId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item added to cart."));
        }

        /// Chức năng: Thay đổi số lượng sản phẩm trong giỏ hàng. Trả về: Dữ liệu giỏ hàng sau điều chỉnh.
        [HttpPut("UpdateCartItem")]
        public async Task<IActionResult> UpdateCartItem([FromQuery] Guid cartDetailId, [FromBody] UpdateItemRequest request)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.UpdateCartItemAsync(userId, cartDetailId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Cart item updated."));
        }

        /// Chức năng: Xóa sản phẩm khỏi giỏ hàng. Trả về: Dữ liệu giỏ hàng mới nhất.
        [HttpDelete("RemoveFromCart")]
        public async Task<IActionResult> RemoveFromCart([FromQuery] Guid cartDetailId)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.RemoveFromCartAsync(userId, cartDetailId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item removed from cart."));
        }

        /// Chức năng: Làm trống toàn bộ giỏ hàng. Trả về: Thông báo xác nhận làm trống giỏ hàng.
        [HttpDelete("ClearCart")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetCurrentUserId();
            await _cartService.ClearCartAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Cart cleared."));
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid authentication claims.");
            }
            return userId;
        }
    }
}
