using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Cart
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, tính toán và truy vấn trực tiếp DbContext.
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        /// Chức năng: Tìm hoặc tự động tạo mới giỏ hàng cho người dùng
        private async Task<BookManagement.Repository.Entities.Cart> GetOrCreateCartEntityAsync(Guid userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartBookDetails)
                    .ThenInclude(cbd => cbd.Book)
                        .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new BookManagement.Repository.Entities.Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                await _context.Carts.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        /// Chức năng: Lấy thông tin giỏ hàng của người dùng
        public async Task<CartResponse> GetCartAsync(Guid userId)
        {
            var cart = await GetOrCreateCartEntityAsync(userId);
            return MapToResponse(cart);
        }

        /// Chức năng: Thêm mới sản phẩm sách vào giỏ hàng
        public async Task<CartResponse> AddToCartAsync(Guid userId, AddItemRequest request)
        {
            var cart = await GetOrCreateCartEntityAsync(userId);
            var book = await _context.Books
                .Include(b => b.Shop)
                .FirstOrDefaultAsync(b => b.Id == request.BookId);

            if (book == null) throw new KeyNotFoundException("Sản phẩm sách không tồn tại.");

            if (book.Status != BookStatus.ACTIVE)
            {
                throw new InvalidOperationException($"Sản phẩm '{book.Title}' hiện không còn mở bán.");
            }

            if (book.Shop == null || book.Shop.Condition != ShopCondition.OPEN)
            {
                throw new InvalidOperationException($"Cửa hàng cung cấp cuốn sách '{book.Title}' hiện chưa được duyệt hoặc đang tạm đóng cửa.");
            }

            var existing = cart.CartBookDetails.FirstOrDefault(cbd => cbd.BookId == request.BookId);
            int currentQtyInCart = existing?.Quantity ?? 0;
            int totalTargetQty = currentQtyInCart + request.Quantity;

            if (totalTargetQty > book.StockQuantity)
            {
                throw new InvalidOperationException($"Sản phẩm '{book.Title}' chỉ còn {book.StockQuantity} cuốn trong kho (bạn đang muốn mua tổng cộng {totalTargetQty} cuốn).");
            }

            if (existing != null)
            {
                existing.Quantity = totalTargetQty;
                existing.UnitPrice = book.Price;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newItem = new CartBookDetail
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    BookId = book.Id,
                    Quantity = request.Quantity,
                    UnitPrice = book.Price,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _context.CartBookDetails.AddAsync(newItem);
            }

            await _context.SaveChangesAsync();
            var updatedCart = await GetOrCreateCartEntityAsync(userId);
            return MapToResponse(updatedCart);
        }

        /// Chức năng: Cập nhật số lượng sản phẩm trong giỏ hàng
        public async Task<CartResponse> UpdateCartItemAsync(Guid userId, Guid cartDetailId, UpdateItemRequest request)
        {
            var cart = await GetOrCreateCartEntityAsync(userId);
            var item = cart.CartBookDetails.FirstOrDefault(cbd => cbd.Id == cartDetailId);
            if (item == null) throw new KeyNotFoundException("Cart item not found.");

            if (request.Quantity <= 0)
            {
                _context.CartBookDetails.Remove(item);
            }
            else
            {
                var book = item.Book ?? await _context.Books.FirstOrDefaultAsync(b => b.Id == item.BookId);
                if (book != null)
                {
                    if (book.Status != BookStatus.ACTIVE)
                    {
                        throw new InvalidOperationException($"Sản phẩm '{book.Title}' hiện không còn mở bán.");
                    }

                    if (request.Quantity > book.StockQuantity)
                    {
                        throw new InvalidOperationException($"Sản phẩm '{book.Title}' chỉ còn {book.StockQuantity} cuốn trong kho (bạn yêu cầu {request.Quantity} cuốn).");
                    }

                    item.UnitPrice = book.Price;
                }

                item.Quantity = request.Quantity;
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            var updatedCart = await GetOrCreateCartEntityAsync(userId);
            return MapToResponse(updatedCart);
        }

        /// Chức năng: Xóa 1 sản phẩm khỏi giỏ hàng
        public async Task<CartResponse> RemoveFromCartAsync(Guid userId, Guid cartDetailId)
        {
            var cart = await GetOrCreateCartEntityAsync(userId);
            var item = cart.CartBookDetails.FirstOrDefault(cbd => cbd.Id == cartDetailId);
            if (item == null) throw new KeyNotFoundException("Cart item not found in user cart.");

            _context.CartBookDetails.Remove(item);
            await _context.SaveChangesAsync();

            var updatedCart = await GetOrCreateCartEntityAsync(userId);
            return MapToResponse(updatedCart);
        }

        /// Chức năng: Làm trống toàn bộ giỏ hàng
        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartBookDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.CartBookDetails.Any())
            {
                _context.CartBookDetails.RemoveRange(cart.CartBookDetails);
                await _context.SaveChangesAsync();
            }
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
