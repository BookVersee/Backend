using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Cart
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IBookRepository _bookRepository;

        public CartService(ICartRepository cartRepository, IBookRepository bookRepository)
        {
            _cartRepository = cartRepository;
            _bookRepository = bookRepository;
        }

        public async Task<CartResponse> GetCartAsync(Guid userId)
        {
            var cart = await _cartRepository.GetOrCreateCartAsync(userId);
            return MapToResponse(cart);
        }

        public async Task<CartResponse> AddToCartAsync(Guid userId, AddItemRequest request)
        {
            var cart = await _cartRepository.GetOrCreateCartAsync(userId);
            var book = await _bookRepository.GetByIdAsync(request.BookId);
            if (book == null) throw new KeyNotFoundException("Book not found.");

            var existing = cart.CartBookDetails.FirstOrDefault(cbd => cbd.BookId == request.BookId);
            if (existing != null)
            {
                existing.Quantity += request.Quantity;
                existing.UnitPrice = book.Price;
                await _cartRepository.UpdateCartDetailAsync(existing);
            }
            else
            {
                var newItem = new CartBookDetail
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    BookId = book.Id,
                    Quantity = request.Quantity,
                    UnitPrice = book.Price
                };
                await _cartRepository.AddCartDetailAsync(newItem);
            }

            var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
            return MapToResponse(updatedCart!);
        }

        public async Task<CartResponse> UpdateCartItemAsync(Guid userId, Guid cartDetailId, UpdateItemRequest request)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null) throw new KeyNotFoundException("Cart not found.");

            var item = cart.CartBookDetails.FirstOrDefault(cbd => cbd.Id == cartDetailId);
            if (item == null) throw new KeyNotFoundException("Cart item not found.");

            if (request.Quantity <= 0)
                await _cartRepository.RemoveCartDetailAsync(cartDetailId);
            else
            {
                item.Quantity = request.Quantity;
                await _cartRepository.UpdateCartDetailAsync(item);
            }

            var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
            return MapToResponse(updatedCart!);
        }

        public async Task<CartResponse> RemoveFromCartAsync(Guid userId, Guid cartDetailId)
        {
            await _cartRepository.RemoveCartDetailAsync(cartDetailId);
            var updatedCart = await _cartRepository.GetCartByUserIdAsync(userId);
            return MapToResponse(updatedCart!);
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart != null) await _cartRepository.ClearCartAsync(cart.Id);
        }

        private static CartResponse MapToResponse(BookManagement.Repository.Entities.Cart cart)
        {
            var shopGroups = cart.CartBookDetails
                .GroupBy(cbd => cbd.Book?.ShopId ?? Guid.Empty)
                .Select(g =>
                {
                    var items = g.Select(cbd => new CartItemResponse
                    {
                        CartDetailId = cbd.Id,
                        BookId = cbd.BookId,
                        BookTitle = cbd.Book?.Title ?? "Unknown",
                        BookImage = cbd.Book?.ImageUrl,
                        UnitPrice = cbd.UnitPrice,
                        Quantity = cbd.Quantity
                    }).ToList();

                    return new ShopGroupResponse
                    {
                        ShopId = g.Key,
                        ShopName = g.FirstOrDefault()?.Book?.Shop?.ShopName ?? "Shop",
                        Items = items,
                        ShopSubtotal = items.Sum(i => i.TotalPrice)
                    };
                }).ToList();

            return new CartResponse
            {
                CartId = cart.Id,
                UserId = cart.UserId,
                ShopGroups = shopGroups,
                GrandTotal = shopGroups.Sum(sg => sg.ShopSubtotal)
            };
        }
    }
}
