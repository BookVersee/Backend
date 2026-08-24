using System;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
    }
}
