using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private readonly AppDbContext _context;

        public UserSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(UserSession session)
        {
            await _context.UserSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.UserSessions
                .Include(us => us.User)
                .FirstOrDefaultAsync(us => us.RefreshToken == refreshToken);
        }

        public async Task<IEnumerable<UserSession>> GetUserActiveSessionsAsync(Guid userId)
        {
            return await _context.UserSessions
                .Where(us => us.UserId == userId && !us.IsRevoked && us.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task UpdateAsync(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserSessionsAsync(Guid userId)
        {
            var activeSessions = await _context.UserSessions
                .Where(us => us.UserId == userId && !us.IsRevoked)
                .ToListAsync();

            foreach (var session in activeSessions)
            {
                session.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
