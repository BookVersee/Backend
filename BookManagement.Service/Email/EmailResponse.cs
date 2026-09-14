using System;

namespace BookManagement.Service.Email
{
    public class EmailResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = null!;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
