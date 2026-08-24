using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Cart;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart));
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddItemRequest request)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.AddToCartAsync(userId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item added to cart."));
        }

        [HttpPut("items/{cartDetailId}")]
        public async Task<IActionResult> UpdateCartItem(Guid cartDetailId, [FromBody] UpdateItemRequest request)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.UpdateCartItemAsync(userId, cartDetailId, request);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Cart item updated."));
        }

        [HttpDelete("items/{cartDetailId}")]
        public async Task<IActionResult> RemoveFromCart(Guid cartDetailId)
        {
            var userId = GetCurrentUserId();
            var cart = await _cartService.RemoveFromCartAsync(userId, cartDetailId);
            return Ok(ApiResponse<CartResponse>.SuccessResponse(cart, "Item removed from cart."));
        }

        [HttpDelete("clear")]
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
