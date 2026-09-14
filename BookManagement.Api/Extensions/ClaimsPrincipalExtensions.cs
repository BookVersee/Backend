using System;
using System.Security.Claims;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Api.Extensions
{
    /// Vị trí: Extension Helper - Bóc tách UserId và UserRole từ JWT ClaimsPrincipal.
    public static class ClaimsPrincipalExtensions
    {
        /// Chức năng: Bóc tách UserId từ JWT Claims
        public static Guid GetUserId(this ClaimsPrincipal? user)
        {
            if (user == null)
            {
                throw new UnauthorizedAccessException("Authentication context is null.");
            }

            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing authentication claims.");
            }
            return userId;
        }

        /// Chức năng: Bóc tách cả UserId và UserRole từ JWT Claims cùng một lúc
        public static (Guid UserId, UserRole Role) GetUserInfo(this ClaimsPrincipal? user)
        {
            if (user == null)
            {
                throw new UnauthorizedAccessException("Authentication context is null.");
            }

            var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            var roleStr = user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role");

            Guid.TryParse(userIdStr, out var userId);
            Enum.TryParse<UserRole>(roleStr, true, out var role);

            return (userId, role);
        }
    }
}
