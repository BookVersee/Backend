using System;
using System.Collections.Generic;

namespace BookManagement.Service.JwtService
{
    public class GenerateTokenRequest
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public IEnumerable<string>? AdditionalPermissions { get; set; }
    }
}
