using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Idempotency;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BookManagement.Api.Filters
{
    /// <summary>
    /// Action Filter Attribute Chống trùng lặp dữ liệu & Chống Spam Request (Idempotency Key Filter).
    /// Chức năng chính:
    /// - Kiểm tra Header "Idempotency-Key" trong Request được gửi lên.
    /// - Nếu cùng 1 Key được bấm nhiều lần liên tiếp (ví dụ: bấm nút Đặt hàng / Thanh toán 2 lần), Filter sẽ chặn các Request sau và trả về kết quả đã lưu trữ trước đó.
    /// - Ngăn ngừa tình trạng tạo đơn hàng trùng hoặc trừ tiền 2 lần.
    /// Vị trí: Presentation Layer (BookManagement.Api/Filters).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class IdempotentAttribute : Attribute, IAsyncActionFilter
    {
        public string HeaderName { get; set; } = "Idempotency-Key";
        public int CacheDurationMinutes { get; set; } = 10;
        public int TimeoutSeconds { get; set; } = 30;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            string? idempotencyKeyHeader = null;

            if (httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues) && headerValues.Count > 0)
            {
                idempotencyKeyHeader = headerValues[0];
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Idempotency-Key", out var xHeaderValues) && xHeaderValues.Count > 0)
            {
                idempotencyKeyHeader = xHeaderValues[0];
            }

            if (string.IsNullOrWhiteSpace(idempotencyKeyHeader))
            {
                await next();
                return;
            }

            var idempotencyService = httpContext.RequestServices.GetRequiredService<IIdempotencyService>();

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) 
                         ?? httpContext.User.FindFirstValue("sub") 
                         ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                         ?? "anonymous";

            var fullKey = $"{userId}:{httpContext.Request.Path}:{idempotencyKeyHeader}";

            var acquireResult = await idempotencyService.TryAcquireAsync(fullKey, TimeSpan.FromSeconds(TimeoutSeconds));

            if (acquireResult.Status == IdempotencyStatus.Completed)
            {
                context.Result = new ObjectResult(acquireResult.CachedResponse)
                {
                    StatusCode = acquireResult.StatusCode ?? 200
                };
                return;
            }

            if (acquireResult.Status == IdempotencyStatus.Processing)
            {
                context.Result = new ConflictObjectResult(ApiResponse.ErrorResponse(
                    acquireResult.Message ?? "Yêu cầu giao dịch đang được xử lý, vui lòng không thao tác liên tục."));
                return;
            }

            ActionExecutedContext? executedContext = null;
            try
            {
                executedContext = await next();
            }
            catch
            {
                await idempotencyService.ReleaseAsync(fullKey);
                throw;
            }

            if (executedContext.Exception != null)
            {
                await idempotencyService.ReleaseAsync(fullKey);
                return;
            }

            if (executedContext.Result is ObjectResult objResult)
            {
                int statusCode = objResult.StatusCode ?? 200;
                if (statusCode >= 200 && statusCode < 300)
                {
                    await idempotencyService.CompleteAsync(fullKey, statusCode, objResult.Value, TimeSpan.FromMinutes(CacheDurationMinutes));
                }
                else
                {
                    await idempotencyService.ReleaseAsync(fullKey);
                }
            }
            else if (executedContext.Result is StatusCodeResult statusResult)
            {
                int statusCode = statusResult.StatusCode;
                if (statusCode >= 200 && statusCode < 300)
                {
                    await idempotencyService.CompleteAsync(fullKey, statusCode, null, TimeSpan.FromMinutes(CacheDurationMinutes));
                }
                else
                {
                    await idempotencyService.ReleaseAsync(fullKey);
                }
            }
            else
            {
                await idempotencyService.ReleaseAsync(fullKey);
            }
        }
    }
}
