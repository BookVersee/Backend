using System;

namespace BookManagement.Service.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public object? Errors { get; set; }
        public string? TraceId { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public static ApiResponse SuccessResponse(object? data, string message = "Request processed successfully", string? traceId = null)
        {
            return new ApiResponse<object>
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = traceId
            };
        }

        public static ApiResponse ErrorResponse(string message, object? errors = null, string? traceId = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors,
                TraceId = traceId
            };
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T? data, string message = "Request processed successfully", string? traceId = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = traceId
            };
        }

        public static ApiResponse<T> FailureResponse(string message, object? errors = null, string? traceId = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                TraceId = traceId
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
}
