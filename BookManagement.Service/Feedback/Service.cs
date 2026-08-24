using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Feedback
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IOrderRepository _orderRepository;

        public FeedbackService(
            IFeedbackRepository feedbackRepository,
            INotificationRepository notificationRepository,
            IOrderRepository orderRepository)
        {
            _feedbackRepository = feedbackRepository;
            _notificationRepository = notificationRepository;
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<FeedbackResponse>> GetBookFeedbacksAsync(Guid bookId)
        {
            var feedbacks = await _feedbackRepository.GetFeedbacksByBookIdAsync(bookId);
            return feedbacks.Select(MapToResponse);
        }

        public async Task<FeedbackResponse> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request)
        {
            // 1. Retrieve OrderDetail with Order
            var orderDetail = await _orderRepository.GetOrderDetailByIdAsync(request.OrderDetailId);
            if (orderDetail == null)
            {
                throw new KeyNotFoundException("Order item not found.");
            }

            // 2. Ensure Order belongs to the logged-in user
            if (orderDetail.Order.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only submit feedback for your own orders.");
            }

            // 3. Ensure Order status is COMPLETED or DELIVERED
            if (orderDetail.Order.OrderStatus != OrderStatus.COMPLETED &&
                orderDetail.Order.OrderStatus != OrderStatus.DELIVERED)
            {
                throw new InvalidOperationException("Feedback can only be submitted for completed or delivered orders.");
            }

            // 4. Ensure feedback has not been submitted for this order item already
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
            var created = await _feedbackRepository.GetFeedbackByIdAsync(feedback.Id);
            return MapToResponse(created!);
        }

        public async Task ReportResponseAsync(Guid userId, Guid responseId, ReportResponseRequest request)
        {
            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                Content = $"User reported shop response {responseId}: {request.Reason}",
                IsRead = false
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
