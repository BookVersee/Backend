using System;
using System.Collections.Generic;

namespace BookManagement.Service.Common
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public object? Data { get; set; }
        public object? Errors { get; set; }
        public string? TraceId { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public static ApiResponse SuccessResponse(object? data, string message = "Request processed successfully", string? traceId = null)
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = traceId,
                TimestampUtc = DateTime.UtcNow
            };
        }

        public static ApiResponse ErrorResponse(string message, object? errors = null, string? traceId = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                TraceId = traceId,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public new T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T? data, string message = "Request processed successfully", string? traceId = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = traceId,
                TimestampUtc = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> FailureResponse(string message, object? errors = null, string? traceId = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                TraceId = traceId,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }

    public static class ApiResponseFactory
    {
        public static ApiResponse SuccessResponse(object? data, string message = "Request processed successfully", string? traceId = null)
        {
            return ApiResponse.SuccessResponse(data, message, traceId);
        }

        public static ApiResponse ErrorResponse(string message, object? errors = null, string? traceId = null)
        {
            return ApiResponse.ErrorResponse(message, errors, traceId);
        }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize < 1 ? 10 : PageSize));
    }

    public class PagedResultDto<T> : PagedResult<T>
    {
        public int TotalItems { get => TotalCount; set => TotalCount = value; }
        public int PageIndex { get => Page; set => Page = value; }
    }
}
