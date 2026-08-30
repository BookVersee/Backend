using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Common;
using BookManagement.Service.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// Chức năng: Lấy danh sách toàn bộ thông báo cá nhân
        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var (userId, role) = User.GetUserInfo();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(notifications));
        }

        /// Chức năng: Lấy danh sách các thông báo chưa đọc
        [HttpGet("GetUnreadNotifications")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var (userId, role) = User.GetUserInfo();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(notifications));
        }

        /// Chức năng: Đánh dấu 1 thông báo cụ thể là đã đọc
        [HttpPut("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var (userId, role) = User.GetUserInfo();
            var success = await _notificationService.MarkNotificationAsReadAsync(userId, id);
            if (!success)
            {
                return NotFound(ApiResponse<string>.ErrorResponse("Notification not found or access denied."));
            }
            return Ok(ApiResponse<string>.SuccessResponse("Notification marked as read."));
        }

        /// Chức năng: Đánh dấu tất cả thông báo là đã đọc
        [HttpPut("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var (userId, role) = User.GetUserInfo();
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("All notifications marked as read."));
        }
    }
}
