using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Feedback;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// Chức năng: Lấy danh sách bài đánh giá sản phẩm sách. Trả về: Danh sách bình luận và điểm sao đánh giá.
        [HttpGet("GetBookFeedbacks")]
        public async Task<IActionResult> GetBookFeedbacks([FromQuery] Guid bookId)
        {
            var feedbacks = await _feedbackService.GetBookFeedbacksAsync(bookId);
            return Ok(ApiResponse<IEnumerable<FeedbackResponse>>.SuccessResponse(feedbacks));
        }

        /// Chức năng: Gửi bình luận và điểm đánh giá sản phẩm. Trả về: Thông tin bài đánh giá đã tạo.
        [Authorize]
        [HttpPost("WriteFeedback")]
        public async Task<IActionResult> WriteFeedback([FromBody] CreateFeedbackRequest request)
        {
            var userId = GetCurrentUserId();
            var feedback = await _feedbackService.CreateFeedbackAsync(userId, request);
            return Ok(ApiResponse<FeedbackResponse>.SuccessResponse(feedback, "Feedback submitted successfully."));
        }

        /// Chức năng: Báo cáo vi phạm phản hồi của người bán. Trả về: Thông báo xác nhận gửi báo cáo.
        [Authorize]
        [HttpPost("ReportResponse")]
        public async Task<IActionResult> ReportResponse([FromQuery] Guid responseId, [FromBody] ReportResponseRequest request)
        {
            var userId = GetCurrentUserId();
            await _feedbackService.ReportResponseAsync(userId, responseId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Report submitted to Admin for review."));
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
