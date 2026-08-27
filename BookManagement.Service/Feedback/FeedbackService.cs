using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly AppDbContext _context;

        public FeedbackService(
            IFeedbackRepository feedbackRepository,
            INotificationRepository notificationRepository,
            IOrderRepository orderRepository,
            AppDbContext context)
        {
            _feedbackRepository = feedbackRepository;
            _notificationRepository = notificationRepository;
            _orderRepository = orderRepository;
            _context = context;
        }

        public async Task<IEnumerable<FeedbackResponse>> GetBookFeedbacksAsync(Guid bookId)
        {
            var feedbacks = await _feedbackRepository.GetFeedbacksByBookIdAsync(bookId);
            return feedbacks.Select(MapToResponse);
        }

        public async Task<FeedbackResponse> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request)
        {
            var orderDetail = await _orderRepository.GetOrderDetailByIdAsync(request.OrderDetailId);
            if (orderDetail == null)
            {
                throw new KeyNotFoundException("Order item not found.");
            }

            if (orderDetail.Order.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only submit feedback for your own orders.");
            }

            if (orderDetail.Order.OrderStatus != OrderStatus.COMPLETED &&
                orderDetail.Order.OrderStatus != OrderStatus.DELIVERED)
            {
                throw new InvalidOperationException("Feedback can only be submitted for completed or delivered orders.");
            }

            var existingFeedback = await _feedbackRepository.GetFeedbackByOrderDetailIdAsync(request.OrderDetailId);
            if (existingFeedback != null)
            {
                throw new InvalidOperationException("Feedback has already been submitted for this order item.");
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
                ImageUrl = request.ImageUrl
            };

            await _feedbackRepository.CreateFeedbackAsync(feedback);

            // Recalculate average rating for Book & Shop
            if (orderDetail.BookId != Guid.Empty)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookId);
                if (book != null)
                {
                    var bookFeedbacks = await _context.Feedbacks
                        .Where(f => f.OrderDetail.BookId == book.Id)
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
                var shop = await _context.Shops.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == shopId);
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
                    if (shop.UserId != Guid.Empty)
                    {
                        var shopNotification = new BookManagement.Repository.Entities.Notification
                        {
                            Id = Guid.NewGuid(),
                            UserId = shop.UserId,
                            Type = NotificationType.SYSTEM,
                            ReferenceId = feedback.Id,
                            Content = $"Shop của bạn vừa nhận được đánh giá {feedback.Rating} sao từ khách hàng.",
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _notificationRepository.CreateNotificationAsync(shopNotification);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var created = await _feedbackRepository.GetFeedbackByIdAsync(feedback.Id);
            return MapToResponse(created!);
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
            await _notificationRepository.CreateNotificationAsync(notification);
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
