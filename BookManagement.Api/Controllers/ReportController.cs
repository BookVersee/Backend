using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Common;
using BookManagement.Service.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Phiên làm việc hết hạn hoặc không hợp lệ.");
            }
            return userId;
        }

        /// Chức năng: Gửi báo cáo vi phạm (Đoạn chat, Feedback, Shop, User)
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportRequestDto dto)
        {
            var reporterId = GetUserId();
            var report = await _reportService.CreateReportAsync(reporterId, dto);
            return Ok(ApiResponse<ReportResponseDto>.SuccessResponse(report, "Gửi báo cáo thành công. Hệ thống/Admin sẽ kiểm tra xử lý."));
        }

        /// Chức năng: Admin xem danh sách toàn bộ các báo cáo vi phạm
        [HttpGet]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> GetReports([FromQuery] string? status = null, [FromQuery] string? type = null)
        {
            var reports = await _reportService.GetReportsAsync(status, type);
            return Ok(ApiResponse<object>.SuccessResponse(reports, "Lấy danh sách báo cáo thành công."));
        }

        /// Chức năng: Admin phê duyệt hoặc từ chối báo cáo (Tự động tính 3-strikes lock shop)
        [HttpPost("{id}/resolve")]
        [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> ResolveReport(Guid id, [FromBody] ResolveReportRequestDto dto)
        {
            var adminId = GetUserId();
            await _reportService.ResolveReportAsync(adminId, id, dto);
            return Ok(ApiResponse<string>.SuccessResponse("Xử lý báo cáo thành công."));
        }
    }
}
