using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Models;
using BookManagement.Service.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
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

        /// Chức năng: Lấy toàn bộ danh sách thông báo cá nhân. Trả về: Danh sách thông báo của người dùng.
        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(notifications));
        }

        /// Chức năng: Lấy danh sách thông báo chưa đọc. Trả về: Danh sách thông báo chưa xem.
        [HttpGet("GetUnreadNotifications")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            return Ok(ApiResponse<IEnumerable<NotificationResponse>>.SuccessResponse(notifications));
        }

        /// Chức năng: Đánh dấu 1 thông báo là đã đọc. Trả về: Thông báo xác nhận đã xem.
        [HttpPut("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromQuery] Guid id)
        {
            await _notificationService.MarkNotificationAsReadAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Notification marked as read."));
        }

        /// Chức năng: Đánh dấu tất cả thông báo là đã đọc. Trả về: Thông báo xác nhận đã đọc toàn bộ.
        [HttpPut("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllNotificationsAsReadAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("All notifications marked as read."));
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
