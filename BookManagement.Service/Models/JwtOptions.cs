using System.ComponentModel.DataAnnotations;

namespace BookManagement.Service.Models;

public class JwtOptions
{
    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;
    [Required] public string SecretKey { get; set; } = string.Empty;
    [Required] public int ExpireMinutes { get; set; } = 0;
}
