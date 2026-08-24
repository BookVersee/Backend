using System.Security.Claims;
using UserEntity = BookManagement.Repository.Entities.User;

namespace BookManagement.Service.JwtService;

public interface ITokenService
{
    string GenerateAccessToken(UserEntity user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}