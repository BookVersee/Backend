using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Feedback;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [ApiController]
    [Route("api/feedback")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// Chức năng: Xem danh sách đánh giá của 1 cuốn sách
        [HttpGet("GetBookFeedbacks")]
        public async Task<IActionResult> GetBookFeedbacks(Guid bookId)
        {
            var feedbacks = await _feedbackService.GetBookFeedbacksAsync(bookId);
            return Ok(ApiResponse<IEnumerable<FeedbackResponse>>.SuccessResponse(feedbacks));
        }

        /// Chức năng: Gửi đánh giá và chấm sao cho sản phẩm sách đã mua
        [Authorize]
        [HttpPost("WriteFeedback")]
        public async Task<IActionResult> WriteFeedback(CreateFeedbackRequest request)
        {
            var (userId, role) = User.GetUserInfo();
            var feedback = await _feedbackService.CreateFeedbackAsync(userId, request);
            return Ok(ApiResponse<FeedbackResponse>.SuccessResponse(feedback, "Feedback submitted successfully."));
        }

        /// Chức năng: Báo cáo phản hồi của Shop bị vi phạm lên Admin
        [Authorize]
        [HttpPost("ReportResponse")]
        public async Task<IActionResult> ReportResponse(Guid responseId, ReportResponseRequest request)
        {
            var (userId, role) = User.GetUserInfo();
            await _feedbackService.ReportResponseAsync(userId, responseId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Report submitted to Admin for review."));
        }
    }
}
