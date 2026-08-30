using System;
using System.Security.Claims;

namespace BookManagement.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Extracted extension method to retrieve Current UserId from JWT ClaimsPrincipal
        /// </summary>
        public static Guid GetUserId(this ClaimsPrincipal user)
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
    }
}
