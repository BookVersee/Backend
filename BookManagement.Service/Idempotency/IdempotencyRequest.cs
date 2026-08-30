using System;

namespace BookManagement.Service.Idempotency
{
    public class IdempotencyRequest
    {
        public string Key { get; set; } = null!;
        public TimeSpan? Timeout { get; set; }
    }
}
