using System;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Repository.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            var normalized = usernameOrEmail.Trim().ToLower();
            return await _context.Users.FirstOrDefaultAsync(u => 
                u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalized = email.Trim().ToLower();
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == normalized);
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            var normalized = username.Trim().ToLower();
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == normalized);
        }

        public async Task CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
