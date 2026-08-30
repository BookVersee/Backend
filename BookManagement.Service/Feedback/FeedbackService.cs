using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _context;

        public FeedbackService(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<BookManagement.Repository.Entities.Feedback> GetFullFeedbackQuery()
        {
            return _context.Feedbacks
                .Include(f => f.Shop)
                .Include(f => f.OrderDetail)
                .Include(f => f.Response)
                .AsNoTracking();
        }

        public async Task<IEnumerable<FeedbackResponse>> GetBookFeedbacksAsync(Guid bookId)
        {
            var feedbacks = await GetFullFeedbackQuery()
                .Where(f => f.OrderDetail != null && f.OrderDetail.BookId == bookId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return feedbacks.Select(MapToResponse);
        }

        public async Task<FeedbackResponse> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Book)
                .FirstOrDefaultAsync(od => od.Id == request.OrderDetailId);

            if (orderDetail == null)
            {
                throw new KeyNotFoundException("Order item not found.");
            }

            if (orderDetail.Order.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only submit feedback for your own orders.");
            }

            if (orderDetail.Order.OrderStatus != OrderStatus.DELIVERED)
            {
                throw new InvalidOperationException("Feedback can only be submitted for delivered orders.");
            }

            var existingFeedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.OrderDetailId == request.OrderDetailId);
            if (existingFeedback != null)
            {
                throw new InvalidOperationException("Feedback has already been submitted for this order item.");
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                throw new ArgumentException("Điểm đánh giá phải từ 1 đến 5 sao.");
            }

            var shopId = request.ShopId == Guid.Empty
                ? (orderDetail.Book?.ShopId ?? request.ShopId)
                : request.ShopId;

            var feedback = new BookManagement.Repository.Entities.Feedback
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                OrderDetailId = request.OrderDetailId,
                Rating = request.Rating,
                Content = request.Content,
                Type = request.Type,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _context.Feedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();

            // Recalculate average rating for Book & Shop
            if (orderDetail.BookId != Guid.Empty)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookId);
                if (book != null)
                {
                    var bookFeedbacks = await _context.Feedbacks
                        .Where(f => f.OrderDetail != null && f.OrderDetail.BookId == book.Id)
                        .Select(f => (double)f.Rating)
                        .ToListAsync();

                    if (bookFeedbacks.Any())
                    {
                        book.Rating = (float)bookFeedbacks.Average();
                    }
                }
            }

            if (shopId != Guid.Empty)
            {
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
                if (shop != null)
                {
                    var shopFeedbacks = await _context.Feedbacks
                        .Where(f => f.ShopId == shopId)
                        .Select(f => (double)f.Rating)
                        .ToListAsync();

                    if (shopFeedbacks.Any())
                    {
                        shop.Rating = (float)shopFeedbacks.Average();
                    }

                    // Notification for Shop Owner
                    if (shop.Id != Guid.Empty)
                    {
                        var shopNotification = new BookManagement.Repository.Entities.Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = shop.Id,
                            Type = NotificationType.SYSTEM,
                            ReferenceId = feedback.Id,
                            Content = $"Shop của bạn vừa nhận được đánh giá {feedback.Rating} sao từ khách hàng.",
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _context.Notifications.AddAsync(shopNotification);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var created = await GetFullFeedbackQuery().FirstOrDefaultAsync(f => f.Id == feedback.Id);
            return MapToResponse(created ?? feedback);
        }

        public async Task ReportResponseAsync(Guid userId, Guid responseId, ReportResponseRequest request)
        {
            var response = await _context.Responses
                .Include(r => r.Feedback)
                    .ThenInclude(f => f.OrderDetail)
                        .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(r => r.Id == responseId);

            if (response == null)
            {
                throw new KeyNotFoundException("Phản hồi của Shop không tồn tại.");
            }

            if (response.Feedback?.OrderDetail?.Order?.UserId != userId)
            {
                throw new InvalidOperationException("Bạn chỉ có quyền báo cáo phản hồi của Shop đối với bài đánh giá của chính bạn.");
            }

            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                ReferenceId = response.Id,
                Content = $"User reported shop response {responseId}: {request.Reason}",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        private static FeedbackResponse MapToResponse(BookManagement.Repository.Entities.Feedback f) => new FeedbackResponse
        {
            Id = f.Id,
            ShopId = f.ShopId,
            ShopName = f.Shop?.ShopName ?? "Shop",
            OrderDetailId = f.OrderDetailId,
            Rating = f.Rating,
            Content = f.Content,
            Type = f.Type,
            ImageUrl = f.ImageUrl,
            CreatedAt = f.CreatedAt,
            Response = f.Response == null ? null : new ShopResponseResponse
            {
                Id = f.Response.Id,
                FeedbackId = f.Response.FeedbackId,
                ShopId = f.Response.ShopId,
                Content = f.Response.Content,
                ImageUrl = f.Response.ImageUrl,
                CreatedAt = f.Response.CreatedAt
            }
        };
    }
}
