using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Cart;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [Authorize(Roles = "CUSTOMER,SHOP")]
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// Chức năng: Lấy danh sách sản phẩm giỏ hàng của người dùng
        [HttpGet("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            var (userId, role) = User.GetUserInfo();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart));
        }

        /// Chức năng: Thêm sản phẩm sách vào giỏ hàng
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart(AddItemRequest request)
        {
            var (userId, role) = User.GetUserInfo();
            var cart = await _cartService.AddToCartAsync(userId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item added to cart."));
        }

        /// Chức năng: Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPut("UpdateCartItem")]
        public async Task<IActionResult> UpdateCartItem(Guid cartDetailId, UpdateItemRequest request)
        {
            var (userId, role) = User.GetUserInfo();
            var cart = await _cartService.UpdateCartItemAsync(userId, cartDetailId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Cart item updated."));
        }

        /// Chức năng: Xóa 1 sản phẩm khỏi giỏ hàng
        [HttpDelete("RemoveFromCart")]
        public async Task<IActionResult> RemoveFromCart(Guid cartDetailId)
        {
            var (userId, role) = User.GetUserInfo();
            var cart = await _cartService.RemoveFromCartAsync(userId, cartDetailId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item removed from cart."));
        }

        /// Chức năng: Làm trống toàn bộ giỏ hàng
        [HttpDelete("ClearCart")]
        public async Task<IActionResult> ClearCart()
        {
            var (userId, role) = User.GetUserInfo();
            await _cartService.ClearCartAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Cart cleared."));
        }
    }
}
