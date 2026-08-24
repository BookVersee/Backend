namespace BookManagement.Service.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public object? Data { get; set; }
    public object? Errors { get; set; }
    public string? TraceId { get; set; }
    public DateTime TimestampUtc { get; set; }
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public int StatusCode { get; set; }
    public DateTime TimestampUtc { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Request processed successfully")
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data,
            StatusCode = 200,
            TimestampUtc = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Fail(string message, int statusCode = 400, object? errors = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode,
            TimestampUtc = DateTime.UtcNow
        };
    }
}

public static class ApiResponseFactory
{
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
