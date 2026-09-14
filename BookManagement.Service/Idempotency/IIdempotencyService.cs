using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Idempotency
{
    public interface IIdempotencyService
    {
        Task<IdempotencyResult> TryAcquireAsync(string key, TimeSpan? processingTimeout = null);
        Task CompleteAsync(string key, int statusCode, object? responseData, TimeSpan? cacheDuration = null);
        Task ReleaseAsync(string key);
    }
}
