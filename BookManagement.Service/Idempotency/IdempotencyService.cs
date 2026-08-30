using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace BookManagement.Service.Idempotency
{
    /// Vị trí: Core Infrastructure Service - Kiểm soát và ngăn chặn yêu cầu API trùng lặp (Idempotency Control).
    public class IdempotencyService : IIdempotencyService
    {
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private static readonly TimeSpan DefaultProcessingTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(10);

        private class CachedEntry
        {
            public IdempotencyStatus Status { get; set; }
            public int StatusCode { get; set; }
            public object? Data { get; set; }
            public string? ContentType { get; set; }
            public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        }

        public IdempotencyService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// Chức năng: Kiểm tra và khóa khóa Idempotency Key chống trùng lặp request
        public async Task<IdempotencyResult> TryAcquireAsync(string key, TimeSpan? processingTimeout = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return IdempotencyResult.Acquired();
            }

            var cacheKey = $"idempotency:{key}";
            var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            var timeout = processingTimeout ?? DefaultProcessingTimeout;
            bool acquired = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
            if (!acquired)
            {
                return IdempotencyResult.InProgress();
            }

            try
            {
                if (_cache.TryGetValue(cacheKey, out CachedEntry? entry) && entry != null)
                {
                    if (entry.Status == IdempotencyStatus.Completed)
                    {
                        return IdempotencyResult.Completed(entry.StatusCode, entry.Data, entry.ContentType);
                    }

                    if (entry.Status == IdempotencyStatus.Processing)
                    {
                        if (DateTimeOffset.UtcNow - entry.CreatedAt < timeout)
                        {
                            return IdempotencyResult.InProgress();
                        }
                    }
                }

                var processingEntry = new CachedEntry
                {
                    Status = IdempotencyStatus.Processing,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _cache.Set(cacheKey, processingEntry, timeout);
                return IdempotencyResult.Acquired();
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// Chức năng: Đánh dấu hoàn tất xử lý request và lưu kết quả Response vào RAM Cache
        public Task CompleteAsync(string key, int statusCode, object? responseData, TimeSpan? cacheDuration = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.CompletedTask;
            }

            var cacheKey = $"idempotency:{key}";
            var duration = cacheDuration ?? DefaultCacheDuration;

            var completedEntry = new CachedEntry
            {
                Status = IdempotencyStatus.Completed,
                StatusCode = statusCode,
                Data = responseData,
                ContentType = "application/json",
                CreatedAt = DateTimeOffset.UtcNow
            };

            _cache.Set(cacheKey, completedEntry, duration);
            return Task.CompletedTask;
        }

        /// Chức năng: Giải phóng khóa Idempotency Key khi request bị lỗi
        public Task ReleaseAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return Task.CompletedTask;
            }

            var cacheKey = $"idempotency:{key}";
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }
    }
}
