using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface IUserSessionRepository
    {
        Task CreateAsync(UserSession session);
        Task<UserSession?> GetByRefreshTokenAsync(string refreshToken);
        Task<IEnumerable<UserSession>> GetUserActiveSessionsAsync(Guid userId);
        Task UpdateAsync(UserSession session);
        Task RevokeAllUserSessionsAsync(Guid userId);
    }
}
