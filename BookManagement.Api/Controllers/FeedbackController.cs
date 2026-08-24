using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Feedback;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet("books/{bookId}")]
        public async Task<IActionResult> GetBookFeedbacks(Guid bookId)
        {
            var feedbacks = await _feedbackService.GetBookFeedbacksAsync(bookId);
            return Ok(ApiResponse<object>.SuccessResponse(feedbacks));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> WriteFeedback([FromBody] CreateFeedbackRequest request)
        {
            var userId = GetCurrentUserId();
            var feedback = await _feedbackService.CreateFeedbackAsync(userId, request);
            return Ok(ApiResponse<FeedbackResponse>.SuccessResponse(feedback, "Feedback submitted successfully."));
        }

        [Authorize]
        [HttpPost("responses/{responseId}/report")]
        public async Task<IActionResult> ReportResponse(Guid responseId, [FromBody] ReportResponseRequest request)
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
