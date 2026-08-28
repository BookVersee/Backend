using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Idempotency
{
    public enum IdempotencyStatus
    {
        None,
        Processing,
        Completed
    }

    public class IdempotencyResult
    {
        public IdempotencyStatus Status { get; set; }
        public int? StatusCode { get; set; }
        public string? ContentType { get; set; }
        public object? CachedResponse { get; set; }
        public string? Message { get; set; }

        public static IdempotencyResult Acquired() => new() { Status = IdempotencyStatus.Processing };
        public static IdempotencyResult InProgress(string message = "Yêu cầu đang được xử lý. Vui lòng không gửi lại liên tục.") 
            => new() { Status = IdempotencyStatus.Processing, Message = message };
        public static IdempotencyResult Completed(int statusCode, object? cachedResponse, string? contentType = "application/json") 
            => new() { Status = IdempotencyStatus.Completed, StatusCode = statusCode, CachedResponse = cachedResponse, ContentType = contentType };
    }

    public interface IIdempotencyService
    {
        Task<IdempotencyResult> TryAcquireAsync(string key, TimeSpan? processingTimeout = null);
        Task CompleteAsync(string key, int statusCode, object? responseData, TimeSpan? cacheDuration = null);
        Task ReleaseAsync(string key);
    }
}
